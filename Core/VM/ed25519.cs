using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ETModel
{
    public partial class ed25519
    {
#if (UNITY_IPHONE || UNITY_WEBGL || UNITY_SWITCH) && !UNITY_EDITOR
        const string ed25519_Dll = "__Internal";
#else
        const string ed25519_Dll = "ed25519.dll";
#endif

        [DllImport(ed25519_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ed25519_create_seed([MarshalAs(UnmanagedType.LPArray)] byte[] seed);

        [DllImport(ed25519_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ed25519_create_keypair([MarshalAs(UnmanagedType.LPArray)] byte[] public_key,
                                                         [MarshalAs(UnmanagedType.LPArray)] byte[] private_key,
                                                         [MarshalAs(UnmanagedType.LPArray)] byte[] seed);

        [DllImport(ed25519_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ed25519_sign( [MarshalAs(UnmanagedType.LPArray)] byte[] signature,
                                                [MarshalAs(UnmanagedType.LPArray)] byte[] message,
                                                int message_len,
                                                [MarshalAs(UnmanagedType.LPArray)] byte[] public_key,
                                                [MarshalAs(UnmanagedType.LPArray)] byte[] private_key);

        [DllImport(ed25519_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ed25519_verify(byte[]  signature,
                                                [MarshalAs(UnmanagedType.LPArray)] byte[] message,
                                                int message_len,
                                                [MarshalAs(UnmanagedType.LPArray)] byte[] public_key);

        public static string ConvertStringToHex(string strASCII, string separator = null)
        {
            StringBuilder sbHex = new StringBuilder();
            foreach (char chr in strASCII)
            {
                sbHex.Append(String.Format("{0:X2}", Convert.ToInt32(chr)));
                sbHex.Append(separator ?? string.Empty);
            }
            return sbHex.ToString();
        }

        //public static string ToHexString(byte[] value)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (byte b in value)
        //        sb.AppendFormat("{0:x2}", b);
        //    return sb.ToString();
        //}


    }

}