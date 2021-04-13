using System;
using System.Numerics;
using XLua;

namespace ETModel
{
    [LuaCallCSharp]
    public static class BigHelper
    {
        public static string Add(string a,string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");

            return (BigFloat.Parse(a) + BigFloat.Parse(b)).ToString();
        }

        public static string Sub(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            return (BigFloat.Parse(a) - BigFloat.Parse(b)).ToString();
        }

        public static string Mul(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            return (BigFloat.Parse(a) * BigFloat.Parse(b)).ToString();
        }

        public static string Div(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            return (BigFloat.Parse(a) / BigFloat.Parse(b)).ToString();
        }

        // 大于 > , when equals = true is >=
        public static bool Greater(string a, string b,bool equals)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            if (equals)
                return (BigFloat.Parse(a) >= BigFloat.Parse(b));
            return (BigFloat.Parse(a) > BigFloat.Parse(b));
        }

        // 小于 < , when equals = true is <=
        public static bool Less(string a, string b, bool equals)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            if (equals)
                return (BigFloat.Parse(a) <= BigFloat.Parse(b));
            return (BigFloat.Parse(a) < BigFloat.Parse(b));
        }

        // 等于
        public static bool Equals(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");
            return (BigFloat.Parse(a) == BigFloat.Parse(b));
        }

        public static string Sqrt(string a)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            return BigFloat.Sqrt(BigFloat.Parse(a)).ToString();
        }

        public static string Min(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");

            if ((BigFloat.Parse(a) > BigFloat.Parse(b)))
                return b;
            return a;
        }

        public static string Max(string a, string b)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            b = string.IsNullOrEmpty(b) ? "0" : b.Replace(",", "");

            if ((BigFloat.Parse(a) < BigFloat.Parse(b)))
                return b;
            return a;
        }

        // 取整
        public static string Round8(string a)
        {
            a = string.IsNullOrEmpty(a) ? "0" : a.Replace(",", "");
            return BigFloat.Parse(a).ToString(8);
        }

        public static void Test()
        {
            BigFloat pi = BigFloat.Parse("3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930381964428810975665933446128475648233786783165271201909145648566923460348610454326648213393607260249141273724587006606315588174881520920962829254091715364367892590360011330530548820466521384146951941511609433057270365759591953092186117381932611793105118548074462379962749567351885752724891227938183011949");
            Console.WriteLine("\nPi to a really long decimal place:");
            Console.WriteLine(pi.ToString(16));
            BigFloat pi2 = BigFloat.Parse("3.141592653589");
            Console.WriteLine(pi2.ToString(16));

            Console.WriteLine(BigFloat.Round(pi2).ToString(16));

        }

    }
}