using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;

namespace ETModel
{
    public class Rule : Component
    {
        public CalculatePower calculatePower = new CalculatePower();

        protected Queue<Dictionary<string, BlockSub>> blockSubQueue = new Queue<Dictionary<string, BlockSub>>();
        protected Dictionary<string, BlockSub> blockSubs = new Dictionary<string, BlockSub>();

        BlockMgr blockMgr = null;
        Consensus  consensus = null;
        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        RelayNetwork relayNetwork = Entity.Root.GetComponentInChild<RelayNetwork>();
        HttpPool     httpRule = null;
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
        NodeManager  nodeManager = null;
        TransferComponent transferComponent = null;

        bool bRun = true;

        public override void Awake(JToken jd = null)
        {
            if (jd["Run"] != null)
                bool.TryParse(jd["Run"].ToString(), out bRun);
        }

        public override void Start()
        {
            consensus    = Entity.Root.GetComponent<Consensus>();
            blockMgr     = Entity.Root.GetComponent<BlockMgr>();
            httpRule     = Entity.Root.GetComponent<HttpPool>();
            nodeManager  = Entity.Root.GetComponent<NodeManager>();
            relayNetwork = Entity.Root.GetComponentInChild<RelayNetwork>();
            transferComponent = Entity.Root.GetComponentInChild<TransferComponent>();

            if (bRun) {
                Run();
            }
        }

        static public long pooltime = 15;
        static public long broadcasttime = pooltime * 10 / 15;
        public long height   = 1;
        public string hashmining = "";
        public int state = 0;

        public async void Run()
        {
            //Entity.Root.GetComponent<LevelDBStore>().Undo(100);
            await Task.Delay(3000);

            long.TryParse(levelDBStore.Get("UndoHeight"), out height);

            Log.Debug($"Rule.Run at height {height}");

            Wallet wallet = Wallet.GetWallet();
            P2P_NewBlock p2p_NewBlock = new P2P_NewBlock() { networkID = NodeManager.networkIDBase };
            Block blk    = null;
            Block preblk = null;
            TimePass timePass = new TimePass(1);
            bool bBlkMining = true;

            if(httpRule==null)
                broadcasttime = pooltime * 5 / 15; // 不挖矿就提前广播块

            state = 0xff;
            while (true)
            {
                try
                {
                    if (state < 0xff)
                    {
                        await Task.Delay(1000);
                    }

                    long time = (nodeManager.GetNodeTime() / 1000) % pooltime;
                    if (blk == null && time < broadcasttime && timePass.IsPassSet() )
                    {
                        preblk = GetLastMcBlock();
                        if (consensus.IsRule(preblk.height + 1, wallet.GetCurWallet().ToAddress()))
                        {
                            blk = CreateBlock(preblk,ref bBlkMining);
                            if (blk != null&& bBlkMining)
                            {
                                diff_max = 0;
                                hashmining = blk.ToHashMining();
                                httpRule?.SetMinging(blk.height, hashmining, consensus.calculatePower.GetPower());
                            }
                        }
                        else
                        {
                            long.TryParse(levelDBStore.Get("UndoHeight"), out height);
                            await Task.Delay(1000);
                        }
                    }

                    if (blk != null && httpRule == null )
                    {
                        // 挖矿
                        if (httpRule == null&& bBlkMining)
                        {
                            Mining(blk, hashmining);
                        }
                    }

                    // 广播块
                    if (blk != null && time >= broadcasttime)
                    {
                        if (bBlkMining)
                        { 
                            if (httpRule != null)
                            {
                                Dictionary<string, MinerTask> miners = httpRule.GetMiner(blk.height);
                                if (miners != null)
                                {
                                    double diff = miners.Values.Max(t => t.diff);
                                    var miner = miners.Values.FirstOrDefault(c => c.diff == diff);
                                    if (miner != null && miner.random != null && miner.random.Count > 0) {
                                        blk.random = miner.random[miner.random.Count - 1];
                                    }
                                    httpRule.SetMinging(blk.height, "", consensus.calculatePower.GetPower());
                                }
                            }
                            height = blk.height;
                            blk.hash = blk.ToHash();
                            blk.sign = blk.ToSign(Wallet.GetWallet().GetCurWallet());
                            blockMgr.AddBlock(blk);
                        }

                        p2p_NewBlock.block = JsonHelper.ToJson(blk);
                        p2p_NewBlock.ipEndPoint = networkInner.ipEndPoint.ToString();
                        nodeManager.Broadcast(p2p_NewBlock, blk);
                        relayNetwork?.Broadcast(p2p_NewBlock);

                        Log.Debug($"Rule.Broadcast H:{blk.height} Mining:{bBlkMining} hash:{blk.hash} T:{blk.linkstran.Count}");

                        calculatePower.Insert(blk);
                        hashmining = "";
                        diff_max   = 0;
                        blk = null;

                    }

                    await Task.Delay(10);
                }
                catch (Exception )
                {
                    //Log.Debug(e.ToString());
                    await Task.Delay(1000);
                }
            }
        }

