using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class BlockMgr : Component
    {
        NodeManager  nodeManager  = Entity.Root.GetComponent<NodeManager>();
        LevelDBStore levelDBStore = Entity.Root.GetComponent<LevelDBStore>();

        public override void Awake(JToken jd = null)
        {

        }

        public override void Start()
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);


        }

        public bool AddBlock(Block blk)
        {
            if (blk != null && GetBlock(blk.hash) == null)
            {
                if (!Entity.Root.GetComponent<Consensus>().Check(blk))
                    return false;
                using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
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
            using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
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
            using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
            {
                List<string> list = snapshot.Heights.Get(height.ToString());
                if (list != null)
                {
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        snapshot.Blocks.Delete(list[ii]);
                        cacheDict.Remove(list[ii]);
                        cacheList.Remove(list[ii]);
                    }
                    snapshot.Heights.Delete(height.ToString());
                }
                snapshot.Commit();
            }
        }

        public void DelBlockWithHeight(Consensus consensus, string prehash,long perheight)
        {
            using (DbSnapshot snapshot = levelDBStore.GetSnapshot())
            {
                List<Block> blks = GetBlock(perheight + 1);
                blks = BlockChainHelper.GetRuleBlk(consensus, blks, prehash);

                List<string> list = snapshot.Heights.Get((perheight + 1).ToString());
                if (list != null)
                {
                    for (int ii = list.Count - 1; ii >= 0; ii--)
                    {
                        if (blks.Find(x => x.hash == list[ii]) == null)
                        {
                            snapshot.Blocks.Delete(list[ii]);
                            cacheDict.Remove(list[ii]);
                            cacheList.Remove(list[ii]);
                            list.RemoveAt(ii);
                        }
                    }
                    snapshot.Heights.Add((perheight + 1).ToString(), list);
                    snapshot.Commit();
                }
            }
        }

        Dictionary<string, Block> cacheDict = new Dictionary<string, Block>();
        List<string> cacheList = new List<string>();

        public Block GetBlock(string hash)
        {
            if (hash==null||hash=="")
                return null;

            if (cacheDict.TryGetValue(hash, out Block currValue))
                return currValue;

            currValue = levelDBStore.Blocks.Get(hash);
            if (currValue != null)
            {
                cacheDict.Add(hash, currValue);
                cacheList.Insert(cacheList.Count, hash);

                if (cacheList.Count > 1000)
                {
                    cacheDict.Remove(cacheList[0]);
                    cacheList.RemoveAt(0);
                }
            }
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

        public long newBlockHeight = 0;
        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;
            Block blk = JsonHelper.FromJson<Block>(p2p_Block.block);
            //Log.Debug($"NewBlock IP:{session.RemoteAddress.ToString()} hash:{blk.hash} ");
            Log.Debug($"NewBlock:{blk.Address} H:{blk.height} prehash:{blk.prehash}");

            newBlockHeight = blk.height;
            // 有高度差的直接忽略
            long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);
            if (transferHeight - 1 < blk.height)
            {
                AddBlock(blk);
            }

            // 如果收到的是桶外的数据 , 向K桶内进行一次广播
            if (nodeManager.IsNeedBroadcast2Kad(p2p_Block.ipEndPoint))
            {
                p2p_Block.ipEndPoint = Entity.Root.GetComponent<ComponentNetworkInner>().ipEndPoint.ToString();
                nodeManager.Broadcast2Kad(p2p_Block);
            }

        }



    }


}









