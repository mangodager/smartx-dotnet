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

        static public void Assert(bool b,string info)
        {
            if (!b)
            {
                Log.Info(info);
                throw new Exception(info);
            }
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
            var dbSnapshot = LuaVMStack.s_dbSnapshot;

            string amount = "0";
            Account account = dbSnapshot.Accounts.Get(address);
            if(account != null)
                amount = account.amount;
            return amount;
        }

        static public bool CheckAddress(string address)
        {
            return Wallet.CheckAddress(address);
        }

        static public string PledgeFactory()
        {
            return Entity.Root.GetComponent<Consensus>().PledgeFactory;
        }

        static public Block GetMcBlock(long height)
        {
            return BlockChainHelper.GetMcBlock(height);
        }

        static public void _SetValue(string table,string key, string json)
        {
            LuaVMStack.s_dbSnapshot.StoragesMap.Add($"{LuaVMStack.s_consAddress}__{table}__{key}", json);
        }

        static public string _GetValue(string table, string key)
        {
            return LuaVMStack.s_dbSnapshot.StoragesMap.Get($"{LuaVMStack.s_consAddress}__{table}__{key}");
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

        static public void TransferEvent(string sender, string _to, string msg)
        {
            if(Entity.Root.GetComponent<Consensus>().transferShow)
            {
                // bind to account
                var consAddress = LuaVMStack.s_consAddress;
                var dbSnapshot  = LuaVMStack.s_dbSnapshot;
                var transfer    = LuaVMStack.s_transfer;
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

                        dbSnapshot.BindTransfer2Account($"{_to}{consAddress}", LuaVMStack.s_transfer.hash);
                    }
                }
                LuaVMStack.Add2TransferTemp(msg);
            }
        }

        static public bool Transfer(string addressIn, string addressOut, string amount)
        {
            var transfer     = LuaVMStack.s_transfer;
            var dbSnapshot   = LuaVMStack.s_dbSnapshot;
            var height       = LuaVMStack.s_transfer.height;
            var transferShow = Entity.Root.GetComponent<Consensus>().transferShow;

            if (BigHelper.Less(amount, "0", true))
                throw new Exception("Transfer amount Less 0");

            if (transfer.addressIn != addressIn && LuaVMStack.s_consAddress != addressIn && LuaVMStack.s_sender != addressIn)
                throw new Exception("Transfer address error");

            if (height != 1)
            {
                Account accountIn = dbSnapshot.Accounts.Get(addressIn);
                if (accountIn == null)
                    throw new Exception("Transfer accountIn error");
                if (BigHelper.Less(accountIn.amount, amount, false))
                    throw new Exception("Transfer accountIn.amount is not enough");
                accountIn.amount = BigHelper.Sub(accountIn.amount, amount);
                dbSnapshot.Accounts.Add(accountIn.address, accountIn);
                if (transferShow)
                    dbSnapshot.BindTransfer2Account(addressIn, transfer.hash);
            }

            Account accountOut = dbSnapshot.Accounts.Get(addressOut) ?? new Account() { address = addressOut, amount = "0", nonce = 0 };
            accountOut.amount = BigHelper.Add(accountOut.amount, amount);
            dbSnapshot.Accounts.Add(accountOut.address, accountOut);

            if (transferShow)
            {
                dbSnapshot.BindTransfer2Account(addressOut, transfer.hash);
                transfer.height = height;
                dbSnapshot.Transfers.Add(transfer.hash, transfer);
            }
            return true;
        }

        static public bool IsERC(string address)
        {
            return string.IsNullOrEmpty(address) ? false : LuaVMStack.s_dbSnapshot.Contracts.Get(address) != null;
        }

        static public bool IsERC(string address, string scriptName)
        {
            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
            return luaVMEnv.IsERC(LuaVMStack.s_dbSnapshot, address, scriptName);
        }

        static public object[] Call(string consAddress, string data)
        {
            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
            var rel = luaVMEnv.LuaCall(LuaVMStack.s_dbSnapshot, consAddress, LuaVMStack.s_consAddress, data, LuaVMStack.s_transfer.height, out object[] result);
            if(rel)
                return result;
            return null;
        }

        static public bool TransferToken(string consAddress,string addressIn, string addressOut, string amount)
        {
            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
            var data = $"transfer(\"{addressOut}\",\"{amount}\")";

            var transfer     = LuaVMStack.s_transfer;
            if (transfer.addressIn != addressIn && LuaVMStack.s_consAddress != addressIn && LuaVMStack.s_sender != addressIn)
                return false;

            if (luaVMEnv.IsERC(LuaVMStack.s_dbSnapshot, consAddress, "ERCSat"))
            {
                return Transfer(addressIn, addressOut, amount);
            }

            return luaVMEnv.LuaCall(LuaVMStack.s_dbSnapshot, consAddress, addressIn, data, LuaVMStack.s_transfer.height, out object[] result);
        }

        static public string BalanceOf(string consAddress, string address)
        {
            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
            var data = $"balanceOf(\"{address}\")";
            bool rel = luaVMEnv.LuaCall(LuaVMStack.s_dbSnapshot, consAddress, LuaVMStack.s_consAddress, data, LuaVMStack.s_transfer.height, out object[] result);
            if (rel && result != null && result.Length >= 1)
            {
                var amount = ((string)result[0]) ?? "0";
                return amount;
            }
            return "0";
        }

        static public string Create(string data,string depend)
        {
            LuaVMEnv luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
            bool rel = luaVMEnv.LuaCreate(LuaVMStack.s_dbSnapshot, LuaVMStack.s_transfer.addressIn, data, LuaVMStack.s_transfer.timestamp, depend, LuaVMStack.s_transfer.height, out string addressNew);
            if (rel && !string.IsNullOrEmpty(addressNew) )
            {
                return addressNew;
            }
            return null;
        }


    }

}
