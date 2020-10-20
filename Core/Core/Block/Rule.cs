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

        protected Dictionary<string, BlockSub> blockSubs = new Dictionary<string, BlockSub>();
        BlockMgr blockMgr = null;
        Consensus  consensus = null;
        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        HttpPool httpPool = null;
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
        NodeManager nodeManager = null;
        bool bRun = true;

        public override void Awake(JToken jd = null)
        {
            if (jd["Run"] != null)
                bool.TryParse(jd["Run"].ToString(), out bRun);
        }

        public override void Start()
        {
            consensus  = Entity.Root.GetComponent<Consensus>();
            blockMgr = Entity.Root.GetComponent<BlockMgr>();
            httpPool = Entity.Root.GetComponentInChild<HttpPool>();
            nodeManager = Entity.Root.GetComponent<NodeManager>();

            if(bRun)
            Run();
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
            P2P_NewBlock p2p_NewBlock = new P2P_NewBlock();
            Block blk    = null;
            Block preblk = null;
            TimePass timePass = new TimePass(1);
            var httpRule = Entity.Root.GetComponent<HttpPool>();

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
                            blk = CreateBlock(preblk);
                            diff_max = 0;
                            hashmining = blk.ToHashMining();
                            httpRule?.SetMinging(blk.height, hashmining, consensus.calculatePower.GetPower());
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
                        if(httpRule==null)
                            Mining(blk,hashmining);
                    }

                    // 广播块
                    if (blk != null && time >= broadcasttime)
                    {
                        if (httpRule != null)
                        {
                            Dictionary<string, MinerTask> miners = httpRule.GetMiner(blk.height);
                            if (miners != null)
                            {
                                double diff = miners.Values.Max(t => t.diff);
                                var miner = miners.Values.FirstOrDefault(c => c.diff == diff);
                                if(miner!=null)
                                    blk.random = miner.random;
                                httpRule?.SetMinging(blk.height, "", consensus.calculatePower.GetPower());
                            }
                        }

                        height = blk.height;
                        blk.hash = blk.ToHash();
                        blk.sign = blk.ToSign(Wallet.GetWallet().GetCurWallet());
                        blockMgr.AddBlock(blk);
                        p2p_NewBlock.block = JsonHelper.ToJson(blk);
                        p2p_NewBlock.ipEndPoint = networkInner.ipEndPoint.ToString();
                        nodeManager.Broadcast(p2p_NewBlock);
                        diff_max = 0;

                        Log.Debug($"Rule.Broadcast {blk.height} {blk.hash}");
                        calculatePower.Insert(blk);
                        blk = null;
                        hashmining = "";

                    }

                    await Task.Delay(10);
                }
                catch (Exception )
                {
                    await Task.Delay(1000);
                }
            }
        }

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
                chain2 = chain2.GetMcBlockNext();
            }

            blockMgr.DelBlockWithHeight(consensus,chain1.hash,chain1.height);
            Block blk1 = chain1.GetMcBlock();

            // 2F1
            float timestamp = TimeHelper.Now();
            double t_2max = consensus.GetRuleCount(blk1.height+1);
            List<Block> blks = blockMgr.GetBlock(blk1.height+1);
            blks = BlockChainHelper.GetRuleBlk(consensus, blks, chain1.hash);
            if(blks.Count >= BlockChainHelper.Get2F1(t_2max) )
            {
                chain2 = BlockChainHelper.GetMcBlockNextNotBeLink(chain1,timestamp, pooltime);
                if (chain2!=null)
                {
                    var blk2 = chain2.GetMcBlock();
                    double t_1max = consensus.GetRuleCount(blk2.height-1);
                    if (blk2.linksblk.Count >= BlockChainHelper.Get2F1(t_1max))
                        return blk2;
                }
            }

            // Super Address
            var blkSuper = blks.Find((x) => { return x.Address == consensus.superAddress; });
            //if (blkSuper != null && blkSuper.Address == consensus.superAddress && blks.Count >= Math.Min(2, consensus.GetRuleCount(blk1.height+1)) )
            if (blkSuper != null && blkSuper.Address == consensus.superAddress && blks.Count >= Math.Max(2, (BlockChainHelper.Get2F1(t_2max) / 2)))
            {
                return blkSuper;
            }

            return blk1;
        }

        public Dictionary<string, BlockSub> GetTransfers()
        {
            return blockSubs;
        }

        public int AddTransfer(BlockSub transfer)
        {
            if (!consensus.IsRule(height, Wallet.GetWallet().GetCurWallet().ToAddress()))
                return -1;

            transfer.hash = transfer.ToHash();

            if (!Wallet.Verify(transfer.sign, transfer.hash, transfer.addressIn))
                return -2;

            if(BigInt.Less(transfer.amount,"0",false))
                return -3;
            Account account = null;
            using (var snapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
            {
                account = snapshot.Accounts.Get(transfer.addressIn);
            }
            if(account == null)
                return -4;

            if(BigInt.Less(account.amount, transfer.amount,false))
                return -5;

            lock (blockSubs)
            {
                // 出块权限
                if (blockSubs.Count>6000)
                    return -6;

                blockSubs.Remove(transfer.hash);
                blockSubs.Add(transfer.hash, transfer);
            }
            return 1;
        }

        public Block CreateBlock(Block preblk)
        {
            if(preblk==null)
                return null;

            blockMgr.DelBlockWithHeight(consensus,preblk.hash, preblk.height);

            Block myblk = new Block();
            myblk.Address    = Wallet.GetWallet().GetCurWallet().ToAddress();
            myblk.prehash    = preblk != null ? preblk.hash : "";
            myblk.timestamp  = nodeManager.GetNodeTime() / 1000;
            myblk.height     = preblk !=null ? preblk.height+1 : 1;
            myblk.random     = System.Guid.NewGuid().ToString("N").Substring(0, 16);

            //引用上一周期的连接块
            List<Block> blks = blockMgr.GetBlock(preblk.height);
            blks = BlockChainHelper.GetRuleBlk(consensus, blks, preblk.prehash);
            for ( int ii = 0; ii < blks.Count; ii++)
            {
                myblk.AddBlock(ii, blks[ii]);
            }
            RefTransfer(myblk);
            myblk.prehashmkl = myblk.ToHashMerkle(blockMgr);

            return myblk;
        }

        //引用当前周期交易
        public bool RefTransfer(Block blk)
        {
            try
            {
                lock (blockSubs)
                {
                    var list = blockSubs.Values.ToList<BlockSub>();
                    list.Sort((BlockSub a, BlockSub b) => {
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
                    blockSubs.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
                blockSubs.Clear();
            }
            return true;
        }

        public int GetTransfersCount()
        {
            return blockSubs.Count;
        }

        double diff_max  = 0;
        public void Mining(Block blk,string hashmining)
        {
            string random = System.Guid.NewGuid().ToString("N").Substring(0, 16);
            string hash   = CryptoHelper.Sha256(hashmining + random);

            double diff = Helper.GetDiff(hash);
            if (diff > diff_max)
            {
                diff_max   = diff;
                blk.hash   = hash;
                blk.random = random;
            }

        }


        [MessageMethod(NetOpcode.Q2P_Transfer)]
        public static void P2P_Transfer_Handle(Session session, int opcode, object msg)
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






















