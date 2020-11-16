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

        static public bool IsRuleOnline(long height,string address)
        {
            var mcblk = BlockChainHelper.GetMcBlock(height-1);
            if(mcblk != null)
            {
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();
                for (int i = 0; i < mcblk.linksblk.Count; i++)
                {
                    Block blk = blockMgr.GetBlock(mcblk.linksblk[i]);
                    if (blk != null)
                    {
                        if (blk.Address == address)
                            return true;
                    }
                }
            }
            return false;
        }

        static public void TransferEvent(string sender, string _to, string _value)
        {
            if(Entity.Root.GetComponent<Consensus>().transferShow)
            {
                // bind to account
                var consAddress = LuaVMEnv.s_consAddress;
                var dbSnapshot  = LuaVMEnv.s_dbSnapshot;
                if (!string.IsNullOrEmpty(consAddress))
                {
                    var mapIn = dbSnapshot.ABC.Get(sender) ?? new List<string>();
                    mapIn.Remove(consAddress);
                    mapIn.Add(consAddress);
                    dbSnapshot.ABC.Add(sender, mapIn);
                    if (!string.IsNullOrEmpty(_to))
                    {
                        var mapOut = dbSnapshot.ABC.Get(_to) ?? new List<string>();
                        mapOut.Remove(consAddress);
                        mapOut.Add(consAddress);
                        dbSnapshot.ABC.Add(_to, mapOut);

                        dbSnapshot.BindTransfer2Account($"{_to}{consAddress}", LuaVMEnv.s_transfer.hash);
                    }
                }
            }
        }

    }

}
