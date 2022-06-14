using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;
using Nethereum.Contracts;

namespace MWH.MyNethereum.QuickRef
{
    [Event("Transfer")]
    public class TransferEvent : IEventDTO
    {
        [Parameter("address", "_from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "_to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string Owner { get; set; }
    }

    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 2)]
        public BigInteger TokenAmount { get; set; }
    }

    static public class FunctionOutputHelpers
    {
        // event MultipliedEvent(
        // address indexed sender,
        // int oldProduct,
        // int value,
        // int newProduct
        // );

        [FunctionOutput]
        public class MultipliedEventArgs
        {
            [Parameter("address", "sender", 1, true)]
            public string sender { get; set; }

            [Parameter("int", "oldProduct", 2, false)]
            public int oldProduct { get; set; }

            [Parameter("int", "value", 3, false)]
            public int value { get; set; }

            [Parameter("int", "newProduct", 4, false)]
            public int newProduct { get; set; }

        }

        //event NewMessageEvent(
        // address indexed sender,
        // uint256 indexed ind,
        // string msg
        //);

        [FunctionOutput]
        public class NewMessageEventArgs
        {
            [Parameter("address", "sender", 1, true)]
            public string sender { get; set; }

            [Parameter("uint256", "ind", 2, true)]
            public int ind { get; set; }

            [Parameter("string", "msg", 3, false)]
            public string msg { get; set; }
        }
    }

    static public class Web3Helpers
    {
        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }

        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(javaTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }
    }


}