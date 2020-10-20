using System;
using System.Linq;
using System.Security.Cryptography;

namespace ETModel
{
    public class Crypto
    {
        public static readonly Crypto Default = new Crypto();

        public static byte[] Hash160(byte[] message)
        {
            return message.Sha256().RIPEMD160();
        }

        public static byte[] Hash256(byte[] message)
        {
            return message.Sha256().Sha256();
        }

    }
}
