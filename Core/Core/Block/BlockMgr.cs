using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class BlockMgr : Component
    {
        NodeManager  nodeManager  = Entity.Root.GetComponent<NodeManager>();
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
        BlockMgrCache blockCache  = new BlockMgrCache();

        public override void Awake(JToken jd = null)
        {

        }

        public override void Start()
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);


        }
        public bool AddBlock(Block blk,bool replace=false)
        {
            if (blk != null && (replace || GetBlock(blk.hash) == null))
            {
                if (!Entity.Root.GetComponent<Consensus>().Check(blk))
                {
                    Log.Warning($"Block Check Error: {blk.ToStringEx()}");
                    return false;
                }
                using (DbSnapshot snapshot = levelDBStore.GetSnapshot(0,true))
                {
                    List<string> list = snapshot.Heights.Get(blk.height.ToString());
                    if (list == null)
                        list = new List<string>();
                    list.Remove(blk.hash);
                    list.Add(blk.hash);

                    snapshot.Heights.Add(blk.height.ToString(), list);
                    snapshot.Blocks.Add(blk.hash, blk);
                    snapshot.Commit();
                }
            }
            return true;
        }

        public void DelBlock(string hash)
        {
            using (DbSnapshot snapshot = levelDBStore.GetSnapshot(0, true))
            {
                Block blk = snapshot.Blocks.Get(hash);
                if (blk != null)
                {
                    List<string> list = snapshot.Heights.Get(blk.height.ToString());
                    if (list != null)
                    {
                        list.Remove(blk.hash);
                        snapshot.Heights.Add(blk.height.ToString(), list);
                    }
                }
                snapshot.Blocks.Delete(hash);
                snapshot.Commit();
            }
        }


        public void DelBlock(long height)
        {
            using (DbSnapshot snapshot = levelDBStore.GetSnapshot(0, true))
            {
                List<string> list = snapshot.Heights.Get(height.ToString());
                if (list != null)
                {
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        snapshot.Blocks.Delete(list[ii]);
                        blockCache.Remove(list[ii]);
                    }
                    snapshot.Heights.Delete(height.ToString());
                }
                snapshot.Commit();
            }
        }

        public Block GetBlock(string hash)
        {
            if (hash==null||hash=="")
                return null;

            if (blockCache.Get(hash, out Block currValue))
                return currValue;

            currValue = levelDBStore.Blocks.Get(hash);
            blockCache.Add(currValue);
            return currValue;
        }

        // 获取某个高度的所有块
        public List<Block> GetBlock(long height)
        {
            List<Block>  blks = new List<Block>();

            List<string> list = levelDBStore.Heights.Get(height.ToString());
            if (list != null)
            {
                foreach (string hash in list)
                {
                    Block blk = GetBlock(hash);
                    if (blk != null)
                        blks.Add(blk);
                    else
                        Log.Info($"not found {hash}");
                }
            }
            return blks;
        }

        public void DelBlockWithHeight(Consensus consensus, long start)
        {
            //Log.Info($"DelBlockWithHeight {start} {start + 4}");

            using (DbSnapshot snapshot = levelDBStore.GetSnapshot(0,true))
            {
                bool bCommit = false;
                for (long height = start; height <= start + 4; height++)
                {
                    List<string> list = snapshot.Heights.Get(height.ToString());
                    if (list==null||list.Count <= 100)
                        continue;

                    var blks = GetBlock(height);
                    var blksMap = new Dictionary<string, Block>();
                    var ruleBlks = consensus.GetRule(height);
                    foreach (var ruleinfo in ruleBlks.Values)
                    {
                        for (int ii = 0; ii < blks.Count; ii++)
                        {
                            if (blksMap.TryGetValue(blks[ii].Address, out Block blkr))
                            {
                                if (blkr.timestamp > blks[ii].timestamp)
                                {
                                    blksMap.Remove(blks[ii].Address);
                                    blksMap.Add(blks[ii].Address, blks[ii]);
                                }
                                else if (blkr.timestamp == blks[ii].timestamp && blkr.hash.CompareTo(blks[ii].hash) < 0)
                                {
                                    blksMap.Remove(blks[ii].Address);
                                    blksMap.Add(blks[ii].Address, blks[ii]);
                                }
                            }
                            else
                            {
                                blksMap.Add(blks[ii].Address, blks[ii]);
                            }
                        }
                    }

                    var blkRules = blksMap.Values.ToList();
                    if (list != null)
                    {
                        for (int ii = blks.Count - 1; ii >= 0; ii--)
                        {
                            bool syncFlag = false;
                            if(blks[ii].temp != null)
                                syncFlag = blks[ii].temp.Find(x=>x=="SyncFlag") != null;

                            if (blks.Find(x => x.hash == list[ii] ) == null && !syncFlag)
                            {
                                list.RemoveAt(ii);
                                bCommit = true;
                            }
                        }
                        snapshot.Heights.Add(height.ToString(), list);
                    }
                }
                if (bCommit)
                {
                    snapshot.Commit();
                }
            }
        }

        public long   newBlockHeight = 0;
        TimePass temppass = new TimePass(5);
        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;
            Block blk = JsonHelper.FromJson<Block>(p2p_Block.block);

            if (!NodeManager.CheckNetworkID(p2p_Block.networkID))
            {
                //Log.Warning($"NewBlk:{blk.Address} H:{blk.height} ipEndPoint:{p2p_Block.ipEndPoint} RemoteAddress:{session.RemoteAddress}");
                Log.Warning($"NewBlk:{blk.Address} H:{blk.height}");
                return;
            }
#if !RELEASE
            //Log.Debug($"NewBlk:{blk.Address} H:{blk.height} ipEndPoint:{p2p_Block.ipEndPoint} RemoteAddress:{session.RemoteAddress} hash:{blk.hash}");
            Log.Debug($"NewBlk:{blk.Address} H:{blk.height} Pre:{blk.prehash} T:{blk.linkstran.Count}");
#else
            if (temppass.IsPassSet()) {
                Log.Debug($"NewBlk:{blk.Address} H:{blk.height} Pre:{blk.prehash} T:{blk.linkstran.Count}");
            }
#endif

            newBlockHeight = blk.height;
            // 有高度差的直接忽略
            long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);
            if (transferHeight - 1 < blk.height)
            {
                AddBlock(blk);
            }

            //// 如果收到的是桶外的数据 , 向K桶内进行一次广播
            //if (nodeManager.IsNeedBroadcast2Kad(p2p_Block.ipEndPoint))
            //{
            //    p2p_Block.ipEndPoint = Entity.Root.GetComponent<ComponentNetworkInner>().ipEndPoint.ToString();
            //    nodeManager.Broadcast2Kad(p2p_Block);
            //}

        }



    }


}









