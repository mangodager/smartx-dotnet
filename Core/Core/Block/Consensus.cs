﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;

namespace ETModel
{
    public class RuleInfo
    {
        public string Address;
        public string Contract;
        public string Amount;
        public long   Start;
        public long   End;
        public long   LBH;
    }

    // 在裁决时间点向其他裁决服广播视图, 到达2/3的视图视为决议，广播2/3的决议
    public class Consensus : Component
    {
        public CalculatePower calculatePower = new CalculatePower(1L);
        public string auxiliaryAddress = "";
        public string consAddress = "";
        public string SatswapFactory = "";
        public string ERCSat = "";
        public string PledgeFactory = "";
        public string LockFactory = "";

        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        BlockMgr blockMgr = Entity.Root.GetComponent<BlockMgr>();
        NodeManager nodeManager = Entity.Root.GetComponent<NodeManager>();
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
        Rule rule = null;
        public bool transferShow = true; // 强制打开防止发送重复交易
        public bool openSyncFast = true;
        public bool bRun = true;
        public bool autoMergeChain = true;

        public override void Awake(JToken jd = null)
        {
            try
            {
                //if (jd["transferShow"] != null)
                //    Boolean.TryParse(jd["transferShow"]?.ToString(), out transferShow);
                //Log.Info($"Consensus.transferShow = {transferShow}");
                if (jd["openSyncFast"] != null)
                    Boolean.TryParse(jd["openSyncFast"]?.ToString(), out openSyncFast);
                Log.Info($"Consensus.openSyncFast = {openSyncFast}");

                if (jd["autoMergeChain"] != null) {
                    Boolean.TryParse(jd["autoMergeChain"]?.ToString(), out autoMergeChain);
                }
                Log.Info($"Consensus.autoMergeChain = {autoMergeChain}");

                if (jd["Run"] != null)
                    bool.TryParse(jd["Run"].ToString(), out bRun);

                ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
                componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);

                string genesisText = File.ReadAllText("./Data/genesisBlock.dat");
                Block genesisblk = JsonHelper.FromJson<Block>(genesisText);
                auxiliaryAddress = genesisblk.Address;

                try
                {
                consAddress    = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.depend == "RuleContract_v1.0"));
                SatswapFactory = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.depend == "SatswapFactory"));
                ERCSat         = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.depend == "ERCSat"));
                PledgeFactory  = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.depend == "PledgeFactory"));
                LockFactory    = LuaVMEnv.GetContractAddress(genesisblk.linkstran.Values.First((x) => x.depend == "LockFactory"));
                }
                catch(Exception)
                {
                }

                long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out transferHeight);
                if (transferHeight == 0)
                {
                    if (true)
                    {
                        blockMgr.AddBlock(genesisblk);
                        ApplyGenesis(genesisblk);

                        Log.Info($"RuleContract   : {consAddress}");
                        Log.Info($"SatswapFactory : {SatswapFactory}");
                        Log.Info($"ERCSat         : {ERCSat}");
                        Log.Info($"PledgeFactory  : {PledgeFactory}");
                        Log.Info($"LockFactory    : {LockFactory}");
                    }
                }

                //debug
                using (DbSnapshot snapshot = levelDBStore.GetSnapshot(0,true))
                {
                    LuaVMScript luaVMScript = new LuaVMScript() { script = FileHelper.GetFileData("./Data/Contract/RuleContract_curr.lua").ToByteArray(), tablName = "RuleContract_curr" };
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

                ////string aa = BigInt.Div("1000,1000","1000");
                //if(NodeManager.networkIDBase == "alpha_1.5.3")
                //{
                //    var hash = CryptoHelper.Sha256(FileHelper.GetFileData("./Data/Contract/RuleContract_curr.lua").ToByteArray()).ToHexString();
                //    if(hash!= "abfe80b6bb9baeae8e56e8128eee991929515b7c2ce6da0764287ce6727d8611")
                //    {
                //        bRun = false;
                //        Log.Warning("./Data/Contract/RuleContract_curr.lua Version mismatch!!!");
                //        Log.Warning("Consensus.Run Stop!!!");
                //        throw new Exception("RuleContract_curr.lua Version mismatch!");
                //    }
                //}
                syncHeightFast.Awake(jd);
            }
            catch (Exception e)
            {
                Log.Error(e);
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
            if (height > 2243600) // 2022/5/27 POW Reduce production
            {
                long half = height / (2050560L * 2); // 4*60*24*356 * 2 , half life of 2 year
                long reward = 4L;
                while (half-- > 0)
                {
                    reward = reward / 2;
                }
                return Math.Max(reward, 1L);
            }
            else
            {
                long half = height / (2050560L * 2); // 4*60*24*356 * 2 , half life of 2 year
                long reward = 72L;
                while (half-- > 0)
                {
                    reward = reward / 2;
                }
                return Math.Max(reward, 1L);
            }
        }

        static public long GetRewardRule(long height)
        {
            if (height > 2254900) // 2022/5/27 POS Reduce production
            {
                long half = height / (2050560L * 2); // 4*60*24*356 , half life of 2 year
                long reward = 2L;
                while (half-- > 0)
                {
                    reward = reward / 2;
                }
                return Math.Max(reward, 1L);
            }
            else
            if (height > 345600)
            {
                long half = height / (2050560L * 2); // 4*60*24*356 , half life of 2 year
                long reward = 12L;
                while (half-- > 0)
                {
                    reward = reward / 2;
                }
                return Math.Max(reward, 1L);
            }
            else
            {
                long half = height / (2050560L * 2); // 4*60*24*356 , half life of 2 year
                long reward = 4L;
                while (half-- > 0)
                {
                    reward = reward / 2;
                }
                return Math.Max(reward, 1L);
            }
        }

        static public string GetRewardRule_2022_06_06(long height)
        {
            if (height > 2311931) // 2022/06/06 POS RATE
            {
                long half = height / (2050560L * 2); // 4*60*24*356 , half life of 2 year
                string reward = "2.3";
                while (half-- > 0)
                {
                    reward = BigHelper.Div(reward, "2");
                }
                return BigHelper.Max(reward, "1");
            }
            else
            {
                return GetRewardRule(height).ToString();
            }
        }

        static public string GetRewardRuleRate(long height)
        {
            if (height > 2311931) // 2022/06/06 POS RATE
            {
                return "0.87";
            }
            return "0.975";
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
            if (string.IsNullOrEmpty(target.hash))
                return false;

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

        public RuleInfo GetRule(long height, string Address)
        {
            RuleInfo info = null;
            if (GetRule(height).TryGetValue(Address, out info))
            {
                if (info.Start <= height && (height < info.End || info.End == -1) && info.Address == Address)
                {
                    return info;
                }
            }
            return null;
        }

        public ConcurrentDictionary<long, Dictionary<string, RuleInfo>> cacheRule = new ConcurrentDictionary<long, Dictionary<string, RuleInfo>>();
        public Dictionary<string, RuleInfo> GetRule(long height)
        {
            if (cacheRule.TryGetValue(height - 1, out Dictionary<string, RuleInfo> value) && value != null)
                return value;

            long target = cacheRule.Keys.FirstOrDefault( (x) => { return Math.Abs(x - height) <= 5; }  );
            if (target != 0)
            {
                if(cacheRule[target]!=null)
                    return cacheRule[target];
                else
                if (target == transferHeight)
                    return null;
                else
                    return GetRule(transferHeight);
            }

            if (transferHeight!=height&&Math.Abs(transferHeight - height)<= 5)
                return GetRule(transferHeight);

            using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
            {
                //Log.Debug($"GetRule {height}");
                string str = snapshot.Get($"Rule_{height}");
                if (!string.IsNullOrEmpty(str))
                {
                    var ruleInfo = JsonHelper.FromJson<Dictionary<string, RuleInfo>>(str);
                    if (ruleInfo != null) {
                        cacheRule.TryRemove(height, out Dictionary<string, RuleInfo> tempdel);
                        cacheRule.TryAdd(height, ruleInfo);
                        return ruleInfo;
                    }
                }
                else
                {
                    cacheRule.TryAdd(height, null);
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

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshot(0,true))
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
            calculatePower.InsertLink(mcblk, blockMgr);
            //Log.Debug($"ApplyHeight={blockChain.height}, {calculatePower.GetPower()}, {mcblk.GetDiff()}");

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshotUndo(blockChain.height))
            {
                if (!cacheRule.TryGetValue(mcblk.height-1, out Dictionary<string, RuleInfo> ruleInfosLast)||ruleInfosLast==null)
                {
                    var temp = dbSnapshot.Get($"Rule_{mcblk.height - 1}");
                    ruleInfosLast = JsonHelper.FromJson<Dictionary<string, RuleInfo>>(temp);
                    cacheRule.TryRemove(mcblk.height, out Dictionary<string, RuleInfo> tempdel1);
                    cacheRule.TryAdd(mcblk.height-1, ruleInfosLast);
                }

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
                            linkblk.linkstran[jj].height = 0;
                            if (!ApplyTransferFee(dbSnapshot, linkblk.linkstran[jj], linkblk.height, linkblk))
                                continue;
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

                cacheRule.TryRemove(mcblk.height, out Dictionary<string, RuleInfo> tempdel2);
                cacheRule.TryAdd(mcblk.height, ruleInfos);
            }
            return true;
        }

        public bool ApplyTransfer(DbSnapshot dbSnapshot, BlockSub transfer, long height)
        {
            if (transfer.type != "transfer"){
                return true;
            }
            do
            {
                if (transfer.addressIn == transfer.addressOut) {
                    LuaVMStack.Add2TransferTemp("Transfer address error", transfer);
                    break;
                }
                if (BigHelper.Less(transfer.amount, "0", true)) {
                    LuaVMStack.Add2TransferTemp("Transfer amount Less 0", transfer);
                    break;
                }

                if (transfer.extend != null)
                {
                    var strlastHeight = transfer.extend.Find(x => x.IndexOf("deadline:") == 0);
                    if (!string.IsNullOrEmpty(strlastHeight))
                    {
                        var array = strlastHeight.Split(":");
                        if (array.Length == 2 && long.TryParse(array[1], out long deadline))
                        {
                            if (deadline < height) {
                                LuaVMStack.Add2TransferTemp($"Transfer deadline error", transfer);
                                break;
                            }
                        }
                    }

                    if (height >= 88380) // Smartx 3.1.1 Fix
                    {
                        var unique = transfer.extend.Find(x => x.IndexOf("unique") == 0);
                        if (!string.IsNullOrEmpty(unique) && !string.IsNullOrEmpty(transfer.data))
                        {
                            string hasht = dbSnapshot.Get($"unique_{transfer.data}");
                            if (!string.IsNullOrEmpty(hasht))
                                break;
                        }
                    }


                }

                //if (!transfer.CheckSign() && height != 1)
                //    return true;

                if (height != 1)
                {
                    Account accountIn = dbSnapshot.Accounts.Get(transfer.addressIn);
                    if (accountIn == null){
                        LuaVMStack.Add2TransferTemp("Transfer addressIn error", transfer);
                        break;
                    }
                    if (BigHelper.Less(accountIn.amount, transfer.amount, false)) {
                        LuaVMStack.Add2TransferTemp("Transfer accountIn.amount is not enough", transfer);
                        break;
                    }
                    if (accountIn.nonce + 1 != transfer.nonce) {
                        LuaVMStack.Add2TransferTemp("Transfer nonce error", transfer);
                        break;
                    }
                    accountIn.amount = BigHelper.Sub(accountIn.amount, transfer.amount);
                    accountIn.nonce += 1;
                    dbSnapshot.Accounts.Add(accountIn.address, accountIn);
                    if (transferShow)
                    {
                        dbSnapshot.BindTransfer2Account(transfer.addressIn, transfer.hash);
                        if (!string.IsNullOrEmpty(transfer.data)&&transfer.data.Length==transfer.hash.Length)
                        {
                            dbSnapshot.Add($"unique_{transfer.data}", transfer.hash);
                        }
                    }
                }

                Account accountOut = dbSnapshot.Accounts.Get(transfer.addressOut) ?? new Account() { address = transfer.addressOut, amount = "0", nonce = 0 };
                accountOut.amount = BigHelper.Add(accountOut.amount, transfer.amount);
                dbSnapshot.Accounts.Add(accountOut.address, accountOut);

                if (transferShow)
                {
                    dbSnapshot.BindTransfer2Account(transfer.addressOut, transfer.hash);
                    transfer.height = height;
                }
            }
            while (false);

            if (transferShow&&(transfer.height != 0 || dbSnapshot.Transfers.Get(transfer.hash) == null))
            {
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
            var amountRuletolat  = GetRewardRule_2022_06_06(mcblk.height);
            var amountRuleCons   = BigHelper.Mul(amountRuletolat, GetRewardRuleRate(mcblk.height));
            var amountRuleRate   = BigHelper.Sub(amountRuletolat, amountRuleCons);
            var timestamp  = mcblk.timestamp;

            BlockSub Reward = null;
            BlockSub Reward_RuleCons = null;
            BlockSub Reward_RuleRate = null;
            if (transferShow)
            {
                Reward_RuleCons = new BlockSub() { hash = "", type = "Reward_RCons", timestamp = timestamp, amount = amountRuleCons };
                Reward_RuleCons.hash = Reward_RuleCons.ToHash();
                Reward_RuleCons.height = mcblk.height;

                Reward_RuleRate = new BlockSub() { hash = "", type = "Reward_RRate", timestamp = timestamp, amount = amountRuleRate };
                Reward_RuleRate.hash = Reward_RuleRate.ToHash();
                Reward_RuleRate.height = mcblk.height;

                Reward = new BlockSub() { hash = "", type = "Reward_Mc", timestamp = timestamp, amount = amount };
                Reward.hash = Reward.ToHash();
                Reward.height = mcblk.height;

                dbSnapshot.Transfers.Add(Reward_RuleCons.hash, Reward_RuleCons);
                dbSnapshot.Transfers.Add(Reward_RuleRate.hash, Reward_RuleRate);
                dbSnapshot.Transfers.Add(Reward.hash, Reward);
            }

            // rule奖励
            int ruleCount = 0;
            for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
            {
                Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                if (linkblk != null && IsRule(mcblk.height, linkblk.Address))
                {
                    ruleCount++;
                    string ruleAddress = linkblk.Address;
                    var rule = GetRule(mcblk.height, linkblk.Address);
                    if (rule != null && !string.IsNullOrEmpty(rule.Contract) ) {
                        ruleAddress = rule.Contract;
                    }
                    {   // Reward_RuleCons
                        Account linkAccount = dbSnapshot.Accounts.Get(ruleAddress) ?? new Account() { address = ruleAddress, amount = "0", nonce = 0 };
                        linkAccount.amount = BigHelper.Add(linkAccount.amount, amountRuleCons);
                        dbSnapshot.Accounts.Add(linkAccount.address, linkAccount);
                        if (transferShow)
                        {
                            dbSnapshot.BindTransfer2Account(linkAccount.address, Reward_RuleCons.hash);
                        }
                    }
                    {   // amountRuleRate
                        Account linkAccount = dbSnapshot.Accounts.Get(linkblk.Address) ?? new Account() { address = linkblk.Address, amount = "0", nonce = 0 };
                        linkAccount.amount = BigHelper.Add(linkAccount.amount, amountRuleRate);
                        dbSnapshot.Accounts.Add(linkAccount.address, linkAccount);
                        if (transferShow)
                        {
                            dbSnapshot.BindTransfer2Account(linkAccount.address, Reward_RuleRate.hash);
                        }
                    }
                }
            }

            // 出块奖励
            Account account = dbSnapshot.Accounts.Get(mcblk.Address) ?? new Account() { address = mcblk.Address, amount = "0", nonce = 0 };
            account.amount = BigHelper.Add(account.amount, amount);
            dbSnapshot.Accounts.Add(account.address, account);

            if (transferShow)
            {
                dbSnapshot.BindTransfer2Account(account.address, Reward.hash);
            }
        }

        LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
        public bool ApplyContract(DbSnapshot dbSnapshot, BlockSub transfer, long height)
        {
            if (transfer.type != "contract") {
                return true;
            }

            do
            {
                if (transfer.addressIn == transfer.addressOut) {
                    LuaVMStack.Add2TransferTemp("Transfer address error", transfer);
                    break;
                }
                //if (transfer.data == null || transfer.data == "")
                //    return true;
                //if (!transfer.CheckSign())
                //    return true;

                // 设置交易index
                Account accountIn = dbSnapshot.Accounts.Get(transfer.addressIn) ?? new Account() { address = transfer.addressIn, amount = "0", nonce = 0 };
                if (accountIn.nonce + 1 != transfer.nonce) {
                    LuaVMStack.Add2TransferTemp("Transfer nonce error", transfer);
                    break;
                }
                accountIn.nonce += 1;
                dbSnapshot.Accounts.Add(accountIn.address, accountIn);

                transfer.height = height;
                bool rel = luaVMEnv.Execute(dbSnapshot, transfer, height, out object[] reslut);

                if (transferShow)
                {
                    if (rel)
                    {
                        var consAddressNew = LuaVMEnv.GetContractAddress(transfer);
                        if (transfer.addressOut == "" && transfer.depend.IndexOf("ERC20") == -1)
                        {
                            dbSnapshot.BindTransfer2Account($"{transfer.addressIn}", transfer.hash);
                        }
                        dbSnapshot.BindTransfer2Account($"{transfer.addressIn}{consAddressNew}", transfer.hash);
                    }
                    else
                    {
                        transfer.height = 0;
                    }
                }
            }
            while (false);

            if (transferShow && (transfer.height != 0 || dbSnapshot.Transfers.Get(transfer.hash) == null))
            {
                dbSnapshot.Transfers.Add(transfer.hash, transfer);
            }
            return true;
        }

        // take off fee
        public bool ApplyTransferFee(DbSnapshot dbSnapshot, BlockSub transfer, long height,Block blk)
        {
            // fix 2043114
            if (height >= 2043114)
            {
                if (transfer.addressIn == "cpFCgjrDXKjceWw7JCQ27zCXRx9ti62JS")
                {
                    if (transfer.addressOut != "hAJGVuSB9BvsK8tN4c6iRTQMvKW19KBGH")
                    {
                        return false;
                    }
                }
                if (transfer.addressIn == "QhK1rw5quaKNPj1L7MhPRFkgWXdxgKqfn")
                {
                    if(transfer.addressOut != "hAJGVuSB9BvsK8tN4c6iRTQMvKW19KBGH")
                    {
                        return false;
                    }
                }
            }

            if (height != 1)
            {
                if (dbSnapshot.Transfers.Get(transfer.hash) != null)
                {
                    //// 节点重复打包已经存在交易将被扣手续费
                    //Account accountIn = dbSnapshot.Accounts.Get(blk.Address);
                    //if (accountIn == null)
                    //    return false;
                    //if (BigHelper.Less(accountIn.amount, "0.002", false))
                    //    return false;
                    //accountIn.amount = BigHelper.Sub(accountIn.amount, "0.002");
                    //dbSnapshot.Accounts.Add(accountIn.address, accountIn);
                    return false;
                }
                else
                {
                    do
                    {
                        // 正常扣除手续费
                        Account accountIn = dbSnapshot.Accounts.Get(transfer.addressIn);
                        if (accountIn == null) {
                            LuaVMStack.Add2TransferTemp("amount Less 0.002", transfer);
                            break;
                        }
                        if (BigHelper.Less(accountIn.amount, "0.002", false)) {
                            LuaVMStack.Add2TransferTemp("amount Less 0.002", transfer);
                            break;
                        }
                        accountIn.amount = BigHelper.Sub(accountIn.amount, "0.002");
                        dbSnapshot.Accounts.Add(accountIn.address, accountIn);

                        Account blkAccount = dbSnapshot.Accounts.Get(blk.Address);
                        if (blkAccount != null)
                        {
                            blkAccount.amount = BigHelper.Add(blkAccount.amount, "0.002");
                            dbSnapshot.Accounts.Add(blkAccount.address, blkAccount);
                        }
                        return true;
                    }
                    while (false);

                    if (transferShow)
                    {
                        dbSnapshot.Transfers.Add(transfer.hash, transfer);
                    }
                }
            }
            return false;
        }

        public long transferHeight    = 0;
        List<P2P_NewBlock> newBlocks  = new List<P2P_NewBlock>();
        TimePass bifurcatedReportTime = new TimePass(15);
        TimePass cacheRuleClearTime   = new TimePass(15);

        //// 记录transferHeight多长时间没变化了
        //long  transferHeightLast = -1;
        //float transferHeightTime = TimeHelper.time;

        public async void Run()
        {
            await Task.Delay(2000);

            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);

            Log.Debug($"Consensus.Run at height {transferHeight}");

            blockMgr.DelBlockWithHeight(this,transferHeight);
            // 恢复到停机前的高度
            ApplyBlockChain();

            // 算力统计
            calculatePower.Clear();
            for (long ii = Math.Max(1, transferHeight - calculatePower.statistic); ii <= transferHeight; ii++)
            {
                calculatePower.InsertLink(BlockChainHelper.GetMcBlock(ii), blockMgr);
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
                                ApplyBlockChain();
                            }
                        }
                    }
                    ApplyBlockChain();

                    lock (this)
                    {
                        runAction?.Invoke();
                        runAction = null;
                    }

                    if (bifurcatedReportTime.IsPassSet() && bifurcatedReport != null)
                    {
                        var outputstr = $"find a branch chain. height: {bifurcatedReport.height} address: {bifurcatedReport.Address} , rules count: {bifurcatedReport.linksblk.Count}";
                        Log.Info(outputstr);

                        AutoMergeChain();

                        bifurcatedReport = null;
                    }

                    if (cacheRuleClearTime.IsPassSet())
                    {
                        cacheRule.TryGetValue(transferHeight, out Dictionary<string, RuleInfo> ruleInfo);
                        cacheRule.Clear();
                        if (ruleInfo != null)
                        {
                            cacheRule.TryAdd(transferHeight, ruleInfo);
                        }
                    }
                    
                    if (newBlocks.Count == 0)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        await Task.Delay(1);
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

        // 解决分叉链
        public BlockChain ApplyBlockChainMost(long targetHeight = -1)
        {
            long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            var chain1     = BlockChainHelper.GetBlockChain(transferHeight);
            var chain2     = BlockChainHelper.FindtChainMost(chain1, blockMgr, this);

            if (chain2 == null && transferHeight > 1670000)
            {
                var chainkNext = chain1.GetMcBlockNext(blockMgr, this);
                if (chainkNext != null)
                {
                    chain2 = BlockChainHelper.FindtChainMost(chainkNext, blockMgr, this);
                }
            }

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

        public SyncHeightFast syncHeightFast = new SyncHeightFast();
        public Block bifurcatedReport = null;
        async Task<bool> SyncHeight(Block otherMcBlk, string ipEndPoint)
        {
            if (transferHeight + 20 < otherMcBlk.height)
            {
                return await syncHeightFast.Sync(otherMcBlk, ipEndPoint);
            }
            var rel = await SyncHeightNear(otherMcBlk, ipEndPoint,13);
            return rel;
        }

        public async Task<bool> SyncHeightNear(Block otherMcBlk, string ipEndPoint,int spacing, float timeOut=5)
        {
            if (transferHeight >= otherMcBlk.height)
                return true;

            long.TryParse(levelDBStore.Get("Snap___2F1Height"), out long l2F1Height);
            l2F1Height = Math.Max(1, l2F1Height);
            string hash2F1 = await QueryMcBlockHash(l2F1Height, ipEndPoint);
            BlockChain chain2F1 = BlockChainHelper.GetBlockChain(l2F1Height);
            if (chain2F1 != null && chain2F1.hash != hash2F1)
            {
                if (bifurcatedReport == null && !string.IsNullOrEmpty(hash2F1))
                    bifurcatedReport = otherMcBlk;
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
                string hash = await QueryMcBlockHash(jj, ipEndPoint, timeOut);
                if (hash != "" && (chain == null || chain.hash != hash))
                {
                    continue;
                }
                break;
            }

            //Log.Info($"SyncHeight: {currHeight} to {syncHeight}");
            bool bUndoHeight = false;
            long ii = currHeight;
            bool isContinue = true;
            for (; ii < syncHeight + 1 && ii < currHeight + spacing; ii++)
            {
                Block syncMcBlk = null;
                if (ii == otherMcBlk.height - 1) // 同步prehash， 先查
                {
                    syncMcBlk = blockMgr.GetBlock(otherMcBlk.prehash) ?? await QueryMcBlock(ii, ipEndPoint, timeOut);
                }
                if (ii == otherMcBlk.height)
                {
                    syncMcBlk = otherMcBlk;
                }
                if (syncMcBlk == null)
                {
                    string hash = await QueryMcBlockHash(ii, ipEndPoint, timeOut);
                    if (string.IsNullOrEmpty(hash))
                        break;
                    syncMcBlk = blockMgr.GetBlock(hash) ?? await QueryMcBlock(ii, ipEndPoint, timeOut);
                }

                if (syncMcBlk == null)
                    break;

                if (!AddBlockFlag(syncMcBlk))
                    break;

                for (int jj = 0; jj < syncMcBlk.linksblk.Count; jj++)
                {
                    Block queryBlock = blockMgr.GetBlock(syncMcBlk.linksblk[jj]) ?? await QueryBlock(syncMcBlk.linksblk[jj], ipEndPoint, timeOut);
                    if (queryBlock == null)
                    {
                        isContinue = false;
                        break;
                    }
                    if (!AddBlockFlag(queryBlock))
                    {
                        isContinue = false;
                        break;
                    }
                }
                if(!isContinue)
                    break;

                var beLinks = await QueryBeLinkHash(syncMcBlk.hash, ipEndPoint, timeOut);
                if (beLinks != null)
                {
                    for (int jj = 0; jj < beLinks.Count; jj++)
                    {
                        Block queryBlock = blockMgr.GetBlock(beLinks[jj]) ?? await QueryBlock(beLinks[jj], ipEndPoint, timeOut);
                        if (queryBlock == null)
                        {
                            isContinue = false;
                            break;
                        }
                        if (!AddBlockFlag(queryBlock))
                        {
                            isContinue = false;
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }

                if (!isContinue)
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
                if (currHeight == transferHeight)
                {
                    levelDBStore.UndoTransfers(currHeight - 1);
                }
                long.TryParse(levelDBStore.Get("UndoHeight"), out transferHeight);
            }

            if (ii < syncHeight + 1)
                return false;

            return true;
        }

        bool AddBlockFlag(Block blk,bool b = true)
        {
            if (blk != null)
            {
                if(blk.temp==null)
                    blk.temp = new List<string>();
                blk.temp.Remove("syncFlag");
                if (b)
                {
                    blk.temp.Add("syncFlag");
                }
                return blockMgr.AddBlock(blk,true);
            }
            return false;
        }

        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;
            if (!NodeManager.CheckNetworkID(p2p_Block.networkID))
                return;

            if (string.IsNullOrEmpty(p2p_Block.ipEndPoint))
                return;

            var newBlock = new P2P_NewBlock() { block = p2p_Block.block, networkID = NodeManager.networkIDBase, ipEndPoint = p2p_Block.ipEndPoint };
            newBlocks.RemoveAll((x) => { return x.ipEndPoint == newBlock.ipEndPoint; });
            newBlocks.Add(newBlock) ;
        }

        public async Task<string> QueryPrehashmkl(long syncHeight, string ipEndPoint = null, float timeOut = 5)
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
                R2P_Prehashmkl r2p_Prehashmkl = (R2P_Prehashmkl)await session.Query(q2p_PreHash, timeOut);
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

        public async Task<Block> QueryBlock(string hash, string ipEndPoint = null, float timeOut = 5)
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
                R2P_Block r2p_Block = (R2P_Block)await session.Query(q2p_Block, timeOut);
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

        public async Task<Block> QueryMcBlock(long height, string ipEndPoint=null,float timeOut=5)
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
                R2P_McBlock r2p_McBlock = (R2P_McBlock)await session.Query(q2p_mcBlock, timeOut);
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

        public async Task<string> QueryMcBlockHash(long height, string ipEndPoint = null, float timeOut = 5)
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
                R2P_McBlockHash r2p_McBlock = (R2P_McBlockHash)await session.Query(q2p_mcBlockHash, timeOut);
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

        public async Task<List<string>> QueryBeLinkHash(string hash, string ipEndPoint = null, float timeOut = 5)
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
                R2P_BeLinkHash r2p_BeLinkHash = (R2P_BeLinkHash)await session.Query(q2p_BeLinkHash, timeOut);
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
                Q2P_Sync_Height q2p_Sync_Height = msg as Q2P_Sync_Height;
                R2P_Sync_Height reply_msg = new R2P_Sync_Height();
                reply_msg.height = -1;
                reply_msg.blockChains = "";
                session.Reply(q2p_Sync_Height, reply_msg);
                return;
            }
            else
            {

                Q2P_Sync_Height q2p_Sync_Height = msg as Q2P_Sync_Height;
                R2P_Sync_Height reply_msg = new R2P_Sync_Height();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();

#if !RELEASE
                Log.Info($"Q2P_Sync_Height: {session.RemoteAddress} H:{q2p_Sync_Height.height} S:{q2p_Sync_Height.spacing}");
#else
                Log.Info($"Q2P_Sync_Height: {q2p_Sync_Height.height} S:{q2p_Sync_Height.spacing}");
#endif

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

                    List<Block> blks = blockMgr.GetBlock(reply_msg.height);
                    var blkNext = BlockChainHelper.GetMcBlock(reply_msg.height + 1);
                    if (blkNext != null)
                    {
                        blks = new List<Block>();
                        foreach (string hash in blkNext.linksblk.Values)
                        {
                            Block blk = blockMgr.GetBlock(hash);
                            if (blk!=null && blks.IndexOf(blk)==-1)
                                blks.Add(blk);
                        }
                    }

                    for (reply_msg.handle = q2p_Sync_Height.handle; reply_msg.handle < blks.Count; reply_msg.handle++)
                    {
                        string temp = JsonHelper.ToJson(blks[reply_msg.handle]);
                        reply_msg.blocks.Add(temp);
                        size += temp.Length;
                        if (size > max)
                        {
                            reply_msg.handle++;
                            reply_msg.blockChains = blockChains.Count != 0 ? JsonHelper.ToJson(blockChains) : "";
                            session.Reply(q2p_Sync_Height, reply_msg);
                            return;
                        }
                    }
                }

                reply_msg.height = -1;
                reply_msg.blockChains = blockChains.Count!=0?JsonHelper.ToJson(blockChains):"";
                session.Reply(q2p_Sync_Height, reply_msg);
            }
        }

        public void AutoMergeChain()
        {
            if (!autoMergeChain) {
                return;
            }

            // auxiliary Ruler be MergeChain
            if (auxiliaryAddress == Wallet.GetWallet().GetCurWallet().ToAddress())
            {
                MergeChain();
            }
            else
            // other Ruler
            if (transferHeight + 20 < bifurcatedReport.height)
            {
                // find auxiliary block
                var hashs_1 = levelDBStore.Heights.Get(bifurcatedReport.height.ToString());
                Block auxiliaryBlk = null;
                for (int ii = 0; ii < hashs_1.Count; ii++)
                {
                    var linkBlock = blockMgr.GetBlock(hashs_1[ii]);
                    if (linkBlock != null && linkBlock.Address == auxiliaryAddress)
                    {
                        auxiliaryBlk = linkBlock;
                        break;
                    }
                }
                if (auxiliaryBlk != null)
                {
                    MergeChain();
                }
            }
        }

        public void MergeChain()
        {
            long min = transferHeight - 6;
            long max = min + 13;

            Log.Warning($"Auto MergeChain {max} {min}");

            transferHeight = min;
            Entity.Root.GetComponent<LevelDBStore>().UndoTransfers(min);
            for (long ii = max; ii > min; ii--)
            {
                Entity.Root.GetComponent<BlockMgr>().DelBlock(ii);
            }

            Log.Warning("Auto MergeChain finish");
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

                int index = 0;

                // Transfer
                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = "";
                    transfer.addressOut = blk.Address;
                    transfer.amount = BigHelper.Sub("7000000000","3000000");
                    transfer.nonce = index;
                    transfer.type = "transfer";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce = index;
                    transfer.type = "contract";
                    transfer.depend = "ERCSat";
                    transfer.data = "create(\"SAT\",\"SAT\",\"0\")";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce = index;
                    transfer.type = "contract";
                    transfer.depend = "PledgeFactory";
                    transfer.data = "create(\"PledgeFactory\",\"RPF\")";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce = index;
                    transfer.type = "contract";
                    transfer.depend = "SatswapFactory";
                    transfer.data = "create(\"SatswapFactory\",\"SSF\")";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                {
                    BlockSub transfer = new BlockSub();
                    transfer.addressIn = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce = index;
                    transfer.type = "contract";
                    transfer.depend = "LockFactory";
                    transfer.data = "create(\"LockFactory\",\"LKF\")";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                var ERCSat = LuaVMEnv.GetContractAddress(blk.linkstran.Values.First((x) => x.depend == "ERCSat"));
                var Pledge = LuaVMEnv.GetContractAddress(blk.linkstran.Values.First((x) => x.depend == "PledgeFactory"));
                {
                    BlockSub transfer   = new BlockSub();
                    transfer.addressIn  = blk.Address;
                    transfer.addressOut = Pledge;
                    transfer.amount = "0";
                    transfer.nonce  = index;
                    transfer.type   = "contract";
                    transfer.depend = "";
                    transfer.data   = $"pairCreated(\"{ERCSat}\",\"3000000\")";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash   = transfer.ToHash();
                    transfer.sign   = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                // rule Consensus
                {
                    BlockSub transfer   = new BlockSub();
                    transfer.addressIn  = blk.Address;
                    transfer.addressOut = "";
                    transfer.amount = "0";
                    transfer.nonce  = index;
                    transfer.type   = "contract";
                    transfer.depend = "RuleContract_v1.0";
                    transfer.data   = "create()";
                    transfer.timestamp = blk.timestamp;
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(key);
                    blk.AddBlockSub(index++, transfer);
                }

                //var LockFactory = LuaVMEnv.GetContractAddress(blk.linkstran.Values.First((x) => x.depend == "LockFactory")); ;
                //{
                //    BlockSub transfer   = new BlockSub();
                //    transfer.addressIn  = blk.Address;
                //    transfer.addressOut = LockFactory;
                //    transfer.amount = "0";
                //    transfer.nonce = 6;
                //    transfer.type = "contract";
                //    transfer.depend = "";
                //    transfer.data =  $"approve(\"ez55m4D86AtG4foGBqaDy5z4JwjbNNHVn\",\"{ERCSat}\",\"10000\",\"10\",\"Lock10\")";
                //    transfer.timestamp = blk.timestamp;
                //    transfer.hash = transfer.ToHash();
                //    transfer.sign = transfer.ToSign(key);
                //    blk.AddBlockSub(6, transfer);
                //}

                blk.hash = blk.ToHash();
                blk.sign = blk.ToSign(key);
                File.WriteAllText("./Data/genesisBlock.dat", JsonHelper.ToJson(blk));
            }

        }


    }

}