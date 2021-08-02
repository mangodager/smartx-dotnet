using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.IO;
using Newtonsoft.Json;

namespace ETModel
{

    public class BlockChain
    {
        public string hash;
        public long   height;
        [JsonIgnoreAttribute]
        public int    checkWeight = 0;

        public void Deserialize(BinaryReader reader)
        {
            hash = reader.ReadString();
            height = reader.ReadInt64();
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(hash??"");
            writer.Write(height);
        }
    }

    public static class BlockChainHelper
    {
        // 取某高度mc块
        static public BlockChain GetBlockChain(long height)
        {
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                return dbSnapshot.BlockChains.Get("" + height);
            }
        }

        static public Block GetMcBlock(this BlockChain chain)
        {
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            return blockMgr.GetBlock(chain.hash);
        }

        static public Block GetMcBlock(long height)
        {
            var chain = GetBlockChain(height);
            if (chain != null)
                return chain.GetMcBlock();
            return null;
        }

        static public void Apply(this BlockChain chain, DbSnapshot dbSnapshot)
        {
            dbSnapshot.BlockChains.Add("" + chain.height, chain);
        }

        static public BlockChain GetMcBlockNext2F1(this BlockChain chain, BlockMgr bm=null, Consensus cos = null)
        {
            var blockMgr  = bm  ?? Entity.Root.GetComponent<BlockMgr>();
            var consensus = cos ?? Entity.Root.GetComponent<Consensus>();
            List<Block> blks = blockMgr.GetBlock(chain.height+1);
            List<Block> blks2 = blockMgr.GetBlock(chain.height+2);
            for (int ii = blks.Count - 1; ii >= 0; ii--)
            {
                if (chain.hash != blks[ii].prehash|| !BlockChainHelper.IsIrreversible(consensus, blks[ii], blks2) )
                {
                    blks.RemoveAt(ii);
                }
            }

            if (blks.Count == 0)
                return null;

            Block mcBlk = blks[0];
            double mcWeight = GetBlockWeight(consensus, mcBlk, blks2);
            for (int ii = 1; ii<blks.Count;ii++)
            {
                Block blk = blks[ii];
                double weight = GetBlockWeight(consensus, blk, blks2);
                if (weight > mcWeight)
                {
                    mcBlk = blk;
                    mcWeight = weight;
                }
                else
                if (weight == mcWeight)
                {
                    if (blk.hash.CompareTo(mcBlk.hash) < 0)
                    {
                        mcBlk = blk;
                        mcWeight = weight;
                    }
                }
            }

            return new BlockChain() { hash = mcBlk.hash, height = mcBlk.height };
        }

        static public BlockChain GetMcBlockNext(this BlockChain chain, BlockMgr bm = null, Consensus cos = null)
        {
            var blockMgr = bm ?? Entity.Root.GetComponent<BlockMgr>();
            var consensus = cos ?? Entity.Root.GetComponent<Consensus>();

            var chinanext = GetMcBlockNext2F1(chain, blockMgr, consensus);
            if (chinanext != null)
                return chinanext;

            List<BlockChain> list1 = new List<BlockChain>();
            List<BlockChain> list2 = new List<BlockChain>();

            List<Block> blks1 = blockMgr.GetBlock(chain.height + 1);
            List<Block> blks2 = blockMgr.GetBlock(chain.height + 2);
            FindtChain(chain, blks1, blks2, ref list1, blockMgr, consensus);
            if (list1.Count == 1)
            {
                if (list1[0].checkWeight >= 1)
                    return list1[0];
            }

            //var t_2max = consensus.GetRuleCount(chain.height + 1);
            //List<Block> blks = blockMgr.GetBlock(chain.height + 1);
            //var blksRule = BlockChainHelper.GetRuleBlk(consensus, blks, chain.hash);
            //var blkAuxiliary = blksRule.Find((x) => { return x.Address == consensus.auxiliaryAddress; });
            //if (blkAuxiliary != null && blkAuxiliary.Address == consensus.auxiliaryAddress && blksRule.Count >= Math.Max(2, (BlockChainHelper.Get2F1(t_2max) / 2)))
            //{
            //    List<Block> blksTemp = blockMgr.GetBlock(chain.height + 2);
            //    if(blksTemp.Exists( (x) => { return x.prehash == blkAuxiliary.hash; } )) {
            //        return new BlockChain() { hash = blkAuxiliary.hash, height = blkAuxiliary.height };
            //    }
            //}

            return null;
        }


