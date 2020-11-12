using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace ETModel
{

    public class BlockSub
    {
        public string hash;
        public string type;
        public long   nonce; // nonce
        public string addressIn;
        public string addressOut;
        public string amount;
        public string data;
        public byte[] sign;
        public string depend;
        public long   gas;
        public long   height;
        public long   timestamp;
        public string remarks;
        public List<string> extend; // 扩展
        public List<string> temp;   // 

        public override string ToString()
        {
            string str_extend = "";
            if (extend != null)
            {
                str_extend += "#";
                for (int ii = 0; ii < extend.Count; ii++)
                {
                    str_extend = $"{str_extend}#{extend[ii]}";
                }
                str_extend += "#";
            }
            return $"{type}#{nonce}#{addressIn}#{addressOut}#{amount}#{data}#{depend}#{timestamp}#{remarks}{str_extend}{sign.ToHex()}";
        }

        public string ToHash()
        {
            string str_extend = "";
            if (extend != null)
            {
                str_extend += "#";
                for (int ii = 0; ii < extend.Count; ii++)
                {
                    str_extend = $"{str_extend}#{extend[ii]}";
                }
                str_extend += "#";
            }
            string temp = $"{type}#{nonce}#{addressIn}#{addressOut}#{amount}#{data}#{depend}#{timestamp}#{remarks}{str_extend}";
            return CryptoHelper.Sha256(temp);
        }

        public byte[] ToSign(WalletKey key)
        {
            return Wallet.Sign(hash.HexToBytes(), key);
        }

        public bool CheckSign()
        {
            return Wallet.Verify(sign, hash.HexToBytes(), addressIn);
        }

    }

    public class Account
    {
        public string address;
        public string amount;
        public long nonce;
    }

}