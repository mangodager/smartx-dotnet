using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using XLua;

namespace ETModel
{
    [LuaCallCSharp]
    // 用于访问链上数据的lua接口
    public class LuaContract
    {
        static public void LogPrint(string info)
        {
            Log.Info(info);
        }

        static public string Sha256(string data)
        {
            return CryptoHelper.Sha256(data);
        }

        static public bool VerifySignature(string data,string signature,string address)
        {
            return Wallet.Verify(signature.HexToBytes(), data, address);
        }

        static public string GetAmount(string address)
        {
            DbSnapshot dbSnapshot = LuaVMEnv.s_dbSnapshot;

            string amount = "0";
            Account account = dbSnapshot.Accounts.Get(address);
            if(account != null)
                amount = account.amount;
            return amount;
        }

        static public Block GetMcBlock(long height)
        {
            return BlockChainHelper.GetMcBlock(height);
        }

        static public bool SaveAccount(DbSnapshot dbSnapshot,string address, string account,string json)
        {
            dbSnapshot.StoragesAccount.Add($"{address}_{account}", json);

            return false;
        }

        static public string LoadAccount(DbSnapshot dbSnapshot, string address,string account)
        {
            return dbSnapshot.StoragesAccount.Get($"{address}_{account}"); ;
        }

        static public int StringCompare(string a,string b)
        {
            return a.CompareTo(b);
        }


    }

}