        static public BlockChain GetMcBlockNextNotBeLink(this BlockChain chain)
        {
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var consensus = Entity.Root.GetComponent<Consensus>();
            List<Block> blks = blockMgr.GetBlock(chain.height + 1);
            for (int ii = blks.Count - 1; ii >= 0; ii--)
            {
                if (chain.hash != blks[ii].prehash)
                {
                    blks.RemoveAt(ii);
                }
            }

            if (blks.Count == 0)
                return null;

            Block mcBlk = blks[0];
            double mcWeight = GetBlockWeight(consensus, mcBlk,null,false);
            foreach (Block blk in blks)
            {
                double weight = GetBlockWeight(consensus, blk, null,false);
                if (weight > mcWeight)
                {
                    mcBlk = blk;
                    mcWeight = weight;
                }
                else
                if (weight == mcWeight)
                {
                    if (blk.hash.CompareTo(mcBlk.hash) < 0)
                    {
                        mcBlk = blk;
                        mcWeight = weight;
                    }
                }
            }

            return new BlockChain() { hash = mcBlk.hash, height = mcBlk.height };
        }

        // Block权重公式: T2_weight = MIN（2/3*T1max，T1num） + 0.5f*(diff) + T1max*MIN（2/3*T3max，T3num）
        // 权重最大的块胜出为主块
        static public double GetBlockWeight(Consensus consensus, Block blk, List<Block> blks2 = null,bool belink=true)
        {
            if (blk.height >= 88380) // Smartx 3.1.1 Fix
            {
                if (!consensus.IsRule(blk.height, blk.Address))
                    return 0;
            }

            double t_1max = consensus.GetRuleCount(blk.height - 1);
            double t_2max = consensus.GetRuleCount(blk.height + 1);

            double t_1num = consensus.GetBlockLinkCount(blk);
            double t_2num = belink ? consensus.GetBlockBeLinkCount(blk, blks2) : 0;

            double fDiff = blk.GetDiff();

            double weight = (Math.Min( Get2F1(t_1max), t_1num)  + fDiff + (t_1max * Math.Min( Get2F1(t_2max), t_2num)));

            return weight;
        }

        public static double GetNonRollbackWeight(Consensus consensus, long height)
        {
            double t_1max = consensus.GetRuleCount(height - 1);
            double t_2max = consensus.GetRuleCount(height + 1);

            double fDiff = 0;

            double weight = Get2F1(t_1max) + fDiff + (t_1max * Get2F1(t_2max));

            return weight;
        }

        public static bool IsIrreversible(Consensus consensus, Block blk, List<Block> blks2 = null)
        {
            double t_1max = consensus.GetRuleCount(blk.height - 1);
            double t_2max = consensus.GetRuleCount(blk.height + 1);

            double t_1num = consensus.GetBlockLinkCount(blk);
            double t_2num = consensus.GetBlockBeLinkCount(blk, blks2);

            double weight1 = (Math.Min( Get2F1(t_1max), t_1num) + (t_1max * Math.Min( Get2F1(t_2max), t_2num)));

            double weight2 = Get2F1(t_1max) + (t_1max * Get2F1(t_2max));

            return weight1 >= weight2;
        }

