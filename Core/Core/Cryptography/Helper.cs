using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ETModel
{
    public static class CryptoHelper
    {
        private static ThreadLocal<SHA256> _sha256 = new ThreadLocal<SHA256>(() => SHA256.Create());
        private static ThreadLocal<RIPEMD160Managed> _ripemd160 = new ThreadLocal<RIPEMD160Managed>(() => new RIPEMD160Managed());

        internal static byte[] AES256Decrypt(this byte[] block, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(block, 0, block.Length);
                }
            }
        }

        internal static byte[] AES256Encrypt(this byte[] block, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(block, 0, block.Length);
                }
            }
        }

        internal static byte[] AesDecrypt(this byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using (Aes aes = Aes.Create())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform decryptor = aes.CreateDecryptor(key, iv))
                {
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        internal static byte[] AesEncrypt(this byte[] data, byte[] key, byte[] iv)
        {
            if (data == null || key == null || iv == null) throw new ArgumentNullException();
            if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentException();
            using (Aes aes = Aes.Create())
            {
                aes.Padding = PaddingMode.None;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(key, iv))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public static byte[] Base58CheckDecode(this string input)
        {
            byte[] buffer = Base58.Decode(input);
            if (buffer.Length < 4) throw new FormatException();
            byte[] checksum = buffer.Sha256(0, buffer.Length - 4).Sha256();
            if (!buffer.Skip(buffer.Length - 4).SequenceEqual(checksum.Take(4)))
                throw new FormatException();
            var ret = buffer.Take(buffer.Length - 4).ToArray();
            Array.Clear(buffer, 0, buffer.Length);
            return ret;
        }

        public static bool Base58CheckDecode2(this string input)
        {
            byte[] buffer = null;
            try
            {
                buffer = Base58.Decode(input);
                byte[] buffer2 = new byte[buffer.Length - 4];
                Buffer.BlockCopy(buffer, 0, buffer2, 0, buffer2.Length);

                if (buffer.Length < 4) throw new FormatException();
                string hash1    = CryptoHelper.Sha256(buffer2.ToHexString());
                byte[] checksum = CryptoHelper.Sha256(hash1).HexToBytes();
                if (!buffer.Skip(buffer.Length - 4).SequenceEqual(checksum.Take(4)))
                    return false;
                //var ret = buffer.Take(buffer.Length - 4).ToArray();
                //Array.Clear(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception )
            {
            }
            return false;
        }

        public static string Base58CheckEncode(this byte[] data)
        {
            string hash1 = CryptoHelper.Sha256(data.ToHexString());
            byte[] checksum = CryptoHelper.Sha256(hash1).HexToBytes();
            byte[] buffer = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Buffer.BlockCopy(checksum, 0, buffer, data.Length, 4);
            var ret = Base58.Encode(buffer);

            //Log.Info("Base58CheckEncodedata " + data.ToHexString() );
            //Log.Info("Base58CheckEncode1 " + hash1);
            //Log.Info("Base58CheckEncode2 " + CryptoHelper.Sha256(hash1));
            //Log.Info("Base58CheckEncode3 " + buffer.ToHexString() );
            //Log.Info("Base58CheckEncoderet " + ret );

            Array.Clear(buffer, 0, buffer.Length);
            return ret;
        }

        public static string Base58CheckEncodeOld(this byte[] data)
        {
            byte[] checksum = data.Sha256().Sha256();
            byte[] buffer = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Buffer.BlockCopy(checksum, 0, buffer, data.Length, 4);
            var ret = Base58.Encode(buffer);
            Array.Clear(buffer, 0, buffer.Length);
            return ret;
        }


        public static byte[] RIPEMD160(this byte[] value)
        {
            return _ripemd160.Value.ComputeHash(value.ToArray());
        }

        public static byte[] Sha256(this byte[] value)
        {
            return _sha256.Value.ComputeHash(value);
        }

        public static string Sha256(string value)
        {
            return _sha256.Value.ComputeHash( Encoding.UTF8.GetBytes(value) ).ToHexString();
        }

        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            return _sha256.Value.ComputeHash(value, offset, count);
        }

        internal static byte[] ToAesKey(this string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(passwordHash, 0, passwordHash.Length);
                return passwordHash2;
            }
        }

        internal static byte[] ToAesKey(this SecureString password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = password.ToArray();
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(passwordHash, 0, passwordHash.Length);
                return passwordHash2;
            }
        }

        internal static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return new byte[0];
            List<byte> result = new List<byte>();
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            try
            {
                int i = 0;
                do
                {
                    byte b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;
                    result.Add(b);
                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }
        
    }
}
