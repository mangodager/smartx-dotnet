using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.IO;

namespace ETModel
{

    public class TransferComponent : Component
    {
        BlockMgr     blockMgr = null;
        Consensus    consensus = null;

        protected Block blocklast = null;
        protected long  height    = 0;

        public override void Awake(JToken jd = null)
        {
        }

        public override void Start()
        {
            consensus = Entity.Root.GetComponent<Consensus>();
            blockMgr = Entity.Root.GetComponent<BlockMgr>();
        }

        public bool RefTransfer(Block newBlock)
        {
            if (blocklast != null)
            {
                if (newBlock.height > height + 4)
                {
                    height    = 0;
                    blocklast = null;
                }
                else
                {
                    var blk = blocklast;
                    Block mcblk;
                    if (newBlock.height == blocklast.height + 1)
                    {
                        mcblk = newBlock;
                    }
                    else
                    {
                        mcblk = BlockChainHelper.GetMcBlock(blk.height + 1);
                    }

                    if (mcblk!=null)
                    {
                        for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
                        {
                            if (mcblk.linksblk[ii] == blk.hash)
                            {
                                blocklast = null;
                                height = 0;
                                return false;
                            }
                        }
                    }

                    newBlock.linkstran = blk.linkstran;
                    blocklast = newBlock;
                    return true;
                }
            }
            return false;
        }

        public void OnCreateBlock(Block newBlock)
        {
            if (height==0&& newBlock.linkstran != null && newBlock.linkstran.Count != 0 )
            {
                blocklast = newBlock;
                height    = newBlock.height;
            }
        }

        public BlockSub GetTransferState(string hash)
        {
            if (blocklast != null)
            {
                var blk = blocklast;
                var linkstran = blk.linkstran;
                for (int ii = 0; ii < linkstran.Count; ii++)
                {
                    if (linkstran[ii].hash == hash)
                    {
                        return linkstran[ii];
                    }
                }
            }
            return null;
        }

        public int GetTransferCount()
        {
            if (blocklast != null)
            {
                return blocklast.linkstran.Count;
            }
            return 0;
        }
    }


}



