        public static List<Block> GetRuleBlk(Consensus consensus, List<Block> blks,string prehash)
        {
            Dictionary<string, Block> ruleBlks = new Dictionary<string, Block>();
            foreach (Block bb in blks)
            {
                if (prehash == bb.prehash)
                {
                    if (bb.height >= 88380) // Smartx 3.1.1 Fix
                    {
                        if (!consensus.IsRule(bb.height, bb.Address))
                            continue;
                    }

                    //double weight1 = GetBlockWeight(consensus, bb);
                    if (!ruleBlks.TryGetValue(bb.Address, out Block bkl1))
                        ruleBlks[bb.Address] = bb;
                    if (bkl1 != null && GetBlockWeight(consensus, bkl1) < GetBlockWeight(consensus, bb))
                    {
                        ruleBlks.Remove(bb.Address);
                        ruleBlks[bb.Address] = bb;
                    }
                }
            }

            var listnew = new List<Block>();
            foreach (Block bb in ruleBlks.Values)
            {
                listnew.Add(bb);
            }

            return listnew;
        }

        public static double Get2F1(double count)
        {
            return Math.Floor(2d / 3d * count) + 1;
        }


        public static int CheckChain(BlockChain chain, List<Block> blks2 , BlockMgr blockMgr, Consensus consensus)
        {
            blks2 = blks2 ?? blockMgr.GetBlock(chain.height + 1);

            int rel = IsIrreversible(consensus, chain.GetMcBlock(), blks2) ? 2 : 0;
            var blksRule = BlockChainHelper.GetRuleBlk(consensus, blks2, chain.hash);

            var blkAuxiliary = blksRule.Find((x) => { return x.Address == consensus.auxiliaryAddress; });
            //if (blkAuxiliary != null && blkAuxiliary.Address == consensus.auxiliaryAddress && blksRule.Count >= Math.Min(2, consensus.GetRuleCount(chain.height + 1)))

            var t_2max = consensus.GetRuleCount(chain.height + 1);
            if (blkAuxiliary != null && blkAuxiliary.Address == consensus.auxiliaryAddress && blksRule.Count >= Math.Max(2,(BlockChainHelper.Get2F1(t_2max) / 2)) )
            {
                rel = rel + 1;
            }
            chain.checkWeight = rel;
            return rel;
        }

        public static List<BlockChain> FindtChain( BlockChain chainfrist, List<Block> blks, List<Block> blks2, ref List<BlockChain> list, BlockMgr blockMgr, Consensus consensus)
        {
            blks = blks ?? blockMgr.GetBlock(chainfrist.height + 1);

            for (int ii = blks.Count - 1; ii >= 0; ii--)
            {
                if (chainfrist.hash == blks[ii].prehash)
                {
                    var cc = new BlockChain() { hash = blks[ii].hash, height = blks[ii].height };
                    if (CheckChain(cc, blks2, blockMgr,consensus) > 0)
                    {
                        list.Add(cc);
                    }
                }
            }
            return list;
        }

        public static BlockChain FindtChainMost(BlockChain chainfrist, BlockMgr blockMgr, Consensus consensus = null)
        {
            blockMgr = blockMgr ?? Entity.Root.GetComponent<BlockMgr>();
            consensus = consensus ?? Entity.Root.GetComponent<Consensus>();
            BlockChain chain = chainfrist;

            List<BlockChain> list1 = new List<BlockChain>();
            List<BlockChain> list2 = new List<BlockChain>();
            List<BlockChain> listT = null;

            List<Block> blks1 = blockMgr.GetBlock(chain.height + 1);
            List<Block> blks2 = blockMgr.GetBlock(chain.height + 2);

            FindtChain(chain, blks1, blks2, ref list1, blockMgr, consensus);
            if (list1.Count <= 1) {
                return null;
            }

            long height = chain.height+1;
            while (list1.Count >= 2)
            {
                blks1 = blks2;
                blks2 = blockMgr.GetBlock(height+2);

                for (int ii = list1.Count - 1; ii >= 0; ii--)
                {
                    FindtChain(list1[ii], blks1, blks2, ref list2, blockMgr, consensus);
                }

                var exist = list2.Exists((x) => { return x.checkWeight >= 2; });
                if (exist) // 如果存在2F+1 block就删除 auxiliary block
                    list2.RemoveAll((x) => { return x.checkWeight == 1; });

                listT = list2;
                list2 = list1;
                list1 = listT;
                list2.Clear();

                height++;
            }

            if (list1.Count==1) {
                return list1[0];
            }

            return null;
        }


    }




}