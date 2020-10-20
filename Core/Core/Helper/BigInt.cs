using System;
using System.Numerics;
using XLua;

namespace ETModel
{
    [LuaCallCSharp]
    public static class BigInt
    {
        public static string Add(string a,string b)
        {
            return (BigInteger.Parse(a.Replace(",", "")) + BigInteger.Parse(b.Replace(",", ""))).ToString();
        }

        public static string Sub(string a, string b)
        {
            return (BigInteger.Parse(a.Replace(",", "")) - BigInteger.Parse(b.Replace(",", ""))).ToString();
        }

        public static string Mul(string a, string b)
        {
            return (BigInteger.Parse(a.Replace(",", "")) * BigInteger.Parse(b.Replace(",", ""))).ToString();
        }

        public static string Div(string a, string b)
        {
            return (BigInteger.Parse(a.Replace(",", "")) / BigInteger.Parse(b.Replace(",", ""))).ToString();
        }

        // 大于
        public static bool Greater(string a, string b,bool equal)
        {
            if (equal)
                return (BigInteger.Parse(a.Replace(",", "")) >= BigInteger.Parse(b.Replace(",", "")));
            return (BigInteger.Parse(a) > BigInteger.Parse(b));
        }

        // 小于
        public static bool Less(string a, string b, bool equal)
        {
            if(equal)
                return (BigInteger.Parse(a.Replace(",", "")) <= BigInteger.Parse(b.Replace(",", "")));
            return (BigInteger.Parse(a.Replace(",", "")) < BigInteger.Parse(b.Replace(",", "")));
        }

        // 小于
        public static bool Equal(string a, string b)
        {
            return (BigInteger.Parse(a.Replace(",", "")) == BigInteger.Parse(b.Replace(",", "")));
        }

        // 取整
        public static string Round(string a)
        {
            int index = a.IndexOf(".");
            if (index == -1)
                return a;
            return a.Substring(0,index);
        }


    }
}