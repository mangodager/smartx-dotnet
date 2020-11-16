﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace ETModel
{
    public class RuleInfo
    {
        public string Address;
        public long   Start;
        public long   End;
        public long   LBH;
    }

    // 在裁决时间点向其他裁决服广播视图, 到达2/3的视图视为决议，广播2/3的决议
    public class Consensus : Component
    {
        public CalculatePower calculatePower = new CalculatePower(32L);
        public string auxiliaryAddress = "";
        public string consAddress = "";

        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        BlockMgr blockMgr = Entity.Root.GetComponent<BlockMgr>();
        NodeManager nodeManager = Entity.Root.GetComponent<NodeManager>();
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
        Rule rule = null;
        public bool transferShow = false;
        public bool openSyncFast = true;
        public bool bRun = true;

        public override void Awake(JToken jd = null)
        {
            try
            {
                if (jd["transferShow"] != null)
                    Boolean.TryParse(jd["transferShow"]?.ToString(), out transferShow);
                if (jd["openSyncFast"] != null)
                    Boolean.TryParse(jd["openSyncFast"]?.ToString(), out openSyncFast);
                Log.Info($"Consensus.transferShow = {transferShow}");
                Log.Info($"Consensus.openSyncFast = {openSyncFast}");

                if (jd["Run"] != null)
                    bool.TryParse(jd["Run"].ToString(), out bRun);


                ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
                componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);

                string genesisText = File.ReadAllText("./Data/genesisBlock.dat");
                Block genesisblk = JsonHelper.FromJson<Block>(genesisText);
                auxiliaryAddress = genesisblk.Address;

                consAddress = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.type == "contract"));

                long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out transferHeight);
                if (transferHeight == 0)
                {
                    if (true)
                    {
                        blockMgr.AddBlock(genesisblk);
                        ApplyGenesis(genesisblk);
                    }
                }

                //debug
                using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
                {
                    LuaVMScript luaVMScript = new LuaVMScript() { script = FileHelper.GetFileData("./Data/Contract/RuleContract_curr.lua").ToByteArray() };
                    snapshot.Contracts.Add(consAddress, luaVMScript);
                    snapshot.Commit();
                }

                if (jd["height"] != null)
                {
                    long height = long.Parse(jd["height"].ToString());
                    long.TryParse(levelDBStore.Get("UndoHeight"), out long height_total);
                    while (height_total > height)
                    {
                        height_total = Math.Max(height_total - 100, height);
                        levelDBStore.UndoTransfers(height_total);
                        Log.Debug($"UndoTransfers height = {height_total}");
                    }
                    ApplyBlockChain(height);
                }

                //string aa = BigInt.Div("1000,1000","1000");


            }
            catch (Exception)
            {
            }
        }

        public override void Start()
        {
            rule = Entity.Root.GetComponent<Rule>();

            if (bRun) {
                Run();
            }
        }

        private Action runAction;
        public void AddRunAction(Action a)
        {
            lock (this)
            {
                runAction += a;
            }
        }

        static public long GetReward(long height)
        {
            long half = height / 1025280L / 2; // 2*60*24*356 , half life of 2 year

            long reward = 1024L;
            while (half-- > 0)
            {
                reward = reward / 2;
            }
            return Math.Max(reward,1L);
        }

        static public long GetRewardRule(long height)
        {
            long half = height / 1025280L / 2; // 2*60*24*356 , half life of 2 year

            long reward = 64L;
            while (half-- > 0)
            {
                reward = reward / 2;
            }
            return Math.Max(reward, 1L);
        }


        public bool Check(Block target)
        {
            // 检查交易签名
            if (target.height != 1)
            {
                for (int ii = 0; ii < target.linkstran.Count; ii++)
                {
                    target.linkstran[ii].hash = target.linkstran[ii].ToHash();
                    if (!target.linkstran[ii].CheckSign())
                        return false;
                }
            }

            target.hash = target.ToHash();
            return CheckSign(target);
        }

        // 验证签名
        public bool CheckSign(Block target)
        {
            return Wallet.Verify(target.sign, target.hash, target.Address);
        }

        public bool IsRule(long height, string Address)
        {
            RuleInfo info = null;
            if (GetRule(height).TryGetValue(Address, out info))
            {
                if (info.Start <= height && (height < info.End || info.End == -1) && info.Address == Address)
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<long, Dictionary<string, RuleInfo>> cacheRule = new Dictionary<long, Dictionary<string, RuleInfo>>();
        public Dictionary<string, RuleInfo> GetRule(long height)
        {
            long target = cacheRule.Keys.FirstOrDefault( (x) => { return Math.Abs(x - height) <= 5; }  );
            if (target != 0)
                return cacheRule[target];

            if (transferHeight!=height&&Math.Abs(transferHeight - height)<= 5)
                return GetRule(transferHeight);

            using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
            {
                //Log.Debug($"GetRule {height}");
                string str = snapshot.Get($"Rule_{height}");
                if(!string.IsNullOrEmpty(str))
                {
                    var ruleInfo = JsonHelper.FromJson<Dictionary<string, RuleInfo>>(str);
                    cacheRule.Add(height,ruleInfo);
                    return ruleInfo;
                }
            }
            return transferHeight != 0 ? GetRule(transferHeight) : null;
        }

        // 获取某个块链接数
        public int GetBlockLinkCount(Block target)
        {
            int count = 0;

            for (int i = 0; i < target.linksblk.Count; i++)
            {
                Block blk = blockMgr.GetBlock(target.linksblk[i]);
                if (blk == null)
                    return 0;
                //if (blk != null && CheckSign(blk) && IsRule(blk.height, blk.Address))
                if (blk != null && IsRule(blk.height, blk.Address))
                {
                    count++;
                }
            }
            return count;
        }

        // 获取某个块被链接为主块的次数
        public int GetBlockBeLinkCount(Block target, List<Block> blks2 = null)
        {
            List<Block> blks = blks2 ?? blockMgr.GetBlock(target.height + 1);
            List<Block> blkNum = new List<Block>();
            for (int i = 0; i < blks.Count; i++)
            {
                Block blk = blks[i];
                if (target.hash == blk.prehash)
                {
                    //if (blk != null && CheckSign(blk) && IsRule(blk.height, blk.Address))
                    if (blk != null && IsRule(blk.height, blk.Address))
                    {
                        var exist = blkNum.Exists((x) => { return x.Address == blk.Address; });
                        if (!exist)
                            blkNum.Add(blk);
                    }
                }
            }

            return blkNum.Count;
        }


        public int GetRuleCount(long height,long max = 0)
        {
            int count = 0;
            foreach (RuleInfo info in GetRule(height).Values)
            {
                if (info.Start <= height && (height < info.End || info.End == -1))
                    count++;
                if (max != 0 && max == count)
                    break;
            }
            return count;
        }

        public bool ApplyGenesis(Block mcblk)
        {
            Log.Debug("ApplyGenesis");

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshotUndo(1))
            {
                Block linkblk = mcblk;
                for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                {
                    if (!ApplyTransfer(dbSnapshot, linkblk.linkstran[jj], linkblk.height))
                        return false;
                    if (!ApplyContract(dbSnapshot, linkblk.linkstran[jj], linkblk.height))
                        return false;
                }
                new BlockChain() { hash = mcblk.hash, height = mcblk.height }.Apply(dbSnapshot);

                dbSnapshot.Commit();
            }

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshot())
            {
                var ruleInfos = luaVMEnv.GetRules(consAddress, 1, dbSnapshot);
                dbSnapshot.Add($"Rule_{mcblk.height}", JsonHelper.ToJson(ruleInfos));
                dbSnapshot.Commit();
            }

            return true;
        }

        public bool ApplyHeight(BlockChain blockChain)
        {
            if (blockChain == null)
                return false;

            // 应用高度不是下一高度
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            if (blockChain.height - 1 != transferHeight)
                return false;

            Block mcblk = blockChain.GetMcBlock();
            if (mcblk == null)
                return false;

            Block preblk = BlockChainHelper.GetMcBlock(transferHeight);
            if (mcblk.prehash!=preblk.hash)
                return false;

            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

            // --------------------------------------------------
            calculatePower.Insert(mcblk);
            //Log.Debug($"ApplyHeight={blockChain.height}, {calculatePower.GetPower()}, {mcblk.GetDiff()}");

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshotUndo(blockChain.height))
            {
                blockChain.Apply(dbSnapshot);
                ApplyReward(dbSnapshot, mcblk);

                if (GetRuleCount(mcblk.height - 1, 2) >= 2 && BlockChainHelper.IsIrreversible(this, mcblk))
                    dbSnapshot.Add("2F1Height", mcblk.height.ToString());

                // 连接块交易
                for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
                {
                    Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                    if (linkblk == null)
                    {
                        return false;
                    }
                    if (linkblk.height != 1)
                    {
                        for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                        {
                            if (!ApplyTransfer(dbSnapshot, linkblk.linkstran[jj], linkblk.height))
                                return false;
                            if (!ApplyContract(dbSnapshot, linkblk.linkstran[jj], linkblk.height))
                                return false;
                        }
                    }
                }
                var ruleInfos = luaVMEnv.GetRules(consAddress, mcblk.height, dbSnapshot);
                dbSnapshot.Add($"Rule_{mcblk.height}",JsonHelper.ToJson(ruleInfos));

                dbSnapshot.Commit();
            }

            return true;
        }

        public bool ApplyTransfer(DbSnapshot dbSnapshot, BlockSub transfer, long height)
        {
            if (transfer.type != "transfer")
                return true;
            if (transfer.addressIn == transfer.addressOut)
                return true;
            if (BigHelper.Less(transfer.amount , "0" , true))
                return true;
            //if (!transfer.CheckSign() && height != 1)
            //    return true;

            if (height != 1)
            {
                Account accountIn = dbSnapshot.Accounts.Get(transfer.addressIn);
                if (accountIn == null)
                    return true;
                if (BigHelper.Less(accountIn.amount , transfer.amount,false))
                    return true;
                if (accountIn.nonce + 1 != transfer.nonce)
                    return true;
                accountIn.amount = BigHelper.Sub(accountIn.amount,transfer.amount);
                accountIn.nonce += 1;
                dbSnapshot.Accounts.Add(accountIn.address, accountIn);
                if(transferShow)
                    dbSnapshot.BindTransfer2Account(transfer.addressIn, transfer.hash);

            }

            Account accountOut = dbSnapshot.Accounts.Get(transfer.addressOut) ?? new Account() { address = transfer.addressOut, amount = "0", nonce = 0 };
            accountOut.amount = BigHelper.Add(accountOut.amount,transfer.amount);
            dbSnapshot.Accounts.Add(accountOut.address, accountOut);

            if (transferShow)
            {
                dbSnapshot.BindTransfer2Account(transfer.addressOut, transfer.hash);
                transfer.height = height;
                dbSnapshot.Transfers.Add(transfer.hash, transfer);
            }

            return true;
        }

        // 奖励上一周期的rule、本周期的MC出块rule  , 出块的rule会得到两次奖励
        public void ApplyReward(DbSnapshot dbSnapshot, Block mcblk)
        {
            var rewardKey = $"{mcblk.height}_Reward";
            if (dbSnapshot.Get(rewardKey) == "1")
                return ;

            dbSnapshot.Add(rewardKey, "1");

            var amount     = GetReward(mcblk.height).ToString();
            var amountRule = GetRewardRule(mcblk.height).ToString();
            var timestamp  = TimeHelper.Now();

            BlockSub Reward_Rule = null;
            BlockSub Reward      = null;
            if (transferShow)
            {
                Reward_Rule = new BlockSub() { hash = "", type = "Reward_Rule", nonce = mcblk.height, amount = amountRule };
                Reward_Rule.hash = Reward_Rule.ToHash();
                Reward_Rule.height = mcblk.height;
                Reward_Rule.timestamp = timestamp;
                dbSnapshot.Transfers.Add(Reward_Rule.hash, Reward_Rule);

                Reward = new BlockSub() { hash = "", type = "Reward_Mc", nonce = mcblk.height, data = mcblk.height.ToString(), amount = amount };
                Reward.height = mcblk.height;
                Reward.timestamp = timestamp;
                dbSnapshot.Transfers.Add(Reward.hash = Reward.ToHash(), Reward);
            }

            // rule奖励
            int ruleCount = 0;
            for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
            {
                Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                if (linkblk != null && IsRule(mcblk.height, linkblk.Address))
                {
                    ruleCount++;
                    Account linkAccount = dbSnapshot.Accounts.Get(linkblk.Address) ?? new Account() { address = linkblk.Address, amount = "0", nonce = 0 };
                    linkAccount.amount = BigHelper.Add(linkAccount.amount, amountRule);
                    dbSnapshot.Accounts.Add(linkAccount.address, linkAccount);
                    if (transferShow)
                        dbSnapshot.BindTransfer2Account(linkAccount.address, Reward_Rule.hash);
                }
            }

            // 出块奖励
            Account account = dbSnapshot.Accounts.Get(mcblk.Address) ?? new Account() { address = mcblk.Address, amount = "0", nonce = 0 };
            account.amount = BigHelper.Add(account.amount, amount);
            dbSnapshot.Accounts.Add(account.address, account);

            if (transferShow)
                dbSnapshot.BindTransfer2Account(account.address, Reward.hash);
        }

        LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
        public bool ApplyContract(DbSnapshot dbSnapshot, BlockSub transfer, long height)
        {
            if (transfer.type != "contract")
                return true;
            if (transfer.addressIn == transfer.addressOut)
                return true;
            //if (transfer.data == null || transfer.data == "")
            //    return true;
            //if (!transfer.CheckSign())
            //    return true;

            // 设置交易index
            Account accountIn = dbSnapshot.Accounts.Get(transfer.addressIn) ?? new Account() { address = transfer.addressIn, amount = "0", nonce = 0 };
            if (accountIn.nonce + 1 != transfer.nonce)
                return true;

            accountIn.nonce += 1;
            dbSnapshot.Accounts.Add(accountIn.address, accountIn);

            bool rel = luaVMEnv.Execute(dbSnapshot, transfer, height, out object[] reslut);

            if (transferShow&&rel)
            {
                var consAddressNew = LuaVMEnv.GetContractAddress(transfer);
                dbSnapshot.BindTransfer2Account($"{transfer.addressIn}{consAddressNew}", transfer.hash);
                transfer.height = height;
                dbSnapshot.Transfers.Add(transfer.hash, transfer);
            }

            return true;
        }

        public long transferHeight = 0;
        List<P2P_NewBlock> newBlocks = new List<P2P_NewBlock>();
        TimePass bifurcatedReportTime = new TimePass(15);

        public async void Run()
        {
            await Task.Delay(2000);

            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);

            Log.Debug($"Consensus.Run at height {transferHeight}");

            // 恢复到停机前的高度
            ApplyBlockChain();

            // 算力统计
            calculatePower.Clear();
            for (long ii = Math.Max(1, transferHeight - calculatePower.statistic); ii <= transferHeight; ii++)
            {
                calculatePower.Insert(BlockChainHelper.GetMcBlock(ii));
            }

            int blockIndex = 0;

            while (true)
            {
                try
                {
                    if (newBlocks.Count > 0)
                    {
                        for (blockIndex = blockIndex % newBlocks.Count; blockIndex < newBlocks.Count; blockIndex++)
                        {
                            if (newBlocks[blockIndex] == null)
                                continue;
                            var ipEndPoint = newBlocks[blockIndex].ipEndPoint;
                            Block otherMcBlk = JsonHelper.FromJson<Block>(newBlocks[blockIndex].block);
                            if (Check(otherMcBlk))
                            {
                                if (await SyncHeight(otherMcBlk, newBlocks[blockIndex].ipEndPoint))
                                {
                                    newBlocks.RemoveAll((x) => { return x.ipEndPoint == ipEndPoint; });
                                    break;
                                }
                                //if (await SyncHeight(otherMcBlk, newBlocks[ii].ipEndPoint))
                                ApplyBlockChain();
                            }
                            //break;
                        }
                    }

                    ApplyBlockChain();

                    lock (this)
                    {
                        runAction?.Invoke();
                        runAction = null;
                    }

                    if (bifurcatedReportTime.IsPassOnce() && bifurcatedReport != "")
                    {
                        Log.Info(bifurcatedReport);
                        bifurcatedReport = "";
                    }

                    if (newBlocks.Count == 0)
                    {
                        cacheRule.TryGetValue(transferHeight, out Dictionary<string, RuleInfo> ruleInfo);
                        cacheRule.Clear();
                        if (ruleInfo != null)
                        {
                            cacheRule.Add(transferHeight, ruleInfo);
                        }
                        await Task.Delay(1000);
                    }
                }
                catch (Exception e)
                {
                    newBlocks.Clear();
                    Log.Error(e);
                    await Task.Delay(1000);
                }
            }
        }

        public void ApplyBlockChainOld()
        {
            // 应用账本到T-1周期
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            var before = transferHeight;
            var chain1 = BlockChainHelper.GetBlockChain(transferHeight).GetMcBlockNext(blockMgr, this);
            var chain2 = chain1 == null ? null : chain1.GetMcBlockNext(blockMgr, this);
            while (chain1 != null && chain2 != null)
            {
                if (!ApplyHeight(chain1))
                    break;

                //// 可能会造成找不到块的BUG
                //var mcblk = chain1.GetMcBlock();
                //var blks = blockMgr.GetBlock(mcblk.height - 1);
                //if (blks.Count > mcblk.linksblk.Count)
                //{
                //    for (int ii = blks.Count - 1; ii >= 0; ii--)
                //    {
                //        if (mcblk.linksblk.Find((x) => { return x == blks[ii].hash; }) != blks[ii].hash)
                //        {
                //            var temp = BlockChainHelper.GetBlockChain(blks[ii].height-1);
                //            if(temp.hash!= blks[ii].prehash ) {
                //                blockMgr.DelBlock(blks[ii].hash);
                //            }
                //        }
                //    }
                //}

                chain1 = chain2;
                chain2 = chain2.GetMcBlockNext(blockMgr, this);

                if (before != transferHeight && chain1 != null && chain1.height % 100 == 0)
                    Log.Debug($"ApplyHeight {chain1.height}");
            }
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            if(before!= transferHeight)
                Log.Debug($"ApplyHeight {before} to {transferHeight}");

        }

        // 解决分叉链
        public BlockChain ApplyBlockChainMost(long targetHeight = -1)
        {
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            var chain1 = BlockChainHelper.GetBlockChain(transferHeight);
            var chain2 = BlockChainHelper.FindtChainMost(chain1, blockMgr, this);
            if (chain2 != null)
            {
                List<BlockChain> list = new List<BlockChain>();

                var chainStart = chain2;
                var chainEnd = chain1;
                while (chainStart.height != chainEnd.height)
                {
                    list.Insert(0, chainStart);
                    var blk = chainStart.GetMcBlock();
                    chainStart = new BlockChain() { hash = blk.prehash, height = blk.height - 1 };
                }

                if(list.Count>=2) {
                    if (ApplyHeight(list[0])) {
                        return list[0];
                    }
                }
                return null;
            }
            else
            if(chain1!=null)
            {
                // 应用账本到T-1周期
                chain1 = chain1.GetMcBlockNext(blockMgr, this);
                chain2 = chain1 == null ? null : chain1.GetMcBlockNext(blockMgr, this);
                if (chain1 != null && chain2 != null && (targetHeight==-1||chain1.height<=targetHeight) )
                {
                    if (ApplyHeight(chain1))
                        return chain1;
                }
            }
            return null;
        }

        public void ApplyBlockChain(long targetHeight=-1)
        {
            // 应用账本到T-1周期
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            var before = transferHeight;

            var chain1 = ApplyBlockChainMost(targetHeight);
            while (chain1 != null)
            {
                chain1 = ApplyBlockChainMost(targetHeight);
                if (before != transferHeight && chain1 != null && chain1.height % 100 == 0)
                    Log.Debug($"ApplyHeight {chain1.height}");
            }

            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            if (before != transferHeight)
                Log.Debug($"ApplyHeight {before} to {transferHeight}");
        }

        string bifurcatedReport = "";
        async Task<bool> SyncHeight(Block otherMcBlk, string ipEndPoint)
        {
            if (transferHeight + 20 < otherMcBlk.height)
            {
                // 随机一个openSyncFast节点
                string ipEndPoint2 = nodeManager.GetRandomNode(NodeManager.EnumState.openSyncFast);
                //Log.Info($"SyncHeightFast {ipEndPoint2}");
                if(!string.IsNullOrEmpty(ipEndPoint2))
                    return await SyncHeightFast(otherMcBlk, ipEndPoint2);
            }

            return await SyncHeightNear(otherMcBlk, ipEndPoint);
        }

        async Task<bool> SyncHeightFast(Block otherMcBlk,string ipEndPoint)
        {
            if (transferHeight >= otherMcBlk.height)
                return true;

            long.TryParse(levelDBStore.Get("Snap___2F1Height"), out long l2F1Height);
            l2F1Height = Math.Max(1, l2F1Height);
            string     hash2F1  = await QueryMcBlockHash(l2F1Height, ipEndPoint);
            BlockChain chain2F1 = BlockChainHelper.GetBlockChain(l2F1Height);
            if (chain2F1 != null && chain2F1.hash != hash2F1)
            {
                if(bifurcatedReport == ""&&!string.IsNullOrEmpty(hash2F1))
                    bifurcatedReport = $"find a branch chain. height: {otherMcBlk.height} address: {otherMcBlk.Address} , rules count: {otherMcBlk.linksblk.Count}";
                return true;
            }

            // 接收广播过来的主块
            // 检查链是否一致，不一致表示前一高度有数据缺失
            // 获取对方此高度主块linksblk列表,对方非主块的linksblk列表忽略不用拉取
            // 没有的块逐个拉取,校验数据是否正确,添加到数据库
            // 拉取到的块重复此过程
            // UndoTransfers到拉去到的块高度 , 有新块添加需要重新判断主块把漏掉的账本应用
            // GetMcBlock 重新去主块 ，  ApplyTransfers 应用账本
            long syncHeight = Math.Max(otherMcBlk.height-1, transferHeight);
            long currHeight = Math.Min(otherMcBlk.height-1, transferHeight);
            for (long jj = currHeight; jj > l2F1Height; jj--)
            {
                currHeight = jj;
                BlockChain chain = BlockChainHelper.GetBlockChain(jj);
                string hash = await QueryMcBlockHash(jj, ipEndPoint);
                if ( hash != "" && (chain == null || chain.hash != hash) )
                {
                    continue;
                }
                break;
            }

            // 
            Dictionary<long, string> blockChains = new Dictionary<long, string>();
            bool error = false;
            var q2p_Sync_Height = new Q2P_Sync_Height();
            q2p_Sync_Height.spacing = 23;
            q2p_Sync_Height.height  = currHeight;
            var reply = await QuerySync_Height(q2p_Sync_Height, ipEndPoint,15);
            if (reply != null)
            {
                blockChains = JsonHelper.FromJson<Dictionary<long, string>>(reply.blockChains);
                do
                {
                    for (int kk = 0; kk < reply.blocks.Count; kk++)
                    {
                        var blk = JsonHelper.FromJson<Block>(reply.blocks[kk]);
                        if (!blockMgr.AddBlock(blk))
                        {
                            error = true;
                            break;
                        }
                    }
                    if (!error&& reply.height!=-1)
                    {
                        q2p_Sync_Height.height = reply.height;
                        q2p_Sync_Height.spacing = Math.Max(1, 103 - (reply.height - currHeight));

                        reply = await QuerySync_Height(q2p_Sync_Height, ipEndPoint);
                    }
                }
                while (!error && reply != null && reply.height != -1);
            }

            //Log.Info($"SyncHeight: {currHeight} to {syncHeight}");
            bool bUndoHeight = false;
            long ii = currHeight;
            for ( ; ii < syncHeight + 1 && ii < currHeight + 20; ii++)
            {
                blockChains.TryGetValue(ii, out string hash);
                Block syncMcBlk = blockMgr.GetBlock(hash);
                if (syncMcBlk == null)
                    break;

                // 比较链
                if (syncMcBlk.height > 2 && !bUndoHeight)
                {
                    var chain = BlockChainHelper.GetBlockChain(syncMcBlk.height - 3);
                    if (chain != null)
                    {
                        var chainnext = chain.GetMcBlockNext();
                        if (chainnext != null)
                        {
                            Block blk2 = blockMgr.GetBlock(chainnext.hash);
                            Block blk3 = blockMgr.GetBlock(syncMcBlk.prehash);
                            if (blk2 != null && blk3 != null && blk2.hash != blk3.prehash)
                            {
                                bUndoHeight = true;
                            }
                        }
                    }
                }
            }

            // 回滚到同步块的高度
            if (bUndoHeight)
            {
                long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
                levelDBStore.UndoTransfers(currHeight);
                long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            }

            if (ii < syncHeight + 1)
                return false;

            return true;
        }

        async Task<bool> SyncHeightNear(Block otherMcBlk, string ipEndPoint)
        {
            if (transferHeight >= otherMcBlk.height)
                return true;

            long.TryParse(levelDBStore.Get("Snap___2F1Height"), out long l2F1Height);
            l2F1Height = Math.Max(1, l2F1Height);
            string hash2F1 = await QueryMcBlockHash(l2F1Height, ipEndPoint);
            BlockChain chain2F1 = BlockChainHelper.GetBlockChain(l2F1Height);
            if (chain2F1 != null && chain2F1.hash != hash2F1)
            {
                if (bifurcatedReport == "" && !string.IsNullOrEmpty(hash2F1))
                    bifurcatedReport = $"find a branch chain. height: {otherMcBlk.height} address: {otherMcBlk.Address} , rules count: {otherMcBlk.linksblk.Count}";
                return true;
            }

            // 接收广播过来的主块
            // 检查链是否一致，不一致表示前一高度有数据缺失
            // 获取对方此高度主块linksblk列表,对方非主块的linksblk列表忽略不用拉取
            // 没有的块逐个拉取,校验数据是否正确,添加到数据库
            // 拉取到的块重复此过程
            // UndoTransfers到拉去到的块高度 , 有新块添加需要重新判断主块把漏掉的账本应用
            // GetMcBlock 重新去主块 ，  ApplyTransfers 应用账本
            long syncHeight = Math.Max(otherMcBlk.height - 1, transferHeight);
            long currHeight = Math.Min(otherMcBlk.height - 1, transferHeight);
            for (long jj = currHeight; jj > l2F1Height; jj--)
            {
                currHeight = jj;
                BlockChain chain = BlockChainHelper.GetBlockChain(jj);
                string hash = await QueryMcBlockHash(jj, ipEndPoint);
                if (hash != "" && (chain == null || chain.hash != hash))
                {
                    continue;
                }
                break;
            }

            //Log.Info($"SyncHeight: {currHeight} to {syncHeight}");
            bool bUndoHeight = false;
            long ii = currHeight;
            for (; ii < syncHeight + 1 && ii < currHeight + 10; ii++)
            {
                Block syncMcBlk = null;
                if (ii == otherMcBlk.height - 1) // 同步prehash， 先查
                {
                    syncMcBlk = blockMgr.GetBlock(otherMcBlk.prehash) ?? await QueryMcBlock(ii, ipEndPoint);
                }
                if (ii == otherMcBlk.height)
                {
                    syncMcBlk = otherMcBlk;
                }
                else
                {
                    string hash = await QueryMcBlockHash(ii, ipEndPoint);
                    syncMcBlk = blockMgr.GetBlock(hash);
                    if (syncMcBlk == null)
                    {
                        syncMcBlk = await QueryMcBlock(ii, ipEndPoint);
                    }
                }

                if (syncMcBlk == null)
                    break;

                if (!blockMgr.AddBlock(syncMcBlk))
                    break;

                for (int jj = 0; jj < syncMcBlk.linksblk.Count; jj++)
                {
                    Block queryBlock = blockMgr.GetBlock(syncMcBlk.linksblk[jj]) ?? await QueryBlock(syncMcBlk.linksblk[jj], ipEndPoint);
                    if (queryBlock == null)
                        break;
                    if (!blockMgr.AddBlock(queryBlock))
                        break;
                }

                var beLinks = await QueryBeLinkHash(syncMcBlk.hash, ipEndPoint);
                if (beLinks != null)
                {
                    for (int jj = 0; jj < beLinks.Count; jj++)
                    {
                        Block queryBlock = blockMgr.GetBlock(beLinks[jj]) ?? await QueryBlock(beLinks[jj], ipEndPoint);
                        if (queryBlock == null)
                            break;
                        if (!blockMgr.AddBlock(queryBlock))
                            break;
                    }
                }

                // 比较链
                if (syncMcBlk.height > 2 && !bUndoHeight)
                {
                    var chain = BlockChainHelper.GetBlockChain(syncMcBlk.height - 3);
                    if (chain != null)
                    {
                        var chainnext = chain.GetMcBlockNext();
                        if (chainnext != null)
                        {
                            Block blk2 = blockMgr.GetBlock(chainnext.hash);
                            Block blk3 = blockMgr.GetBlock(syncMcBlk.prehash);
                            if (blk2 != null && blk3 != null && blk2.hash != blk3.prehash)
                            {
                                bUndoHeight = true;
                            }
                        }
                    }
                }
            }

            // 回滚到同步块的高度
            if (bUndoHeight)
            {
                long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
                levelDBStore.UndoTransfers(currHeight);
                long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            }

            if (ii < syncHeight + 1)
                return false;

            return true;
        }

        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;
            if (p2p_Block.networkID != BlockMgr.networkID)
                return;

            if (string.IsNullOrEmpty(p2p_Block.ipEndPoint))
                return;

            var newBlock = new P2P_NewBlock() { block = p2p_Block.block, networkID = BlockMgr.networkID, ipEndPoint = p2p_Block.ipEndPoint };
            newBlocks.RemoveAll((x) => { return x.ipEndPoint == newBlock.ipEndPoint; });
            newBlocks.Add(newBlock) ;
        }

        public async Task<string> QueryPrehashmkl(long syncHeight, string ipEndPoint = null)
        {
            Q2P_Prehashmkl q2p_PreHash = new Q2P_Prehashmkl();
            q2p_PreHash.ActorId = nodeManager.GetMyNodeId();
            q2p_PreHash.height  = syncHeight;

            Session session = null;
            if (ipEndPoint != null && ipEndPoint != "")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session != null && !session.IsConnect())
                session = null;

            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if(node!=null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_Prehashmkl r2p_Prehashmkl = (R2P_Prehashmkl)await session.Query(q2p_PreHash);
                return r2p_Prehashmkl != null ? r2p_Prehashmkl.prehashmkl : "";
            }
            return "";
        }

        [MessageMethod(NetOpcode.Q2P_Prehashmkl)]
        public static void Q2P_Prehashmkl_Handle(Session session, int opcode, object msg)
        {
            Q2P_Prehashmkl q2p_Prehashmkl = msg as Q2P_Prehashmkl;
            Block mcbkl = BlockChainHelper.GetMcBlock(q2p_Prehashmkl.height);
            R2P_Prehashmkl r2p_Prehashmkl = new R2P_Prehashmkl() { prehashmkl = mcbkl != null ? mcbkl.prehashmkl : "" };
            session.Reply(q2p_Prehashmkl, r2p_Prehashmkl);
        }

        public async Task<Block> QueryBlock(string hash, string ipEndPoint = null)
        {
            Q2P_Block q2p_Block = new Q2P_Block();
            q2p_Block.ActorId = nodeManager.GetMyNodeId();
            q2p_Block.hash = hash;

            Session session = null;
            if (ipEndPoint != null && ipEndPoint != "")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session !=null&&!session.IsConnect())
                session = null;

            Block blk = null;
            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if(node!=null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_Block r2p_Block = (R2P_Block)await session.Query(q2p_Block);
                if (r2p_Block != null&& r2p_Block.block!="")
                    blk = JsonHelper.FromJson<Block>(r2p_Block.block);
            }

            return blk;
        }

        [MessageMethod(NetOpcode.Q2P_Block)]
        public static void Q2P_Block_Handle(Session session, int opcode, object msg)
        {
            Q2P_Block q2p_Block = msg as Q2P_Block;
            Block blk = Entity.Root.GetComponent<BlockMgr>().GetBlock(q2p_Block.hash);
            R2P_Block r2p_Block = new R2P_Block() { block = blk!=null?JsonHelper.ToJson(blk):"" };
            session.Reply(q2p_Block, r2p_Block);
        }

        [MessageMethod(NetOpcode.Q2P_HasBlock)]
        public static void Q2P_HasBlock_Handle(Session session, int opcode, object msg)
        {
            Q2P_HasBlock q2p_HasBlock = msg as Q2P_HasBlock;
            Block blk = Entity.Root.GetComponent<BlockMgr>().GetBlock(q2p_HasBlock.hash);
            R2P_HasBlock r2p_HasBlock = new R2P_HasBlock() { has = blk != null };
            session.Reply(q2p_HasBlock, r2p_HasBlock);
        }

        public async Task<Block> QueryMcBlock(long height, string ipEndPoint=null)
        {
            Q2P_McBlock q2p_mcBlock = new Q2P_McBlock();
            q2p_mcBlock.ActorId = nodeManager.GetMyNodeId();
            q2p_mcBlock.height = height;

            Session session = null;
            if (ipEndPoint != null && ipEndPoint !="")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session != null && !session.IsConnect())
                session = null;

            Block blk = null;
            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if(node!=null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_McBlock r2p_McBlock = (R2P_McBlock)await session.Query(q2p_mcBlock);
                if (r2p_McBlock != null && r2p_McBlock.block!="" )
                    blk = JsonHelper.FromJson<Block>(r2p_McBlock.block);
            }

            return blk;
        }

        [MessageMethod(NetOpcode.Q2P_McBlock)]
        public static void Q2P_McBlock_Handle(Session session, int opcode, object msg)
        {
            Q2P_McBlock q2p_McBlock = msg as Q2P_McBlock;
            Block mcbkl = BlockChainHelper.GetMcBlock(q2p_McBlock.height);
            R2P_McBlock r2p_McBlock = null;
            r2p_McBlock = new R2P_McBlock() { block = mcbkl!=null?JsonHelper.ToJson(mcbkl):"" };
            session.Reply(q2p_McBlock, r2p_McBlock);
        }

        public async Task<string> QueryMcBlockHash(long height, string ipEndPoint = null)
        {
            Q2P_McBlockHash q2p_mcBlockHash = new Q2P_McBlockHash();
            q2p_mcBlockHash.ActorId = nodeManager.GetMyNodeId();
            q2p_mcBlockHash.height = height;

            Session session = null;
            if (ipEndPoint != null && ipEndPoint != "")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session != null && !session.IsConnect())
                session = null;

            string hash = "";
            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if (node != null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_McBlockHash r2p_McBlock = (R2P_McBlockHash)await session.Query(q2p_mcBlockHash);
                if (r2p_McBlock != null )
                    hash = r2p_McBlock.hash;
            }

            return hash;
        }

        [MessageMethod(NetOpcode.Q2P_McBlockHash)]
        public static void Q2P_McBlockHash_Handle(Session session, int opcode, object msg)
        {
            Q2P_McBlockHash q2q_McBlockHash = msg as Q2P_McBlockHash;
            Block mcbkl = BlockChainHelper.GetMcBlock(q2q_McBlockHash.height);
            var cons = Entity.Root.GetComponent<Consensus>();
            if (mcbkl == null && cons.transferHeight + 3 >= q2q_McBlockHash.height)
            {
                // 取最新高度
                var chain1 = BlockChainHelper.GetBlockChain(cons.transferHeight);
                while (chain1 != null)
                {
                    if (q2q_McBlockHash.height == chain1.height)
                    {
                        mcbkl = chain1.GetMcBlock();
                        break;
                    }
                    chain1 = chain1.GetMcBlockNext();
                }
            }

            R2P_McBlockHash r2p_McBlockHash = new R2P_McBlockHash() { hash = mcbkl != null ? mcbkl.hash : "" };
            if (r2p_McBlockHash.hash == "")
                r2p_McBlockHash.hash = "";

            session.Reply(q2q_McBlockHash, r2p_McBlockHash);
        }

        public async Task<List<string>> QueryBeLinkHash(string hash, string ipEndPoint = null)
        {
            Q2P_BeLinkHash q2p_BeLinkHash = new Q2P_BeLinkHash();
            q2p_BeLinkHash.ActorId = nodeManager.GetMyNodeId();
            q2p_BeLinkHash.hash = hash;

            Session session = null;
            if (ipEndPoint != null && ipEndPoint != "")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session != null && !session.IsConnect())
                session = null;

            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if (node != null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_BeLinkHash r2p_BeLinkHash = (R2P_BeLinkHash)await session.Query(q2p_BeLinkHash);
                if (r2p_BeLinkHash != null&& r2p_BeLinkHash.hashs !=null && r2p_BeLinkHash.hashs != "" )
                    return JsonHelper.FromJson<List<string>>(r2p_BeLinkHash.hashs);
            }

            return null;
        }

        [MessageMethod(NetOpcode.Q2P_BeLinkHash)]
        public static void Q2P_BeLinkHash_Handle(Session session, int opcode, object msg)
        {
            Q2P_BeLinkHash q2q_McBlockHash = msg as Q2P_BeLinkHash;
            R2P_BeLinkHash r2p_McBlockHash = new R2P_BeLinkHash();

            var consensus = Entity.Root.GetComponent<Consensus>();
            var blockMgr  = Entity.Root.GetComponent<BlockMgr>();

            Block mcbkl = blockMgr.GetBlock(q2q_McBlockHash.hash);
            double t_1max = consensus.GetRuleCount(mcbkl.height+1);
            List<Block> blks = blockMgr.GetBlock(mcbkl.height+1);
            blks = BlockChainHelper.GetRuleBlk(consensus, blks, mcbkl.hash);


            string[] hashs = blks.Select(a => a.hash).ToArray();
            r2p_McBlockHash.hashs = JsonHelper.ToJson(hashs);

            session.Reply(q2q_McBlockHash, r2p_McBlockHash);
        }


        public async Task<R2P_Sync_Height> QuerySync_Height(Q2P_Sync_Height q2p_Sync_Height, string ipEndPoint = null,float timeout=5)
        {
            q2p_Sync_Height.ActorId = nodeManager.GetMyNodeId();

            Session session = null;
            if (ipEndPoint != null && ipEndPoint != "")
                session = await networkInner.Get(NetworkHelper.ToIPEndPoint(ipEndPoint));
            if (session != null && !session.IsConnect())
                session = null;

            if (session == null)
            {
                NodeManager.NodeData node = nodeManager.GetRandomNode();
                if (node != null)
                    session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
            }

            if (session != null)
            {
                R2P_Sync_Height reply_msg = (R2P_Sync_Height)await session.Query(q2p_Sync_Height, timeout);
                return reply_msg;
            }

            return null;
        }

        [MessageMethod(NetOpcode.Q2P_Sync_Height)]
        public static void Q2P_Sync_Height_Handle(Session session, int opcode, object msg)
        {
            var consensus = Entity.Root.GetComponent<Consensus>();
            if (!consensus.openSyncFast)
            {
                return;
            }
            else
            {

                Q2P_Sync_Height q2p_Sync_Height = msg as Q2P_Sync_Height;
                R2P_Sync_Height reply_msg = new R2P_Sync_Height();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();

                Log.Info($"Q2P_Sync_Height: {session.RemoteAddress} H:{q2p_Sync_Height.height} S:{q2p_Sync_Height.spacing}");

                long max = 1 * 1024 * 1024;
                long size = 0;
                reply_msg.height = q2p_Sync_Height.height;
                long spacing = q2p_Sync_Height.height + q2p_Sync_Height.spacing;
                Dictionary<long, string> blockChains = new Dictionary<long, string>();

                for (; reply_msg.height < spacing; reply_msg.height++)
                {
                    var chain1 = BlockChainHelper.GetBlockChain(reply_msg.height);
                    if (chain1 != null)
                    {
                        blockChains.Add(chain1.height, chain1.hash);
                    }

                    List<Block> blks;
                    var blkNext = BlockChainHelper.GetMcBlock(reply_msg.height + 1);
                    if (blkNext != null)
                    {
                        blks = new List<Block>();
                        foreach (string hash in blkNext.linksblk.Values)
                        {
                            Block blk = blockMgr.GetBlock(hash);
                            if (blk != null)
                                blks.Add(blk);
                        }
                    }
                    else
                    {
                        blks = blockMgr.GetBlock(reply_msg.height);
                    }

                    for (reply_msg.handle = q2p_Sync_Height.handle; reply_msg.handle < blks.Count; reply_msg.handle++)
                    {
                        string temp = JsonHelper.ToJson(blks[reply_msg.handle]);
                        reply_msg.blocks.Add(temp);
                        size += temp.Length;
                        if (size > max)
                        {
                            reply_msg.handle++;
                            reply_msg.blockChains = JsonHelper.ToJson(blockChains);
                            session.Reply(q2p_Sync_Height, reply_msg);
                            return;
                        }
                    }
                }

                reply_msg.height = -1;
                reply_msg.blockChains = JsonHelper.ToJson(blockChains);
                session.Reply(q2p_Sync_Height, reply_msg);
            }
        }

        public static void MakeGenesis()
        {
            if (true)
            {
                WalletKey key = Wallet.GetWallet().GetCurWallet();

                // Genesis
                Block blk = new Block();
                blk.Address = key.ToAddress();
                blk.prehash = "";
                blk.height = 1;
                blk.timestamp = TimeHelper.Now();
                blk.random = RandomHelper.RandUInt64().ToString("x");

                // Transfer
                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = "";
                    transfer.addressOut = blk.Address;
                    transfer.amount = (3L * 10000L * 10000L).ToString();
                    transfer.nonce = 1;
                    transfer.type = "transfer";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(0,transfer);
                }

                // rule Consensus
                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce = 1;
                    transfer.type = "contract";
                    transfer.depend = "RuleContract_v1.0";
                    transfer.data   = "create()";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(1, transfer);
                }

                blk.hash = blk.ToHash();
                blk.sign = blk.ToSign(key);
                File.WriteAllText("./Data/genesisBlock.dat", JsonHelper.ToJson(blk));
            }

        }


    }

}