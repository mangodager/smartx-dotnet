using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.IO;
using Nethereum.Web3;
using MWH.MyNethereum.QuickRef;
using Nethereum.Hex.HexTypes;
using System.Threading;

using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.CQS;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts;
using Nethereum.BlockchainProcessing.Services;
using Newtonsoft.Json;
using System.Net;

namespace ETModel
{

    public class CrossChain : Component
    {
        const int UNLOCK_TIMEOUT = 2 * 60; // 2 minutes (arbitrary)
        const int SLEEP_TIME = 5 * 1000; // 5 seconds (arbitrary)
        const int MAX_TIMEOUT = 2 * 60 * 1000; // 2 minutes (arbirtrary)

        int chainId = 123778899; //Nethereum test chain, chainId
        string SL2_rpcUrl = "http://123.58.210.13:8546";
        string SAT_httpRpc8101 = "http://122.10.161.138:8102";
        string SAT_bestNodeRpc = "http://app.smartx.one/getNode";
        string SL2_PrivateKey = "";

        List<string> bestNodeList = new List<string>();
        public LevelDBStore crossChainDBStore = new LevelDBStore();
        ComponentNetworkHttp networkHttp;

        Dictionary<string, BlockSub> transfers = new Dictionary<string, BlockSub>();
        Dictionary<string, JObject> recharges = new Dictionary<string, JObject>();
        Dictionary<string, JObject> cashOuts = new Dictionary<string, JObject>();

        JObject MappingSymbol = new JObject();
        JObject MappingContract = new JObject();

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

            string db_path = jd["db_path"]?.ToString();
            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
            crossChainDBStore.Init(DatabasePath);

            SL2_rpcUrl = jd["SL2_rpcUrl"]?.ToString() ?? SL2_rpcUrl;
            SAT_httpRpc8101 = jd["SAT_httpRpc8101"]?.ToString() ?? SAT_httpRpc8101;
            SAT_bestNodeRpc = jd["SAT_bestNodeRpc"]?.ToString() ?? SAT_bestNodeRpc;

            var jt = jd["TokenMapping"];
            for (var i=0; i < jt.Count(); i++ )//遍历数组
            {
                if (!MappingSymbol.ContainsKey(jt[i][0].ToString()))
                {
                    MappingSymbol.Add(new JProperty(jt[i][0].ToString(), jt[i][1].ToString()));
                }
                if (!MappingContract.ContainsKey(jt[i][1].ToString()))
                {
                    MappingContract.Add(new JProperty(jt[i][1].ToString(), jt[i][2].ToString()));
                }
                if (!MappingContract.ContainsKey(jt[i][2].ToString()))
                {
                    MappingContract.Add(new JProperty(jt[i][2].ToString(), jt[i][1].ToString()));
                }
            }

            SL2_PrivateKey = Wallet.GetWallet().GetCurWallet().privatekey.ToHexString();
        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"CrossChainRpc http://{networkHttp.ipEndPoint}/");
            HttpService.ReplaceFunc = ReplaceFunc;

            transfers = LoadDB<Dictionary<string, BlockSub>>("CrossChain_transfers") ?? new Dictionary<string, BlockSub>();
            recharges = LoadDB<Dictionary<string, JObject>>("CrossChain_recharges") ?? new Dictionary<string, JObject>();
            cashOuts  = LoadDB<Dictionary<string, JObject>>("CrossChain_cashOuts") ?? new Dictionary<string, JObject>();