        TimePass DelBlockWithHeightTime = new TimePass(7f);

        // 获取最新高度MC块
        public Block GetLastMcBlock()
        {
            var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();

            // 取最新高度
            long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);
            var chain1 = BlockChainHelper.GetBlockChain(transferHeight);
            var chain2 = chain1.GetMcBlockNext();
            while (chain2 != null)
            {
                chain1 = chain2;
                if(chain1.height>= transferHeight+10)
                    chain2 = null;
                else
                    chain2 = chain1.GetMcBlockNext();
            }

            if (DelBlockWithHeightTime.IsPassSet())
            {
                blockMgr.DelBlockWithHeight(consensus, chain1.height);
            }

            Block blk1 = chain1.GetMcBlock();

            // 2F1
            double t_2max = consensus.GetRuleCount(blk1.height+1);
            List<Block> blks = blockMgr.GetBlock(blk1.height+1);
            blks = BlockChainHelper.GetRuleBlk(consensus, blks, chain1.hash);
            if(blks.Count >= BlockChainHelper.Get2F1(t_2max) )
            {
                chain2 = BlockChainHelper.GetMcBlockNextNotBeLink(chain1);
                if (chain2!=null)
                {
                    var blk2 = chain2.GetMcBlock();
                    double t_1max = consensus.GetRuleCount(blk2.height-1);
                    if (blk2.linksblk.Count >= BlockChainHelper.Get2F1(t_1max))
                        return blk2;
                }
            }

            // Auxiliary Address
            var blkAuxiliary = blks.Find((x) => { return x.Address == consensus.auxiliaryAddress; });
            //if (blkAuxiliary != null && blkAuxiliary.Address == consensus.auxiliaryAddress && blks.Count >= Math.Min(2, consensus.GetRuleCount(blk1.height+1)) )
            if (blkAuxiliary != null && blkAuxiliary.Address == consensus.auxiliaryAddress && blks.Count >= Math.Max(2, (BlockChainHelper.Get2F1(t_2max) / 2)))
            {
                return blkAuxiliary;
            }

            return blk1;
        }

        public Dictionary<string, BlockSub> GetTransfers()
        {
            return blockSubs;
        }

        public bool IsTransferFull(bool checkFull)
        {
            if(checkFull) {
                return blockSubQueue.Count >= 10;
            }
            else {
                return blockSubQueue.Count >= 12;
            }
        }

