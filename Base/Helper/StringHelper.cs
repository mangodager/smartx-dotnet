using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ETModel
{
	public static class StringHelper
	{
		public static IEnumerable<byte> ToBytes(this string str)
		{
			byte[] byteArray = Encoding.Default.GetBytes(str);
			return byteArray;
		}

		public static byte[] ToByteArray(this string str)
		{
			byte[] byteArray = Encoding.Default.GetBytes(str);
			return byteArray;
		}

	    public static byte[] ToUtf8(this string str)
	    {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            return byteArray;
        }

		//public static byte[] HexToBytes(this string hexString)
		//{
		//	if (hexString.Length % 2 != 0)
		//	{
		//		throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
		//	}

		//	var hexAsBytes = new byte[hexString.Length / 2];
		//	for (int index = 0; index < hexAsBytes.Length; index++)
		//	{
		//		string byteValue = "";
		//		byteValue += hexString[index * 2];
		//		byteValue += hexString[index * 2 + 1];
		//		hexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		//	}
		//	return hexAsBytes;
		//}

		public static string Fmt(this string text, params object[] args)
		{
			return string.Format(text, args);
		}

		public static string ListToString<T>(this List<T> list)
		{
			StringBuilder sb = new StringBuilder();
			foreach (T t in list)
			{
				sb.Append(t);
				sb.Append(",");
			}
			return sb.ToString();
		}

        public static int HashCode(string value)
        {
            int h = 0;
            if (h == 0 && value.Length > 0)
            {
                char[] val = value.ToCharArray();

                for (int i = 0; i < val.Length; i++)
                {
                    h = 31 * h + val[i];
                }
            }
            return h;
        }

    }
}