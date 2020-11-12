using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using XLua;
using System.IO;

namespace ETModel
{
    [LuaCallCSharp]
    public class Block
    {
        public string hash;       // 块哈希
        public string prehash;    // 前一个主块哈希 2F+1
        public string prehashmkl; // Merkle哈希
        public long   height;     // 块高度
        public string Address;    // 出块地址
        public long   timestamp;  // 时间戳
        public string random;     // 随机数
        public List<string> extend; // 扩展
        public List<string> temp;   // 

        // 使用Dictionary解决Json序列化后排序错乱问题
        public Dictionary<int,string>    linksblk  = new Dictionary<int,string>();     // 引用块哈希  2F+1
        public Dictionary<int, BlockSub> linkstran = new Dictionary<int, BlockSub>();  // 包含的交易

        public byte[]   sign;    // 裁决签名

        public override string ToString()
        {
            return this.ToStringEx();
        }

    }

    public static class Helper
    {
        static public string ToStringEx(this Block This)
        {
            string str_linksblk = "";
            string str_linkstran = "";
            string str_extend = "";

            for (int ii = 0; ii < This.linksblk.Count;ii++)
            {
                str_linksblk = $"{str_linksblk}#{This.linksblk[ii]}";
            }

            for (int ii = 0; ii < This.linkstran.Count; ii++)
            {
                str_linkstran = $"{str_linkstran}#{This.linkstran[ii].hash}";
            }

            if (This.extend != null)
            {
                str_extend += "#";
                for (int ii = 0; ii < This.extend.Count; ii++)
                {
                    str_extend = $"{str_extend}#{This.extend[ii]}";
                }
                str_extend += "#";
            }

            return $"{This.prehash}#{This.prehashmkl}#{This.height}#{This.Address}#{This.timestamp}#{str_linksblk}#{str_linkstran}{str_extend}";
        }

        static public string ToHash(this Block This, string r=null)
        {
            if (r != null && r != "")
                return CryptoHelper.Sha256((CryptoHelper.Sha256(This.ToString()) + r));
            return CryptoHelper.Sha256((CryptoHelper.Sha256(This.ToString()) + This.random));
        }

        static public string ToHashMining(this Block This)
        {
            return CryptoHelper.Sha256(This.ToString());
        }

        static public byte[] ToSign(this Block This, WalletKey key)
        {
            return Wallet.Sign(This.hash.HexToBytes(), key);
        }

        static public string ToHashMerkle(this Block This, BlockMgr blockMgr)
        {
            //List<string> list1 = new List<string>();
            //for (int ii = 0; ii < This.linksblk.Count; ii++)
            //{
            //    Block linkblk = blockMgr.GetBlock(This.linksblk[ii]);
            //    for (int jj = 0; ii < linkblk.linkstran.Count; jj++)
            //    {
            //        list1.Add(linkblk.linkstran[jj].hash);
            //    }
            //}

            //List<string> list2 = new List<string>();
            //while (list1.Count>1)
            //{
            //    if (list1.Count % 2 == 1)
            //        list1.Add(list1[list1.Count - 1]);

            //    var num = list1.Count / 2;
            //    for (int ii = 0; ii < num; ii+=2)
            //    {
            //        list2.Add(CryptoHelper.Sha256( $"{list1[ii+0]}_{list1[ii+1]}" ));
            //    }
            //    List<string> temp = list1;
            //    list1 = list2;
            //    list2 = temp;
            //    list2.Clear();
            //}

            //if (list1.Count == 1)
            //    return list1[0];

            return "";
        }

        static public Block GetHeader(this Block This)
        {
            Block blk = new Block();
            blk.hash = This.hash;
            blk.prehash = This.prehash;
            blk.prehashmkl = This.prehashmkl;
            blk.height = This.height;
            blk.Address = This.Address;
            blk.timestamp = This.timestamp;
            blk.random = This.random;
            blk.sign = This.sign;

            blk.linksblk  = new Dictionary<int, string>  (This.linksblk.Count);
            blk.linkstran = new Dictionary<int, BlockSub>(This.linkstran.Count);

            blk.linksblk.Add(This.linksblk.Count, "");
            blk.linkstran.Add(This.linkstran.Count, null);

            return blk;
        }

        static public bool AddBlockSub(this Block This, int ii, BlockSub transfer)
        {
            This.linkstran.Add(ii,transfer);
            return true;
        }

        static public bool AddBlock(this Block This, int ii,Block bb)
        {
            This.linksblk.Add(ii,bb.hash);
            return true;
        }

        static System.Numerics.BigInteger diff_max = System.Numerics.BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", System.Globalization.NumberStyles.HexNumber);
        // 获取某个块的难度
        static public double GetDiff(string hash)
        {
            System.Numerics.BigInteger bigint = System.Numerics.BigInteger.Parse("0"+hash, System.Globalization.NumberStyles.HexNumber);
            bigint = (diff_max-bigint) * 10000000000000000 / diff_max;
            return double.Parse(bigint.ToString()) / 10000000000000000;
        }

        static public double GetDiff(this Block This)
        {
            return GetDiff(This.hash);
        }

    }

}