        public int AddTransfer(BlockSub transfer,bool checkFull=true)
        {
            transfer.hash = transfer.ToHash();

            if (!Wallet.Verify(transfer.sign, transfer.hash, transfer.addressIn))
                return -2;

            Account account = null;
            using (var snapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
            {
                account = snapshot.Accounts.Get(transfer.addressIn);

                if (snapshot.Transfers.Get(transfer.hash) != null)
                    return -9;
            }

            if (account == null)
                return -4;

            if(BigHelper.Less(account.amount, "0.002", false))
                return -3;

            if (transfer.type == "transfer")
            {
                if (BigHelper.Less(account.amount, BigHelper.Add(transfer.amount, "0.002"), false))
                    return -5;

                if (!BigHelper.Equals( BigHelper.Round8(transfer.amount) , transfer.amount))
                    return -6;

                if(!Wallet.CheckAddress(transfer.addressOut))
                    return -10;
            }
            else
            {

            }

            if(transfer.addressIn==transfer.addressOut)
                return -8;

            var length = JsonHelper.ToJson(transfer).Length;
            if (length > 1024 * 4) {
                return -11;
            }

            if (!consensus.IsRule(height, Wallet.GetWallet().GetCurWallet().ToAddress()))
                return -1;

            lock (blockSubs)
            {
                if (blockSubs.Count >= 600)
                {
                    blockSubQueue.Enqueue(blockSubs);
                    blockSubs = new Dictionary<string, BlockSub>();
                }

                if (IsTransferFull(checkFull)) {
                    return -1;
                }

                blockSubs.Remove(transfer.hash);
                blockSubs.Add(transfer.hash, transfer);
            }

            return 1;
        }

        public Block CreateBlock(Block preblk,ref bool bBlkMining)
        {
            if(preblk==null)
                return null;
            string address = Wallet.GetWallet().GetCurWallet().ToAddress();

            Block myblk = new Block();
            myblk.Address    = Wallet.GetWallet().GetCurWallet().ToAddress();
            myblk.prehash    = preblk != null ? preblk.hash : "";
            myblk.timestamp  = nodeManager.GetNodeTime();
            myblk.height     = preblk !=null ? preblk.height+1 : 1;
            myblk.random     = System.Guid.NewGuid().ToString("N").Substring(0, 16);

            //引用上一周期的连接块
            var blks1 = blockMgr.GetBlock(preblk.height);
            var blks2 = BlockChainHelper.GetRuleBlk(consensus, blks1, preblk.prehash);
            for ( int ii = 0; ii < blks2.Count; ii++)
            {
                myblk.AddBlock(ii, blks2[ii]);
            }

            // 比较相同高度的出块
            var blks3 = blockMgr.GetBlock(myblk.height);
            var blklast = blks3.Find(x => (x.Address == address && x.prehash == myblk.prehash && x.linksblk.Count >= myblk.linksblk.Count  ) );
            if (blklast != null)
            {
                bBlkMining = false;
                return blklast;
            }

            CalculatePower.SetDT(myblk,preblk, httpRule);

            if (transferComponent != null)
            {
                if (!transferComponent.RefTransfer(myblk))
                {
                    RefTransfer(myblk);
                }
            }
            else
            {
                RefTransfer(myblk);
            }
            transferComponent?.OnCreateBlock(myblk);

            myblk.prehashmkl = myblk.ToHashMerkle(blockMgr);

            bBlkMining = true;

            //Log.Debug($"Rule.CreateBlock {myblk.height} linksblk:{myblk.linksblk.Count} linkstran:{myblk.linkstran.Count} blks1:{blks1.Count} blks3:{blks3.Count}");

            return myblk;
        }

        //引用当前周期交易
        public bool RefTransfer(Block blk)
        {
            Dictionary<string, BlockSub> blockSubTemp = null;
            try
            {
                lock (blockSubs)
                {
                    blockSubTemp = blockSubQueue.Count > 0 ? blockSubQueue.Dequeue() : blockSubs;
                    var list = blockSubTemp.Values.ToList<BlockSub>();
                    list.Sort((BlockSub a, BlockSub b) =>
                    {
                        int rel = a.nonce.CompareTo(b.nonce);
                        if (rel == 0)
                            rel = a.hash.CompareTo(b.hash);
                        return rel;
                    });
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        if (list[ii] != null)
                        {
                            blk.AddBlockSub(ii, list[ii]);
                        }
                    }
                    blockSubTemp.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
                lock (blockSubs){
                    blockSubTemp?.Clear();
                }
            }
            return true;
        }

        public int GetTransfersCount()
        {
            int transferComponentCount = transferComponent!=null?transferComponent.GetTransferCount():0;
            return blockSubs.Count + blockSubQueue.Count*600 + transferComponentCount;
        }

        public BlockSub GetTransferState(string hash)
        {
            BlockSub tran = null;
            if (blockSubs.TryGetValue(hash, out tran))
            {
                return tran;
            }
            else
            {
                foreach (var subs in blockSubQueue)
                {
                    if (subs.TryGetValue(hash, out tran))
                    {
                        return tran;
                    }
                }
            }

            tran = transferComponent?.GetTransferState(hash);

            return tran;
        }

        double diff_max  = 0;
        public void Mining(Block blk,string hashmining)
        {
            string random = System.Guid.NewGuid().ToString("N").Substring(0, 16);
            //string hash   = CryptoHelper.Sha256(hashmining + random);
            string hash = BlockDag.ToHash(blk.height, hashmining, random);

            double diff = Helper.GetDiff(hash);
            if (diff > diff_max)
            {
                diff_max   = diff;
                blk.hash   = hash;
                blk.random = random;
            }

        }


        [MessageMethod(NetOpcode.Q2P_Transfer)]
        public static void Q2P_Transfer_Handle(Session session, int opcode, object msg)
        {
            Q2P_Transfer q2p_Transfer = msg as Q2P_Transfer;
            BlockSub transfer = JsonHelper.FromJson<BlockSub>(q2p_Transfer.transfer);

            R2P_Transfer r2p_Transfer = new R2P_Transfer() {rel= "-10000" };
            if (transfer.CheckSign())
            {
                r2p_Transfer.rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer).ToString();
            }
            session.Reply(q2p_Transfer, r2p_Transfer);
        }

    }
    

}






