            MonitoringSL2();
            MonitoringSL2_ERC20();
            BestNodeProcess();
            CashOutProcess();
            RechargeProcess();
            TransfersProcess();

        }

        public byte[] ReplaceFunc(byte[] RawUrlFileByte, HttpListenerRequest request)
        {
            var RawUrlFileText = System.Text.Encoding.UTF8.GetString(RawUrlFileByte);

            string Authority = NetworkHelper.DnsToIPEndPoint(request.Url.Authority);

            if (!string.IsNullOrEmpty(Program.jdNode["publicIP"]?.ToString()))
            {
                var ipEndPoint1 = NetworkHelper.ToIPEndPoint(Program.jdNode["publicIP"].ToString());
                var ipEndPoint2 = NetworkHelper.ToIPEndPoint(Authority);
                Authority = ipEndPoint1.Address + ":" + ipEndPoint2.Port;
            }

            RawUrlFileText = RawUrlFileText.Replace("\"http://www.SmartX.com:8101\"", $"\"http://{Authority}\"");

            var cons = Entity.Root.GetComponent<Consensus>();
            if (cons != null)
            {
                RawUrlFileText = RawUrlFileText.Replace("\"dsoZAxn4GEiGycq2sFc24CAQn4SRCgDuS\"", $"\"{cons.SatswapFactory}\"");
                RawUrlFileText = RawUrlFileText.Replace("\"RnnUBgzrzv2z7YrEz5ZhuzVtbkCbspKpV\"", $"\"{cons.ERCSat}\"");
                RawUrlFileText = RawUrlFileText.Replace("\"SWipqG94LJXXx9E8sYbSpZVa8n5TSUD2B\"", $"\"{cons.PledgeFactory}\"");
                RawUrlFileText = RawUrlFileText.Replace("\"RXF5eSnpEGNgRsUZdx9t2o5ByB511NzrT\"", $"\"{cons.LockFactory}\"");
            }

            string ipAddress = IPEndPoint.Parse(Authority).Address.ToString();

            var httpPool = Entity.Root.GetComponent<HttpPool>();
            string poolIP = "";
            poolIP += $"'http://{ipAddress}:{networkHttp.ipEndPoint.Port}':'Ruler->{ipAddress}:{httpPool?.GetHttpAdderrs().Port}',\n";
            poolIP += $"'Ruler->{ipAddress}:{httpPool?.GetHttpAdderrs().Port}':'http://{ipAddress}:{networkHttp.ipEndPoint.Port}'\n";
            RawUrlFileText = RawUrlFileText.Replace("Helper.PoolList = {}", $"Helper.PoolList = {{\n{poolIP}}}");

            return System.Text.Encoding.UTF8.GetBytes(RawUrlFileText);
        }

        T LoadDB<T>(string key)
        {
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
            {
                string str = snapshot.Get(key);
                if (!string.IsNullOrEmpty(str))
                {
                    return JsonHelper.FromJson<T>(str);
                }
            }
            return default;
        }

        void SaveDB<T>(string key, T obj)
        {
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
            {
                snapshot.Add(key, JsonHelper.ToJson(obj));
                snapshot.Commit();
            }
        }

        // 获取充值地址
        public string RechargeAddress(string addressSAT)
        {
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
            {
                string addressSL2 = snapshot.Get($"SAT_To_SL2{addressSAT}");
                if (string.IsNullOrEmpty(addressSL2))
                {
                    var privateKey = Nethereum.Signer.EthECKey.GenerateKey().GetPrivateKey();
                    var account = new Nethereum.Web3.Accounts.Account(privateKey, chainId);
                    addressSL2 = account.Address.ToLower();

                    snapshot.Add($"SAT_To_SL2{addressSAT}", addressSL2);
                    snapshot.Add($"SL2_To_SAT{addressSL2}", addressSAT);

                    snapshot.List.Add($"Recharge_PrivateKey_{chainId}", privateKey);

                    snapshot.Commit();

                    FileStream fs = new FileStream("./Data/SL2_PrivateKey.txt", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    sw.WriteLine($"{addressSL2},{privateKey}");
                    sw.Close();
                    fs.Close();
                }
                return addressSL2;
            }
        }

        // 设置提现地址
        public void SetCashOutAddress(string addressSAT, string addressETH)
        {
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
            {
                if (snapshot.Get($"SAT_CashOut{addressSAT}") == null)
                {
                    snapshot.Add($"SAT_CashOut{addressSAT}", addressETH);
                }
                snapshot.Commit();
            }
        }

        public string GetCashOutAddress(string addressSAT)
        {
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
            {
                return snapshot.Get($"SAT_CashOut{addressSAT}");
            }
        }

        string bestNode = "";
        public async void BestNodeProcess()
        {
            bestNode = SAT_httpRpc8101;

            await Task.Delay(1000);

            var request = new HttpMessage();
            request.map = new Dictionary<string, string>();

            string addressIn = Wallet.GetWallet().GetCurWallet().ToAddress();

            while (true)
            {
                try
                {
                    string str = await ComponentNetworkHttp.QueryString(SAT_bestNodeRpc, request);
                    var list = JsonHelper.FromJson<List<string>>(str);
                    if (list != null && list.Count > 0)
                    {
                        bestNodeList = list;
                    }

                    string temp = "";
                    foreach (var v in bestNodeList)
                    {
                        try
                        {
                            var quest = new HttpMessage();
                            quest.map = new Dictionary<string, string>();
                            quest.map.Add("cmd", "account");
                            quest.map.Add("Address", addressIn);

                            var result = ComponentNetworkHttp.QuerySync(v, quest, 5f);
                            result.map.TryGetValue("Account", out string _address);
                            result.map.TryGetValue("amount", out string _amount);
                            result.map.TryGetValue("nonce", out string _nonce);
                            long.TryParse(_nonce, out long nonce);

                            temp = v;
                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (!string.IsNullOrEmpty(temp))
                    {
                        bestNode = temp;
                    }

                    await Task.Delay(120000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(120000);
                }
            }
        }

        public async void RechargeProcess()
        {
            await Task.Delay(5000);
            var request = new HttpMessage();
            request.map = new Dictionary<string, string>();

            List<JObject> delList = new List<JObject>();
            while (true)
            {
                try
                {
                    bool change = false;
                    delList.Clear();
                    foreach (var v in recharges)
                    {
                        var obj = v.Value;
                        //state 2
                        if (obj["state"].ToString() == "2")
                        {
                            var web3 = new Web3(SL2_rpcUrl);
                            var rpt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(obj["hash"].ToString());
                            var status = rpt.Status.Value;
                            if (status == 1)
                            {
                                obj["state"] = "3";
                                change = true;
                            }
                        }

                        //state 3
                        if (obj["state"].ToString() == "3")
                        {
                            if (obj["SAT_hash"] == null)
                            {
                                try
                                {
                                    if (obj["ERC20"] != null)
                                    {
                                        BlockSub transfer = new BlockSub();
                                        transfer.type = "contract";
                                        transfer.addressIn = Wallet.GetWallet().GetCurWallet().ToAddress();
                                        transfer.addressOut = MappingContract[obj["ERC20"].ToString()].ToString();
                                        transfer.data = $"transfer(\"{obj["addressSAT"]}\",\"{obj["amount"]}\")";
                                        transfer.amount = "";
                                        transfer.depend = "";
                                        transfer.nonce = GetAccountNotice(transfer.addressIn) + 1;
                                        transfer.timestamp = TimeHelper.Now();
                                        transfer.hash = transfer.ToHash();
                                        transfer.sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet());

                                        Log.Debug($"SAT_hash: {transfer.hash} Out:{obj["addressSAT"]} {MappingSymbol[transfer.addressOut]}:{transfer.amount}");

                                        obj["SAT_result"] = SendTransfersContextSync(bestNode, transfer, 5f);
                                        if (obj["SAT_result"].ToString().IndexOf("success\":true") != -1)
                                        {
                                            obj["SAT_hash"] = transfer.hash;
                                            transfers.Add(transfer.hash, transfer);
                                            change = true;
                                        }
                                    }
                                    else
                                    {
                                        BlockSub transfer = new BlockSub();
                                        transfer.type = "transfer";
                                        transfer.addressIn = Wallet.GetWallet().GetCurWallet().ToAddress();
                                        transfer.addressOut = obj["addressSAT"].ToString();
                                        transfer.amount = obj["amount"].ToString();
                                        transfer.data = $"";
                                        transfer.depend = "";
                                        transfer.nonce = GetAccountNotice(transfer.addressIn) + 1;
                                        transfer.timestamp = TimeHelper.Now();
                                        transfer.hash = transfer.ToHash();
                                        transfer.sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet());

                                        Log.Debug($"SAT_hash: {transfer.hash} Out:{obj["addressSAT"]} Sat:{transfer.amount}");

                                        obj["SAT_result"] = SendTransfersContextSync(bestNode, transfer, 5f);
                                        if (obj["SAT_result"].ToString().IndexOf("success\":true") != -1)
                                        {
                                            obj["SAT_hash"] = transfer.hash;
                                            transfers.Add(transfer.hash, transfer);
                                            change = true;
                                        }
                                    }
                                }
                                catch(Exception e)
                                {
                                    Log.Debug(e.ToString());
                                    change = true;
                                }
                            }

                            if (obj["SAT_hash"] != null)
                            {
                                obj["state"] = "4";
                                change = true;
                            }
                        }

                        //state 4
                        if (obj["state"].ToString() == "4")
                        {
                            //transfers.TryGetValue(obj["hash_SAT"].ToString(), out BlockSub transfer);

                            request.map.Clear();
                            request.map.Add("cmd", "TransferState");
                            request.map.Add("hash", obj["SAT_hash"].ToString());

                            var result = ComponentNetworkHttp.QueryStringSync(bestNode, request, 5f);
                            if (!string.IsNullOrEmpty(result))
                            {
                                var resultT = JsonHelper.FromJson<BlockSub>(result);
                                if (resultT != null && resultT.hash == obj["SAT_hash"].ToString())
                                {
                                    // 成功
                                    if (resultT.height != 0) 
                                    {
                                        obj["state"] = "5";
                                        obj["height"] = resultT.height;
                                        change = true;
                                    }
                                    else
                                    // 失败
                                    if(resultT.temp!=null&& resultT.temp.Count>0&& resultT.temp[resultT.temp.Count-1]== "Transfer nonce error")
                                    {
                                        Log.Debug($"hash:{obj["SAT_hash"]} Transfer nonce error");
                                        transfers.Remove(obj["SAT_hash"].ToString());
                                        obj.Remove("SAT_hash");
                                        obj["state"] = "3";
                                        change = true;
                                    }
                                }
                            }
                        }

                        // state 5 set finish
                        if (obj["state"].ToString() == "5")
                        {
                            delList.Add(obj);
                            change = true;
                        }

                    }

                    foreach (var obj in delList)
                    {
                        using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
                        {
                            snapshot.List.Add($"CASH_OUT_{obj["addressSAT"]}", obj.ToString());
                            snapshot.Commit();
                        }
                        recharges.Remove(obj["hash"].ToString());
                        if (obj["SAT_hash"] != null) {
                            transfers.Remove(obj["SAT_hash"].ToString());
                        }
                    }

                    if (change)
                    {
                        SaveDB("CrossChain_recharges", recharges);
                        SaveDB("CrossChain_transfers", transfers);
                    }

                    await Task.Delay(5000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(5000);
                }
            }

        }

        public async void CashOutProcess()
        {
            await Task.Delay(5000);

            var request = new HttpMessage();
            request.map = new Dictionary<string, string>();

            List<JObject> delList = new List<JObject>();

            while (true)
            {
                try
                {
                    bool change = false;
                    delList.Clear();
                    foreach (var v in cashOuts)
                    {
                        var obj = v.Value;

                        //transfers.TryGetValue(obj["hash"].ToString(), out BlockSub transfer);
                        // state 2
                        if (obj["state"].ToString() == "2")
                        {
                            request.map.Clear();
                            request.map.Add("cmd", "TransferState");
                            request.map.Add("hash", obj["hash"].ToString());
                            var result = ComponentNetworkHttp.QueryStringSync(bestNode, request, 5f);
                            if (!string.IsNullOrEmpty(result))
                            {
                                var resultT = JsonHelper.FromJson<BlockSub>(result);
                                if (resultT != null && resultT.hash == obj["hash"].ToString())
                                {
                                    if (resultT.height != 0)
                                    {
                                        obj["state"] = "3";
                                        obj["height"] = resultT.height.ToString();
                                        change = true;
                                    }
                                    else
                                    {
                                        obj["error"] = "1";
                                        change = true;
                                    }
                                }
                            }
                        }
                        // state 3
                        if (obj["state"].ToString() == "3")
                        {
                            if (obj["SL2_hash"] == null || string.IsNullOrEmpty(obj["SL2_hash"].ToString()) )
                            {
                                if (obj["type"].ToString() == "transfer")
                                {
                                    obj["SL2_hash"] = await SendTransactionSL2(SL2_rpcUrl, SL2_PrivateKey, obj["addressCashOut"].ToString(), obj["amount"].ToString());
                                }
                                else
                                if (obj["type"].ToString() == "contract")
                                {
                                    var data = $"transfer(\"{Wallet.GetWallet().GetCurWallet().ToAddress()}\",\"{obj["amount"]}\")";
                                    if (obj["data"].ToString() == data)
                                    {
                                        obj["SL2_hash"] = await SendTransactionERC20(SL2_rpcUrl, MappingContract[obj["addressOut"].ToString()].ToString(), SL2_PrivateKey, obj["addressCashOut"].ToString(), obj["amount"].ToString());
                                    }
                                    else
                                    {
                                        obj["error"] = "1";
                                        change = true;
                                    }
                                }
                                change = true;
                            }
                            if (obj["SL2_hash"] != null&&!string.IsNullOrEmpty(obj["SL2_hash"].ToString()))
                            {
                                obj["state"] = "4";
                                change = true;
                            }
                        }

                        // state 4
                        if (obj["state"].ToString() == "4")
                        {
                            var web3 = new Web3(SL2_rpcUrl);
                            var rpt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(obj["SL2_hash"].ToString());
                            var status = rpt.Status.Value;
                            if (status == 1)
                            {
                                obj["state"] = "5";
                                change = true;
                            }
                        }

                        // state 5 set finish
                        if (((obj["state"].ToString() == "5" && obj["SL2_hash"] != null && !string.IsNullOrEmpty(obj["SL2_hash"].ToString()))
                           || (obj["error"]!=null&&obj["error"].ToString() == "1"))
                        && (TimeHelper.Now() - long.Parse(obj["timeCashOut"].ToString())) > 120000)
                        {
                            delList.Add(obj);
                            change = true;
                        }
                    }

                    foreach (var obj in delList)
                    {
                        using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
                        {
                            snapshot.List.Add($"CASH_OUT_{obj["addressIn"]}", obj.ToString());
                            snapshot.Commit();
                        }
                        cashOuts.Remove(obj["hash"].ToString());
                        transfers.Remove(obj["hash"].ToString());
                    }

                    if (change)
                    {
                        SaveDB("CrossChain_cashOuts", cashOuts);
                    }

                    await Task.Delay(5000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(5000);
                }
            }
        }

        public async void TransfersProcess()
        {
            await Task.Delay(15000);

            var request = new HttpMessage();
            request.map = new Dictionary<string, string>();

            var myAddress = Wallet.GetWallet().GetCurWallet().ToAddress();

            while (true)
            {
                try
                {
                    var list = transfers.Values.Where( x => x.addressIn!=myAddress&&x.height==0)?.ToList();
                    if (list != null)
                    {
                        list.Sort((BlockSub a, BlockSub b) =>
                        {
                            int rel = a.nonce.CompareTo(b.nonce);
                            if (rel == 0)
                                rel = a.hash.CompareTo(b.hash);
                            return rel;
                        });
                        var url = bestNode;
                        foreach (var v in list)
                        {
                            if (_SendTransfersSync(url, v, 5f) == "{\"success\":false,\"rel\":-9}")
                            {
                                transfers.Remove(v.hash);
                            }
                        }
                    }
                    await Task.Delay(15000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(15000);
                }
            }
        }

        public async void MonitoringSL2()
        {
            {
                var account = new Nethereum.Web3.Accounts.Account(SL2_PrivateKey, chainId);
                var _web3 = new Web3(SL2_rpcUrl);
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(account.Address);
                Log.Debug($"Our account: {account.Address} , Balance: {Web3.Convert.FromWei(balance)}");
            }

            await Task.Delay(1000);
            //Connecting to Ethereum mainnet using Infura
            var web3 = new Web3(SL2_rpcUrl);
            while (true)
            {
                try
                {
                    //Getting current block number  
                    var rel = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    ulong endBlockNumber = ulong.Parse(rel.Value.ToString());
                    ulong startBlockNumber = 0;

                    using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
                    {
                        string str = snapshot.Get("startBlockNumber");
                        if (!string.IsNullOrEmpty(str))
                        {
                            ulong.TryParse(str, out startBlockNumber);
                        }
                        if (startBlockNumber == 0)
                        {
                            startBlockNumber = endBlockNumber;
                        }
                    }

                    if (startBlockNumber + 100 < endBlockNumber)
                    {
                        endBlockNumber = Math.Min(startBlockNumber + 100, endBlockNumber);
                    }

                    //var list = await ScanTx2(web3, startBlockNumber, endBlockNumber);
                    var list = await ScanTx(web3, startBlockNumber, endBlockNumber);

                    if (list != null)
                    {
                        foreach (var v in list)
                        {
                            if (!recharges.ContainsKey(v.Key))
                            {
                                recharges.Add(v.Key, v.Value);
                            }
                        }
                        if (list.Count != 0)
                        {
                            SaveDB("CrossChain_recharges", recharges);
                        }
                    }

                    using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
                    {
                        startBlockNumber = endBlockNumber;
                        snapshot.Add("startBlockNumber", startBlockNumber.ToString());
                        snapshot.Commit();
                    }

                    await Task.Delay(3000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(3000);
                }
            }
        }

        public async void MonitoringSL2_ERC20()
        {
            List<string> _list = new List<string>();
            foreach (var v in MappingContract)
            {
                try
                {
                    var a1 = AddressUtil.Current.ConvertToChecksumAddress(v.Key);
                    if (AddressUtil.Current.IsChecksumAddress(a1))
                    {
                        _list.Add(v.Key);
                    }
                }
                catch (Exception)
                {
                }
            }
            string[] contractAddresses = _list.ToArray();

            await Task.Delay(1000);
            //Connecting to Ethereum mainnet using Infura
            var web3 = new Web3(SL2_rpcUrl);
            while (true)
            {
                try
                {
                    //Getting current block number  
                    var rel = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    ulong endBlockNumber = ulong.Parse(rel.Value.ToString());
                    ulong startBlockNumber = 0;

                    using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
                    {
                        string str = snapshot.Get("startBlockNumber");
                        if (!string.IsNullOrEmpty(str))
                        {
                            ulong.TryParse(str, out startBlockNumber);
                        }
                        if (startBlockNumber == 0)
                        {
                            startBlockNumber = endBlockNumber;
                        }
                    }

                    if (startBlockNumber + 100 < endBlockNumber)
                    {
                        endBlockNumber = Math.Min(startBlockNumber + 100, endBlockNumber);
                    }

                    var list = await ScanTx_ERC20(web3, contractAddresses,startBlockNumber, endBlockNumber);

                    if (list != null)
                    {
                        foreach (var v in list)
                        {
                            if (!recharges.ContainsKey(v.Key))
                            {
                                recharges.Add(v.Key, v.Value);
                            }
                        }
                        if (list.Count != 0)
                        {
                            SaveDB("CrossChain_recharges", recharges);
                        }
                    }

                    using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, true))
                    {
                        startBlockNumber = endBlockNumber;
                        snapshot.Add("startBlockNumber", startBlockNumber.ToString());
                        snapshot.Commit();
                    }

                    await Task.Delay(3000);
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                    await Task.Delay(3000);
                }
            }
        }

        public async ETTask<string> SendTransactionERC20(string Url, string contractAddress, string privateKey, string receiverAddress, string value)
        {
            try
            {
                var account = new Nethereum.Web3.Accounts.Account(privateKey, chainId);
                //Log.Debug("Our account: " + account.Address);
                //Now let's create an instance of Web3 using our account pointing to our nethereum testchain
                var web3 = new Web3(account, Url);

                var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
                var transfer = new TransferFunction()
                {
                    To = receiverAddress,
                    TokenAmount = Web3.Convert.ToWei(value, UnitConversion.EthUnit.Ether),
                };
                transfer.GasPrice = await web3.Eth.GasPrice.SendRequestAsync();
                var estimate = await transferHandler.EstimateGasAsync(contractAddress, transfer);
                transfer.Gas = estimate.Value;

                Log.Debug($"SendTransactionERC20 to {receiverAddress} gas:{estimate.Value}");
                var txnReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(contractAddress, transfer);
                Log.Debug("Transaction hash transfer is: " + txnReceipt.TransactionHash);

                // 查余额
                var balanceOfFunctionMessage = new BalanceOfFunction()
                {
                    Owner = account.Address,
                };
                var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
                var balance = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);

                Log.Debug("Balance of deployment owner address after transfer: " + Web3.Convert.FromWei(balance));
                return txnReceipt.TransactionHash;
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            return null;
        }

        private async ETTask<string> SendTransactionSL2(string Url, string privateKey, string receiverAddress, string value)
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey, chainId);
            //Log.Debug("Our account: " + account.Address);
            //Now let's create an instance of Web3 using our account pointing to our nethereum testchain
            var web3 = new Web3(account, Url);

            var transfer = new Nethereum.RPC.Eth.DTOs.TransactionInput
            {
                From = account.Address,
                To = receiverAddress,
                Value = new HexBigInteger(Web3.Convert.ToWei(value, UnitConversion.EthUnit.Ether)),
                Data = "0x",
            };
            transfer.GasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            transfer.Gas = await web3.Eth.TransactionManager.EstimateGasAsync(transfer);

            Log.Debug($"SendTransactionSL2 to {receiverAddress}");
            var txnReceipt = await web3.Eth.TransactionManager.SendTransactionAndWaitForReceiptAsync(transfer, null);
            Log.Debug("Transaction hash transfer is: " + txnReceipt.TransactionHash);

            //var balance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            //Log.Debug("Balance of deployment owner address after transfer: " + Web3.Convert.FromWei(balance));

            return txnReceipt.TransactionHash;
        }

        private static async Task RequestBalance(string Url = "http://rpc.bnbstars.org", string Address = "")
        {
            var web3 = new Web3(Url);
            var balance = await web3.Eth.GetBalance.SendRequestAsync(Address);
            Console.WriteLine("Balance of deployment owner address after transfer: " + Web3.Convert.FromWei(balance));
        }

        // 在区块链中扫描交易
        public async ETTask<Dictionary<string, JObject>> ScanTx(Web3 web3, ulong startBlockNumber, ulong endBlockNumber)
        {
            var list = new Dictionary<string, JObject>();
            //try
            {
                if(endBlockNumber-startBlockNumber>10||endBlockNumber%100==0)
                Log.Debug($"ScanTx: {startBlockNumber} -> {endBlockNumber}");

                long txTotalCount = 0;
                for (ulong blockNumber = startBlockNumber; blockNumber < endBlockNumber; blockNumber++)
                {
                    var blockParameter = new Nethereum.RPC.Eth.DTOs.BlockParameter(blockNumber);
                    var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParameter);
                    var trans = block.Transactions;
                    int txCount = trans.Length;
                    txTotalCount += txCount;

                    //Log.Debug($"blockNumber:{blockNumber} txCount:{txCount}");

                    foreach (var tx in trans)
                    {
                        try
                        {
                            var bn = tx.BlockNumber.Value;
                            var th = tx.TransactionHash;
                            var ti = tx.TransactionIndex.Value;

                            var rpt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(th);
                            var status = rpt.Status.Value;

                            var nc = tx.Nonce.Value;
                            var from = tx.From;

                            var to = tx.To;
                            if (to == null) to = "to:NULL";
                            var v = tx.Value.Value;
                            var g = tx.Gas.Value;
                            var gp = tx.GasPrice.Value;
                            
                            //Log.Debug($"{blockNumber}: " + th.ToString() + " " + ti.ToString() + " " + nc.ToString() + " " + from.ToString() + " " + to.ToString() + " " + v.ToString() + " " + g.ToString() + " " + gp.ToString() + " " + status.ToString());

                            if (status == 1)
                            {
                                using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
                                {
                                    var addressSAT = snapshot.Get($"SL2_To_SAT{to.ToLower()}");
                                    if (!string.IsNullOrEmpty(addressSAT))
                                    {
                                        JObject rechargeData = new JObject();
                                        rechargeData.Add("bn", bn.ToString());
                                        rechargeData.Add("hash", th);
                                        rechargeData.Add("ti", ti.ToString());
                                        rechargeData.Add("addressSL2", to);
                                        rechargeData.Add("addressSAT", addressSAT);
                                        rechargeData.Add("amount", Web3.Convert.FromWei(v).ToString());
                                        rechargeData.Add("state", "2");
                                        rechargeData.Add("timestamp",TimeHelper.Now().ToString());
                                        list.Add(th, rechargeData);

                                        Log.Debug($"Recharge: {th} {addressSAT} {Web3.Convert.FromWei(v).ToString()}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("ScanTx.Tx:\t" + ex.ToString());
                            if (ex.InnerException != null) Log.Debug("ScanTx.Tx:\t" + ex.InnerException.ToString());
                        }
                    }
                }
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());

            //}
            return list;
        }

        public async ETTask<Dictionary<string, JObject>> ScanTx_ERC20(Web3 web3, string[] contractAddresses, ulong startBlockNumber, ulong endBlockNumber)
        {
            var list = new Dictionary<string, JObject>();

            var transferEventLogs = new List<EventLog<TransferEvent>>();

            //create our processor to retrieve transfers
            //restrict the processor to Transfers for a specific contract address
            var processor = web3.Processing.Logs.CreateProcessorForContracts<TransferEvent>(contractAddresses,
                    tfr => transferEventLogs.Add(tfr));

            //if we need to stop the processor mid execution - call cancel on the token
            var cancellationTokenSource = new CancellationTokenSource();

            //crawl the required block range
            await processor.ExecuteAsync(
                toBlockNumber: new BigInteger(endBlockNumber),
                cancellationToken: cancellationTokenSource.Token,
                startAtBlockNumberIfNotProcessed: new BigInteger(startBlockNumber));

            if (endBlockNumber - startBlockNumber > 10 || endBlockNumber % 100 == 0)
            Log.Debug($"ScanTx_ERC20: {startBlockNumber} -> {endBlockNumber}. Logs found: {transferEventLogs.Count}.");

            for (int ii = 0; ii < transferEventLogs.Count; ii++)
            {
                //Log.Debug($"{ii} {transferEventLogs[ii].Log.Address} TranHash:{transferEventLogs[ii].Log.TransactionHash} BlkNum:{transferEventLogs[ii].Log.BlockNumber} From:{transferEventLogs[ii].Event.From} To:{transferEventLogs[ii].Event.To} Value:{transferEventLogs[ii].Event.Value}");

                if ( MappingContract[transferEventLogs[ii].Log.Address]!=null )
                {
                    using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
                    {
                        var addressSAT = snapshot.Get($"SL2_To_SAT{transferEventLogs[ii].Event.To.ToLower()}");
                        if (!string.IsNullOrEmpty(addressSAT))
                        {
                            JObject rechargeData = new JObject();
                            rechargeData.Add("bn", ii.ToString());
                            rechargeData.Add("hash", transferEventLogs[ii].Log.TransactionHash);
                            rechargeData.Add("ti", transferEventLogs[ii].Log.TransactionIndex.ToString());
                            rechargeData.Add("addressSL2", transferEventLogs[ii].Event.To.ToLower());
                            rechargeData.Add("addressSAT", addressSAT);
                            rechargeData.Add("ERC20", transferEventLogs[ii].Log.Address);
                            rechargeData.Add("amount", Web3.Convert.FromWei(transferEventLogs[ii].Event.Value).ToString());
                            rechargeData.Add("state", "2");
                            rechargeData.Add("timestamp", TimeHelper.Now().ToString());
                            list.Add(transferEventLogs[ii].Log.TransactionHash, rechargeData);

                            Log.Debug($"RechargeERC20: {transferEventLogs[ii].Log.TransactionHash} {addressSAT} {Web3.Convert.FromWei(transferEventLogs[ii].Event.Value).ToString()}");
                        }
                    }
                }

            }

            return list;
        }

        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || networkHttp == null
             || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;

            string cmd = httpMessage.map["cmd"].ToLower();
            switch (cmd)
            {
                case "getrechargeaddress":
                    OnGetRechargeAddress(httpMessage);
                    break;
                case "transfer":
                    {
                        if (httpMessage.map.ContainsKey("remarks"))
                        {
                            var remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);
                            if (remarks.IndexOf("CashOutSubmit:")==0)
                            {
                                OnCashOutSubmit(httpMessage);
                                return;
                            }
                        }
                        httpMessage.result = ComponentNetworkHttp.QueryStringSync(SAT_httpRpc8101 + "/" + cmd, httpMessage, 5f);
                    }
                    break;
                case "crosschainconf":
                    OnCrossChainConf(httpMessage);
                    break;
                case "getcashoutlog":
                    GetCashOutLog(httpMessage);
                    break;
                case "getcashoutmgr":
                    GetCashOutMgr(httpMessage);
                    break;
                default:
                    {
                        if (cmd!= "hearbeat" && bestNodeList.Count>0)
                        {
                            httpMessage.result = ComponentNetworkHttp.QueryStringSync(SAT_httpRpc8101 + "/" + cmd, httpMessage, 5f);
                        }
                    }
                    break;
            }
        }

        public void OnGetRechargeAddress(HttpMessage httpMessage)
        {
            if (!HttpRpc.GetParam(httpMessage, "1", "address", out string address))
            {
                return;
            }

            if (!Wallet.CheckAddress(address))
                return;

            var v = RechargeAddress(address);
            httpMessage.result = "{\"ret\":\"success\",\"rechargeAddress\":\"###\"}".Replace("###", v);
        }

        public void OnCrossChainConf(HttpMessage httpMessage)
        {
            if (!HttpRpc.GetParam(httpMessage, "1", "address", out string address))
            {
                return;
            }

            JObject cashOutData = null;
            foreach (var v in cashOuts)
            {
                var obj = v.Value;
                if (obj["addressIn"].ToString() == address)
                {
                    cashOutData = obj;
                }
            }

            JObject result = new JObject();
            result.Add(new JProperty("ret", "success"));
            result.Add(new JProperty("addressCrossChain", Wallet.GetWallet().GetCurWallet().ToAddress()));
            result.Add(new JProperty("cashOutData", cashOutData));
            result.Add(new JProperty("MappingSymbol", MappingSymbol));

            httpMessage.result = result.ToString();
        }

        public void OnCashOutSubmit(HttpMessage httpMessage)
        {
            BlockSub transfer = new BlockSub();
            transfer.hash = httpMessage.map["hash"];
            transfer.type = httpMessage.map["type"];
            transfer.nonce = long.Parse(httpMessage.map["nonce"]);
            transfer.addressIn = httpMessage.map["addressIn"];
            transfer.addressOut = httpMessage.map["addressOut"];
            transfer.amount = httpMessage.map["amount"];
            transfer.data = System.Web.HttpUtility.UrlDecode(httpMessage.map["data"]);
            transfer.timestamp = long.Parse(httpMessage.map["timestamp"]);
            transfer.sign = httpMessage.map["sign"].HexToBytes();

            if (httpMessage.map.ContainsKey("depend"))
                transfer.depend = System.Web.HttpUtility.UrlDecode(httpMessage.map["depend"]);
            if (httpMessage.map.ContainsKey("remarks"))
                transfer.remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);
            if (httpMessage.map.ContainsKey("extend"))
                transfer.extend = JsonHelper.FromJson<List<string>>(System.Web.HttpUtility.UrlDecode(httpMessage.map["extend"]));

            JObject cashOutdata = JObject.FromObject(transfer);
            if (cashOuts.ContainsKey(cashOutdata["hash"].ToString()))
            {
                return;
            }

            if ( cashOutdata["remarks"]== null || string.IsNullOrEmpty(cashOutdata["remarks"].ToString()) )
            {
                httpMessage.result = "{\"success\":false,\"rel\":-21}";
                return;
            }

            var addrCashOut = cashOutdata["remarks"].ToString().Replace("CashOutSubmit:","");
            addrCashOut = AddressUtil.Current.ConvertToChecksumAddress(addrCashOut);
            if (!AddressUtil.Current.IsChecksumAddress(addrCashOut) ||!AddressUtil.Current.IsValidEthereumAddressHexFormat(addrCashOut))
            {
                httpMessage.result = "{\"success\":false,\"rel\":-22}";
                return;
            }

            httpMessage.result = SendTransfersContextSync(bestNode, transfer, 5f);

            if (httpMessage.result.IndexOf("success\":true") != -1)
            {
                transfers.Add(transfer.hash, transfer);

                cashOutdata["addressCashOut"] = addrCashOut;
                cashOutdata["timeCashOut"] = TimeHelper.Now().ToString();
                cashOutdata["state"] = "2";
                cashOuts.Add(transfer.hash, cashOutdata);

                SaveDB("CrossChain_transfers", transfers);
                SaveDB("CrossChain_cashOuts", cashOuts);
            }

        }

        public void GetCashOutLog(HttpMessage httpMessage)
        {
            if (!HttpRpc.GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            HttpRpc.GetParam(httpMessage, "2", "index", out string indexStr);

            int.TryParse(indexStr, out int getIndex);

            var objList = new List<JObject>();
            using (DbSnapshot snapshot = crossChainDBStore.GetSnapshot(0, false))
            {
                int Log_Count = snapshot.List.GetCount($"CASH_OUT_{address}");
                getIndex = Log_Count - getIndex;
                for (int ii = getIndex; ii > getIndex - 20 && ii > 0; ii--)
                {
                    string str = snapshot.List.Get($"CASH_OUT_{address}", ii - 1);
                    if (!string.IsNullOrEmpty(str))
                    {
                        var obj = JsonHelper.FromJson<JObject>(str);
                        if (obj != null)
                        {
                            objList.Add(obj);
                        }
                    }
                }

            }

            foreach (var v in recharges)
            {
                var obj = v.Value;
                if (obj != null && obj["addressSAT"].ToString() == address)
                {
                    objList.Insert(0, obj);
                }
            }

            foreach (var v in cashOuts)
            {
                var obj = v.Value;
                if (obj != null && obj["addressIn"].ToString() == address)
                {
                    objList.Insert(0, obj);
                }
            }

            httpMessage.result = JsonHelper.ToJson(objList);
        }

        public void GetCashOutMgr(HttpMessage httpMessage)
        {
            if (!HttpRpc.GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            HttpRpc.GetParam(httpMessage, "2", "index", out string indexStr);

            int.TryParse(indexStr, out int getIndex);

            var objList = new List<JObject>();
            foreach (var v in recharges)
            {
                var obj = v.Value;
                if (obj != null && (TimeHelper.Now() - long.Parse(obj["timestamp"].ToString())) > 120000)
                {
                    objList.Insert(0, obj);
                }
            }

            foreach (var v in cashOuts)
            {
                var obj = v.Value;
                if (obj != null && (TimeHelper.Now() - long.Parse(obj["timeCashOut"].ToString())) > 120000)
                {
                    objList.Insert(0, obj);
                }
            }

            httpMessage.result = JsonHelper.ToJson(objList);
        }

        long GetAccountNotice(string addressIn)
        {
            var quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();
            quest.map.Add("cmd", "account");
            quest.map.Add("Address", addressIn);

            var result = ComponentNetworkHttp.QuerySync(bestNode, quest, 5f);

            result.map.TryGetValue("Account", out string _address);
            result.map.TryGetValue("amount", out string _amount);
            result.map.TryGetValue("nonce", out string _nonce);

            long.TryParse(_nonce,out long nonce);

            foreach (var v in transfers)
            {
                if (v.Value.addressIn == addressIn)
                {
                    if (v.Value.nonce >= nonce + 1)
                    {
                        nonce = v.Value.nonce;
                    }
                }
            }

            return nonce;
        }

        string _SendTransfersSync(string url, BlockSub transfer, float timeout = 5f)
        {
            var httpMessage = new HttpMessage();
            httpMessage.map = new Dictionary<string, string>();

            httpMessage.map["cmd"] = "transfer";
            httpMessage.map["hash"] = transfer.hash;
            httpMessage.map["type"] = transfer.type;
            httpMessage.map["nonce"] = transfer.nonce.ToString();
            httpMessage.map["addressIn"] = transfer.addressIn;
            httpMessage.map["addressOut"] = transfer.addressOut;
            httpMessage.map["amount"] = transfer.amount;
            httpMessage.map["data"] = System.Web.HttpUtility.UrlEncode(transfer.data);
            httpMessage.map["timestamp"] = transfer.timestamp.ToString();
            httpMessage.map["sign"] = transfer.sign.ToHexString();


            if (!string.IsNullOrEmpty(transfer.depend))
                httpMessage.map["depend"] = System.Web.HttpUtility.UrlEncode(transfer.depend);
            if (!string.IsNullOrEmpty(transfer.remarks))
                httpMessage.map["remarks"] = System.Web.HttpUtility.UrlEncode(transfer.remarks);
            if (transfer.extend!=null)
                httpMessage.map["extend"] = System.Web.HttpUtility.UrlEncode(JsonHelper.ToJson(transfer.extend));

            var result = ComponentNetworkHttp.QueryStringSync(url, httpMessage, timeout);

            return result;
        }

        string SendTransfersContextSync(string url, BlockSub transfer, float timeout = 5f)
        {
            List<BlockSub> list = new List<BlockSub>();
            foreach (var v in transfers)
            {
                if (v.Value.addressIn== transfer.addressIn && v.Value.height==0 )
                {
                    list.Add(v.Value);
                }
            }

            list.Sort((BlockSub a, BlockSub b) =>
            {
                int rel = a.nonce.CompareTo(b.nonce);
                if (rel == 0)
                    rel = a.hash.CompareTo(b.hash);
                return rel;
            });

            foreach (var v in list)
            {
                _SendTransfersSync(url,v, timeout);
            }

            return _SendTransfersSync(url, transfer, timeout);
        }

    }
}



















