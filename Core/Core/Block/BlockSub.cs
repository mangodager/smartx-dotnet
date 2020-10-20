using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;

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
        public long   height;
        public long   timestamp;

        public override string ToString()
        {
            return $"{type}#{nonce}#{addressIn}#{addressOut}#{amount}#{data}#{depend}#{timestamp}#{sign.ToHex()}";
        }

        public string ToHash()
        {
            string temp = $"{type}#{nonce}#{addressIn}#{addressOut}#{amount}#{data}#{depend}#{timestamp}";
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
        public long index;  // transferIndex
    }

}