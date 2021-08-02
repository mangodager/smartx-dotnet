using LevelDB;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ETModel
{

    public class WalletKey
    {
        public byte[] privatekey = new byte[64];
        public byte[] publickey  = new byte[32];
        public byte[] random;

        public string ToAddress()
        {
            return Wallet.ToAddress(publickey);
        }
    }

    public class Wallet
    {
        // smartx-wallet.json
        public class WalletJsonAddress
        {
            public string address;
            public string encrypted;
        }
        public class WalletJson
        {
            public int version = 101;
            public int curIndex = 0;
            public List<WalletJsonAddress> accounts;
        }

        static public Wallet Inst = null;
        string passwords;
        public List<WalletKey> keys = new List<WalletKey>();
        public int curIndex = 0;
        public string walletFile;

        public WalletKey GetCurWallet()
        {
            return keys[curIndex];
        }

        public void SetCurWallet(string address)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].ToAddress() == address)
                {
                    curIndex = i;
                }
            }
        }

        public static bool CheckAddress(string address)
        {
            return string.IsNullOrEmpty(address) ? false : address.Base58CheckDecode2();
        }

        public static string ToAddress(byte[] publickey)
        {
            //byte[] hash = publickey.Sha256().RIPEMD160();
            byte[] hash = CryptoHelper.Sha256(publickey.ToHexString()).ToByteArray().RIPEMD160();
            byte[] data = new byte[21];
            data[0] = 1;
            Buffer.BlockCopy(hash, 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        static public byte[] Seek()
        {
            byte[] seed = new byte[32];
            ed25519.ed25519_create_seed(seed);
            return seed;
        }

        public WalletKey Create(string input=null)
        {
            WalletKey walletKey = new WalletKey();
            input = input ?? Wallet.Inst.Input("Please enter random word: ");
            walletKey.random = CryptoHelper.Sha256(Seek().ToHexString() + "#" + input).HexToBytes();
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, walletKey.random);
            this.keys.Add(walletKey);
            return walletKey;
        }

        static public Wallet GetWallet(string walletFile = "./Data/wallet.json")
        {
            if (Inst != null)
                return Inst;
            Wallet wallet = new Wallet();
            Inst = wallet;
            wallet.walletFile = walletFile;
            //string input = "123";
            string input = wallet.Input("Please enter passwords: ");
            int ret = wallet.OpenWallet(input);
            if (ret == -1 && !string.IsNullOrEmpty(input))
            {
                string input2 = wallet.Input("Please enter your passwords again: ");
                if (input == input2)
                {
                    wallet = wallet.NewWallet(input);
                    wallet.SaveWallet();
                    return wallet;
                }
            }
            else
            if (ret == 1)
            {
                return wallet;
            }
            else
            if (ret == -2)
            {
                Log.Info($"passwords error!");
            }
            else
            if (ret == -3)
            {
                Log.Info($"version error!");
            }
            return null;
        }

        int OpenWallet(string password)
        {
            passwords = password;
            try
            {
                if (!File.Exists(walletFile))
                    return -1;

                string allText = File.ReadAllText(walletFile, System.Text.Encoding.UTF8);
                var walletJson = JsonHelper.FromJson<WalletJson>(allText);
                if (walletJson == null || walletJson.version < 101)
                    return -3;
                var aes256 = new AesEverywhere.AES256();

                for (int i = 0; i < walletJson.accounts.Count; i++)
                {
                    WalletKey walletKey = new WalletKey();
                    string base64 = aes256.Decrypt(walletJson.accounts[i].encrypted, passwords);
                    walletKey.random = base64.HexToBytes();

                    ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, walletKey.random);

                    if (walletKey.ToAddress() != walletJson.accounts[i].address)
                    {
                        return -2;
                    }

                    keys.Add(walletKey);
                }

                curIndex = walletJson.curIndex;
            }
            catch (Exception)
            {
                return -2;
            }
            return 1;
        }

        public void SaveWallet()
        {
            var walletJson = new WalletJson();
            walletJson.curIndex = curIndex;
            walletJson.accounts = new List<WalletJsonAddress>();

            var aes256 = new AesEverywhere.AES256();

            for (int i = 0; i < keys.Count; i++)
            {
                var walletJsonAddress = new WalletJsonAddress();

                walletJsonAddress.address = keys[i].ToAddress();
                walletJsonAddress.encrypted = aes256.Encrypt(keys[i].random.ToHexString(), passwords);

                walletJson.accounts.Add(walletJsonAddress);
            }

            File.WriteAllText(walletFile, JsonHelper.ToJson(walletJson), System.Text.Encoding.UTF8);
        }

        public Wallet NewWallet(string passwords)
        {
            Wallet wallet = this;
            wallet.passwords = passwords;
            wallet.Create();
            return wallet;
        }

        static public byte[] Sign(byte[] data, WalletKey key)
        {
            byte[] signature = new byte[64];
            byte[] sign = new byte[64 + 32];
            ed25519.ed25519_sign(signature, data, data.Length, key.publickey, key.privatekey);
            Buffer.BlockCopy(signature, 0, sign, 0, signature.Length);
            Buffer.BlockCopy(key.publickey, 0, sign, signature.Length, key.publickey.Length);
            return sign;
        }

        static public bool Verify(byte[] sign, string data, string address)
        {
            return Verify(sign, data.HexToBytes(), address);
        }

        static byte[] signature = new byte[64];
        static byte[] publickey = new byte[32];
        static public bool Verify(byte[] sign, byte[] data, string address)
        {
            if (!CheckAddress(address))
                return false;

            lock (signature)
            {
                Buffer.BlockCopy(sign, 0, signature, 0, signature.Length);
                Buffer.BlockCopy(sign, signature.Length, publickey, 0, publickey.Length);

                if (ed25519.ed25519_verify(signature, data, data.Length, publickey) != 1)
                    return false;

                if (ToAddress(publickey) != address)
                    return false;

                return true;
            }
        }

        // 联合签名验签
        static public bool VerifyCo(byte[] sign, byte[] data, string address)
        {
            if (!CheckAddress(address))
                return false;

            List<byte[]> signatures = new List<byte[]>();
            List<byte[]> publickeys = new List<byte[]>();
            int signLen = (64 + 32);
            byte[] publickeyCo = new byte[(int)(sign.Length / signLen) * 32 + (sign.Length - signLen * (int)(sign.Length / signLen))];
            for (int ii = 0; ii < sign.Length / signLen; ii++)
            {
                byte[] signature = new byte[64];
                byte[] publickey = new byte[32];
                Buffer.BlockCopy(sign, ii * signLen, signature, 0, signature.Length);
                Buffer.BlockCopy(sign, ii * signLen + signature.Length, publickey, 0, publickey.Length);
                Buffer.BlockCopy(sign, ii * signLen + signature.Length, publickeyCo, ii * publickey.Length, publickey.Length);

                if (ed25519.ed25519_verify(signature, data, data.Length, publickey) != 1)
                    return false;
            }

            Buffer.BlockCopy(sign, signLen * (int)(sign.Length / signLen), publickeyCo, (int)(sign.Length / signLen) * 32, (sign.Length - signLen * (int)(sign.Length / signLen)));

            if (ToAddress(publickeyCo) != address)
                return false;

            return true;
        }

        public string Input(string showtext)
        {
            //定义一个字符串接收用户输入的内容
            string input = "";

            Console.Write(showtext);

            while (true)
            {
                //存储用户输入的按键，并且在输入的位置不显示字符
                ConsoleKeyInfo ck = Console.ReadKey(true);

                //判断用户是否按下的Enter键
                if (ck.Key != ConsoleKey.Enter)
                {
                    if (ck.Key != ConsoleKey.Backspace)
                    {
                        //将用户输入的字符存入字符串中
                        input += ck.KeyChar.ToString();
                        //将用户输入的字符替换为*
                        Console.Write("*");
                    }
                    else
                    {
                        if (input.Length > 0)
                        {
                            input = input.Remove(input.Length - 1);
                            //删除错误的字符
                            Console.Write("\b \b");
                        }

                    }
                }
                else
                {
                    Console.WriteLine();

                    break;
                }
            }
            return input;
        }

        public bool IsPassword(string password)
        {
            return passwords == password;
        }

        static public void Test()
        {
            string info = "";

            for (int i = 0; i < 100; i++)
            {
                WalletKey walletKey = new WalletKey();
                walletKey.random = Seek();
                string seed = CryptoHelper.Sha256(walletKey.random.ToHexString() + "#" + "123");
                ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed.HexToBytes());

                string address = Wallet.ToAddress(walletKey.publickey);
                info += address + "\n";

            }
            Log.Info("Address    \n" + info);
        }

        static public void Test2()
        {
            WalletKey walletKey = new WalletKey();

            byte[] byteArray = "aa306f7fad8f12dad3e7b90ee15af0b39e9eccd1aad2e757de2d5ad74b42b67a".HexToBytes();
            byte[] seed32 = new byte[32];
            Buffer.BlockCopy(byteArray, 0, seed32, 0, seed32.Length);
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, seed32);


            string address = Wallet.ToAddress(walletKey.publickey);
            Log.Info("publickey  \n" + walletKey.publickey.ToHexString());
            Log.Info("privatekey \n" + walletKey.privatekey.ToHexString());
            Log.Info("Address    \n" + address);


            byte[] data = "e33b68cd7ad3dc29e623e399a46956d54c1861c5cd1e5039b875811d2ca4447d".HexToBytes();
            byte[] sign = Wallet.Sign(data, walletKey);

            Log.Info("sign \n" + sign.ToHexString());

            if (Wallet.Verify(sign, data, address))
            {
                Log.Info("Verify ok ");
            }
        }

        static public void Test3()
        {
            {
                string hexpublickey = "537007d703cedabfed8d81031f974bbb67ab82fbdfc4097bd3ceb9a01b46ff07";
                string addressOld = "0x6411c766bf61f22fe716cc51f5396a7e4279d749";
                string addressNew = Wallet.ToAddress(hexpublickey.HexToBytes());
                Log.Debug($"addressOld : {addressOld} addressNew : {addressNew} check : { CheckPulblicKeyToAddress_Old_Java_V1(addressOld, addressNew, hexpublickey) }");
            }


        }

        static public bool CheckPulblicKeyToAddress_Old_Java_V1(string addressOld, string addressNew, string hexpublickey)
        {
            var hash1 = Blake2Fast.Blake2b.ComputeHash(32, ("302a300506032b6570032100" + hexpublickey).HexToBytes()).RIPEMD160();
            string address1 = hash1.Take(20).ToHexString();
            string address2 = Wallet.ToAddress(hexpublickey.HexToBytes());

            if (address1 != addressOld.ToLower().Replace("0x", ""))
                return false;
            if (address2 != addressNew)
                return false;
            return true;
        }

        static public void Import(string privatekey, string walletFile = "./Data/walletImport.json")
        {
            var walletKey = new WalletKey();
            walletKey.random = privatekey.HexToBytes();
            ed25519.ed25519_create_keypair(walletKey.publickey, walletKey.privatekey, walletKey.random);

            var keys = new List<WalletKey>();
            keys.Add(walletKey);

            var walletJson = new WalletJson();
            walletJson.curIndex = 0;
            walletJson.accounts = new List<WalletJsonAddress>();

            var aes256 = new AesEverywhere.AES256();
            for (int i = 0; i < keys.Count; i++)
            {
                var walletJsonAddress = new WalletJsonAddress();

                walletJsonAddress.address = keys[i].ToAddress();
                walletJsonAddress.encrypted = aes256.Encrypt(keys[i].random.ToHexString(), "smartx123");

                walletJson.accounts.Add(walletJsonAddress);
            }

            File.WriteAllText(walletFile, JsonHelper.ToJson(walletJson), System.Text.Encoding.UTF8);


        }


    }

}