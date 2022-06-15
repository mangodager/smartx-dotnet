using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LevelDB;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class LevelDBExport
    {

        static public void ErgodicPledgeContract()
        {
            JArray arr = new JArray();
            LevelDBStore dbstore = Entity.Root.GetComponent<LevelDBStore>();
            DB db = dbstore.GetDB();
            lock (db)
            {
                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {

                    File.Delete("./" + "ErgodiList" + ".csv");
                    var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
                    using (var it = db.CreateIterator())
                    {
                        for (it.SeekToFirst(); it.IsValid(); it.Next())
                        {
                            if ((!it.KeyAsString().Contains("undo")) && (!it.KeyAsString().Contains("Undo")))
                            {
                                if (it.KeyAsString().IndexOf("Accounts___") == 0)
                                {
                                    try
                                    {
                                        Console.WriteLine($"Value as string: {it.ValueAsString()}");
                                        Dictionary<string, Dictionary<string, object>> kv = JsonHelper.FromJson<Dictionary<string, Dictionary<string, object>>>(it.ValueAsString());
                                        bool bl = false;
                                        string str = kv["obj"]["address"].ToString();
                                        try
                                        {
                                            bl = luaVMEnv.IsERC(dbSnapshot, str, "PledgePair");
                                        }
                                        catch
                                        { }
                                        if (bl)
                                        {
                                            //如果当前地址是质押合约则写入文件中
                                            File.AppendAllText("./" + "ErgodiList" + ".csv", kv["obj"]["address"].ToString() + "\n");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(it.KeyAsString());
                                        Console.WriteLine($"出错了: {it.ValueAsString()}");
                                        Console.WriteLine(e.Message);
                                        break;
                                    }
                                }
                            }
                        }
                        Console.WriteLine("质押合约导出完成");
                    }
                }
            }
        }

        static public JArray TraversalData(string values)
        {
            JArray BalanceList = new JArray();//余额列表
            Dictionary<string, string> TotalPledge = new Dictionary<string, string>();//所有用户质押总和

            Dictionary<string, JObject> CapitalPool = new Dictionary<string, JObject>();//质押合约的资金池总量和凭证总量

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                string[] contractlist = values.Split(',');
                for (int j = 0; j < contractlist.Length; j++)
                {
                    if (contractlist[j] != "")
                    {
                        JObject jobj = new JObject();
                        Account account = dbSnapshot.Accounts.Get(contractlist[j].ToString());//质押合约资金总量

                        var consensus = Entity.Root.GetComponent<Consensus>();
                        var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

                        WalletKey key = Wallet.GetWallet().GetCurWallet();

                        var sender = key.ToAddress();
                        var blockSub = new BlockSub();

                        blockSub.addressIn = sender;
                        blockSub.addressOut = contractlist[j].ToString();
                        blockSub.data = "totalSupply()";
                        bool rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out object[] result);
                        HttpMessage message = new HttpMessage();
                        string totalSupply = "0";
                        if (rel)
                        {
                            if (result != null)
                            {
                                if (result.Length == 1)
                                {
                                    totalSupply = result[0].ToString();
                                }

                            }
                        }
                        if (BigHelper.Greater(account.amount, "1", true) && BigHelper.Greater(totalSupply, "1", true))
                        {
                            jobj.Add(new JProperty("amount", account.amount));//资金池总量
                            jobj.Add(new JProperty("totalSupply", totalSupply));//获取凭证总量
                            CapitalPool.Add(contractlist[j], jobj);
                        }
                    }
                }
            }


            LevelDBStore dbstore = Entity.Root.GetComponent<LevelDBStore>();
            DB db = dbstore.GetDB();
            lock (db)
            {
                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    using (var it = db.CreateIterator())
                    {
                        var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
                        for (it.SeekToFirst(); it.IsValid(); it.Next())
                        {
                            //Log.Info($"Value as string: {it.KeyAsString()}");
                            if ((!it.KeyAsString().Contains("undo")) && (!it.KeyAsString().Contains("Undo")))
                            {
                                if (it.KeyAsString().IndexOf("Accounts___") == 0)
                                {
                                    try
                                    {
                                        Console.WriteLine($"Value as string: {it.ValueAsString()}");
                                        Dictionary<string, Dictionary<string, object>> kv = JsonHelper.FromJson<Dictionary<string, Dictionary<string, object>>>(it.ValueAsString());
                                        string address = kv["obj"]["address"].ToString();
                                        string balance = kv["obj"]["amount"].ToString();
                                        if (!values.Contains(address))
                                        {
                                            bool bl = false;
                                            try
                                            {
                                                bl = luaVMEnv.IsERC(dbSnapshot, address, null);
                                            }
                                            catch
                                            { }
                                            //当前地址不是质押合约则加入列表
                                            if (!bl)
                                            {
                                                JObject obj = new JObject();
                                                obj.Add("balance", kv["obj"]["amount"].ToString());
                                                obj.Add("address", kv["obj"]["address"].ToString());
                                                obj.Add("name", "SAT");
                                                BalanceList.Add(obj);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(it.KeyAsString());
                                        Console.WriteLine($"出错了: {it.ValueAsString()}");
                                        Console.WriteLine(e.Message);
                                        break;
                                    }
                                }
                                else if (it.KeyAsString().Contains("StgMap"))
                                {
                                    Console.WriteLine($"Value as string: {it.ValueAsString()}");
                                    var myvoucher = JsonHelper.FromJson<string>(it.ValueAsString());//我的凭证
                                    if (BigHelper.Greater(myvoucher, "1", true))
                                    {
                                        var StgMapdata = it.KeyAsString().Replace("___", "__").Split("__");

                                        if (CapitalPool.TryGetValue(StgMapdata[1], out JObject amountandtotaSuppy))
                                        {
                                            var amount = amountandtotaSuppy["amount"].ToString();//资金池总数
                                            var totaSuppy = amountandtotaSuppy["totalSupply"].ToString();//凭证总数
                                            if (!TotalPledge.TryGetValue(StgMapdata[3], out string pledgebalance1))
                                            {
                                                pledgebalance1 = "0";
                                            }
                                            var pledgebalance2 = BigHelper.Mul(BigHelper.Div(myvoucher, totaSuppy), amount);//我的质押余额
                                            var pledgebalance3 = BigHelper.Add(pledgebalance1, pledgebalance2);
                                            TotalPledge.Remove(StgMapdata[3]);
                                            TotalPledge.Add(StgMapdata[3], pledgebalance3);
                                        }
                                    }
                                }

                            }
                        }
                        Console.WriteLine("数据导出完成，正在进行排序");
                    }
                }
            }

            if (BalanceList.Any() && BalanceList != null)
            {
                if (TotalPledge.Any() && TotalPledge != null)
                {
                    for (int i = 0; i < BalanceList.Count; i++)
                    {
                        if (TotalPledge.TryGetValue(BalanceList[i]["address"].ToString(), out string pledgebalance))
                        {
                            BalanceList[i]["pledgebalance"] = pledgebalance;
                            BalanceList[i]["totalholding"] = BigHelper.Add(pledgebalance, BalanceList[i]["balance"].ToString());
                        }
                        else
                        {
                            BalanceList[i]["pledgebalance"] = "0";
                            BalanceList[i]["totalholding"] = BalanceList[i]["balance"].ToString();
                        }

                    }
                }

                //去除小于10000的数据
                for (int i = BalanceList.Count -1; i >=0 ; i--)
                {
                    if (BigHelper.Less(BalanceList[i]["totalholding"].ToString(), "10000", true))
                    {
                        BalanceList.Remove(BalanceList[i]);
                    }
                }
                //排序(根据总持有量进行排序totalholding)
                for (int i = 0; i < BalanceList.Count - 1; i++)
                {
                    for (int j = i + 1; j < BalanceList.Count; j++)
                    {
                        if ((float)BalanceList[i]["totalholding"] < (float)BalanceList[j]["totalholding"])
                        {
                            JObject temp = JObject.Parse(BalanceList[i].ToString());
                            BalanceList[i] = BalanceList[j];
                            BalanceList[j] = temp;
                        }

                    }
                }
                //arr.Take(100);
                //取前二百
                JArray array = new JArray();
                for (int i = 0; i < 200; i++)
                {
                    BalanceList[i]["ranking"] = i + 1;
                    array.Add(BalanceList[i]);
                }
                Console.WriteLine("排行数据导出完成");
                return array;
            }
            else
            {
                Console.WriteLine("排行数据导出失败");
                return null;
            }
        }
    }


}
