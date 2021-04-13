using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using XLua;
using System.IO;
using System.Threading.Tasks;

namespace ETModel
{
    public class SyncHeightFast
    {
        NodeManager nodeManager;
        BlockMgr    blockMgr;
        Consensus   cons;
        LevelDBStore levelDBStore;

        public bool bRun = false;

        int GetFunCount = 0;
        List<long> record = new List<long>();

        public async Task<bool> Sync(Block otherMcBlk,string ipEndPoint)
        {
            nodeManager = nodeManager ?? Entity.Root.GetComponent<NodeManager>();
            blockMgr = blockMgr ?? Entity.Root.GetComponent<BlockMgr>();
            cons = cons ?? Entity.Root.GetComponent<Consensus>();
            levelDBStore = levelDBStore ?? Entity.Root.GetComponent<LevelDBStore>();

            if (cons.transferHeight >= otherMcBlk.height)
                return false;

            // 接收广播过来的主块
            // 检查链是否一致，不一致表示前一高度有数据缺失
            // 获取对方此高度主块linksblk列表,对方非主块的linksblk列表忽略不用拉取
            // 没有的块逐个拉取,校验数据是否正确,添加到数据库
            // 拉取到的块重复此过程
            // UndoTransfers到拉去到的块高度 , 有新块添加需要重新判断主块把漏掉的账本应用
            // GetMcBlock 重新去主块 ，  ApplyTransfers 应用账本
            long syncHeight = otherMcBlk.height;
            long currHeight = (int)(cons.transferHeight / 10) * 10;

            var nodes = nodeManager.GetNode(NodeManager.EnumState.openSyncFast);
            if (nodes.Length != 0)
            {
                long spacing = 10;

                int ii = 0;
                int random = RandomHelper.Random() % nodes.Length;
                while (GetFunCount <= nodes.Length)
                {
                    long h = currHeight + ii * spacing;
                    if (h > cons.transferHeight + 300)
                        break;

                    if (record.IndexOf(h) == -1)
                    {
                        Get(h, spacing, nodes[(ii + random) % nodes.Length].ipEndPoint);
                    }
                    ii++;
                }
            }

            record.RemoveAll(x => x < currHeight);

            return await cons.SyncHeightNear(otherMcBlk, nodeManager.GetRandomNode(NodeManager.EnumState.openSyncFast),1f);
        }

        async void Get(long height, long spacing, string ipEndPoint)
        {
            GetFunCount++;

            try
            {
                record.Add(height);
                // 
                Dictionary<long, string> blockChains = new Dictionary<long, string>();
                bool error = false;
                var q2p_Sync_Height = new Q2P_Sync_Height();
                q2p_Sync_Height.spacing = spacing;
                q2p_Sync_Height.height = height;
                var reply = await cons.QuerySync_Height(q2p_Sync_Height, ipEndPoint, 30);
                if (reply == null)
                    reply = await cons.QuerySync_Height(q2p_Sync_Height, nodeManager.GetRandomNode().ipEndPoint, 30);
                if (reply != null && !string.IsNullOrEmpty(reply.blockChains))
                {
                    Log.Info($"SyncHeightFast.Get AddBlock {height} {ipEndPoint}");
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
                        if (!error && reply.height != -1)
                        {
                            q2p_Sync_Height.height = reply.height;
                            q2p_Sync_Height.spacing = Math.Max(1, spacing - (reply.height - height));

                            reply = await cons.QuerySync_Height(q2p_Sync_Height, ipEndPoint);
                        }
                    }
                    while (!error && reply != null && reply.height != -1);
                }
                else
                {
                    Log.Info($"SyncHeightFast.Get Remove {height} {ipEndPoint}");
                    record.Remove(height);
                }
            }
            catch (Exception)
            {
            }

            GetFunCount--;
        }

    }


}
