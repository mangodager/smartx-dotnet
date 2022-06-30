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

    public class NFTFaucet : Component
    {
        string SAT_httpRpc8101 = "http://122.10.161.138:8102";
        string SAT_bestNodeRpc = "http://app.smartx.one/getNode";

        List<string> bestNodeList = new List<string>();
        public LevelDBStore NFTFaucetDBStore = new LevelDBStore();
        ComponentNetworkHttp networkHttp;

        Dictionary<string, BlockSub> transfers = new Dictionary<string, BlockSub>();

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

            string db_path = jd["db_path"]?.ToString();
            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
            NFTFaucetDBStore.Init(DatabasePath);

            SAT_httpRpc8101 = jd["SAT_httpRpc8101"]?.ToString() ?? SAT_httpRpc8101;
            SAT_bestNodeRpc = jd["SAT_bestNodeRpc"]?.ToString() ?? SAT_bestNodeRpc;

        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"NFTFaucetRpc http://{networkHttp.ipEndPoint}/");
            HttpService.ReplaceFunc = ReplaceFunc;

            transfers = LoadDB<Dictionary<string, BlockSub>>("CrossChain_transfers") ?? new Dictionary<string, BlockSub>();

            //BestNodeProcess();
            //TransfersProcess();

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
            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, false))
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
            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, true))
            {
                snapshot.Add(key, JsonHelper.ToJson(obj));
                snapshot.Commit();
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
                    var list = transfers.Values.Where(x => x.addressIn != myAddress && x.height == 0)?.ToList();
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



        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || networkHttp == null
             || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;

            string cmd = httpMessage.map["cmd"].ToLower();
            switch (cmd)
            {
                case "nftfaucet_login":
                    nftfaucet_login(httpMessage);
                    break;
                case "nftfaucet_verification":
                    nftfaucet_verification(httpMessage);
                    break;
                case "nftfaucet_register":
                    nftfaucet_register(httpMessage);
                    break;
                case "nftfaucet_goods":
                    nftfaucet_login(httpMessage);
                    break;
                case "nftfaucet_friends":
                    nftfaucet_friends(httpMessage);
                    break;
                case "nftfaucet_friends_count":
                    nftfaucet_friends_count(httpMessage);
                    break;

                default:
                    {
                        if (cmd != "hearbeat" && bestNodeList.Count > 0)
                        {
                            httpMessage.result = ComponentNetworkHttp.QueryStringSync(SAT_httpRpc8101 + "/" + cmd, httpMessage, 5f);
                        }
                    }
                    break;
            }
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
            if (transfer.extend != null)
                httpMessage.map["extend"] = System.Web.HttpUtility.UrlEncode(JsonHelper.ToJson(transfer.extend));

            var result = ComponentNetworkHttp.QueryStringSync(url, httpMessage, timeout);

            return result;
        }

        public string SendTransfersContextSync(string url, BlockSub transfer, float timeout = 5f)
        {
            List<BlockSub> list = new List<BlockSub>();
            foreach (var v in transfers)
            {
                if (v.Value.addressIn == transfer.addressIn && v.Value.height == 0)
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
                _SendTransfersSync(url, v, timeout);
            }

            return _SendTransfersSync(url, transfer, timeout);
        }

        Dictionary<string, JObject> verifications = new Dictionary<string, JObject>();

        public void nftfaucet_verification(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!HttpRpc.GetParam(httpMessage, "1", "phone", out string phone))
            {
                httpMessage.result = "command error! \n";
                return;
            }

            if (verifications.ContainsKey(phone))
            {
                if (verifications.TryGetValue(phone, out JObject verData))
                {
                    if (long.Parse(verData["timestamp"].ToString()) - TimeHelper.Now() > 120000)
                    {
                        verifications.Remove(phone);
                    }
                    else
                    {
                        httpMessage.result = "success";
                        return;
                    }
                }
            }

            string verification = (RandomHelper.Random() % 1000000).ToString().PadLeft(6, '0');// 随机
            var Uid = "edmini";//短信接口账户名
            var SMSUrl = "https://utf8api.smschinese.cn/";//发送短信接口
            var key = "FFE23E8E0F4CC55EC2B80153090C5000";//对应SMS接口功能里的密钥
            var smsMob = phone;//需要发送短信的手机
            var smsText = "【艺幻数藏】您的注册验证码是" + verification + "如非本人操作，请忽略本短信";//短信内容格式为【艺幻数藏】您当前的验证码为：*****，如非本人操作，请忽略
            var Url = SMSUrl + "?" + "Uid=" + Uid + "&Key=" + key + "&smsMob=" + phone + "&smsText=" + smsText;

            var QueryMessage = new HttpMessage();
            //QueryMessage.map = new Dictionary<string, string>();
            //QueryMessage.map["Uid"] = Uid;
            //QueryMessage.map["Key"] = key;
            //QueryMessage.map["smsMob"] = phone;
            //QueryMessage.map["smsText"] = smsText;
            //var str_result = ComponentNetworkHttp.QueryStringSync(SMSUrl, QueryMessage, 5f);

            var str_result = ComponentNetworkHttp.QueryStringSync(Url, QueryMessage, 5f);

            int result = int.Parse(str_result);

            if (result >0)
            {
                JObject verData = new JObject();
                verData.Add("verification", verification);
                verData.Add("timestamp", TimeHelper.Now().ToString());
                verifications.Add(phone, verData);

                httpMessage.result = "success";
            }
        }

        public void nftfaucet_register(HttpMessage httpMessage)
        {
            // 手机号 ， 密码 ， 注册时间 ， 自动生成SAT钱包地址
            // 关联邀请人 , 生成自己的邀请码
            httpMessage.result = "";
            if (!HttpRpc.GetParam(httpMessage, "1", "phone", out string phone))
            {
                httpMessage.result = "command error! \n";
                return;
            }
            if (!HttpRpc.GetParam(httpMessage, "2", "password", out string password))
            {
                httpMessage.result = "command error! \n";
                return;
            }
            if (!HttpRpc.GetParam(httpMessage, "3", "verification", out string verification))
            {
                httpMessage.result = "command error! \n";
                return;
            }

            if (verifications.ContainsKey(phone))
            {
                if (verifications.TryGetValue(phone, out JObject verData))
                {
                    if (long.Parse(verData["timestamp"].ToString()) - TimeHelper.Now() > 120000)
                    {
                        verifications.Remove(phone);
                        httpMessage.result = "Verification error";
                        return;
                    }
                    else
                    {
                        if (verData["verification"].ToString() != verification)
                        {
                            httpMessage.result = "Verification error";
                            return;
                        }
                    }
                }
            }

            HttpRpc.GetParam(httpMessage, "4", "invitation", out string invitation);
            HttpRpc.GetParam(httpMessage, "5", "address", out string address);


            JObject registerData = new JObject();
            registerData.Add("phone", phone);
            registerData.Add("password", password);
            registerData.Add("verification", verification);
            registerData.Add("address", address);
            registerData.Add("invitation", phone);
            registerData.Add("timestamp", TimeHelper.Now().ToString());

            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, true))
            {
                if (snapshot.Get($"register_{phone}") != null)
                {
                    httpMessage.result = "Already exists";

                }
                else
                {
                    Log.Info($"nftfaucet_register {phone} invitation:{invitation}");
                    if (!string.IsNullOrEmpty(invitation))
                    {
                        snapshot.List.Add($"invitation_{invitation}", phone);
                    }
                    snapshot.Add($"register_{phone}", registerData.ToString());
                    snapshot.Commit();
                    httpMessage.result = "success";
                    verifications.Remove(phone);
                }
            }

        }

        public void nftfaucet_login(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!HttpRpc.GetParam(httpMessage, "1", "phone", out string phone))
            {
                httpMessage.result = "command error! \n";
                return;
            }
            if (!HttpRpc.GetParam(httpMessage, "2", "password", out string password))
            {
                httpMessage.result = "command error! \n";
                return;
            }

            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, false))
            {
                string data = snapshot.Get($"register_{phone}");
                if (string.IsNullOrEmpty(data))
                {
                    httpMessage.result = "non-existent";

                }
                else
                {
                    JObject registerData = JObject.Parse(data);
                    if (!string.IsNullOrEmpty(registerData["password"].ToString()) && registerData["password"].ToString() == password)
                    {
                        registerData["password"] = "";
                        httpMessage.result = registerData.ToString();
                    }
                    else
                    {
                        httpMessage.result = "Password error";
                    }
                }
            }


        }

        public void nftfaucet_goods(HttpMessage httpMessage)
        {
            if (!HttpRpc.GetParam(httpMessage, "Index", "index", out string index))
            {
                index = "0";
            }


        }

        public void nftfaucet_friends(HttpMessage httpMessage)
        {
            List<string> invitation_phones = new List<string>();
            httpMessage.result = JsonHelper.ToJson(invitation_phones);
            if (!HttpRpc.GetParam(httpMessage, "1", "phone", out string phone))
            {
                httpMessage.result = "command error! \n";
                return;
            }

            if (!HttpRpc.GetParam(httpMessage, "Index", "index", out string index))
            {
                index = "0";
            }
            int Index = int.Parse(index);

            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, false))
            {
                string data = snapshot.Get($"register_{phone}");
                if (string.IsNullOrEmpty(data))
                {
                    httpMessage.result = "0";
                }
                else
                {
                    JObject registerData = JObject.Parse(data);

                    int TFA_Count = snapshot.List.GetCount($"invitation_{registerData["invitation"]}");
                    Index = TFA_Count - Index * 10;
                    for (int ii = Index; ii > Index - 10 && ii > 0; ii--)
                    {
                        string invitation_phone = snapshot.List.Get($"invitation_{registerData["invitation"]}", ii - 1);
                        if (!string.IsNullOrEmpty(invitation_phone))
                        {
                            invitation_phones.Add(invitation_phone);
                        }
                    }
                    httpMessage.result = JsonHelper.ToJson(invitation_phones);
                }
            }


        }


        public void nftfaucet_friends_count(HttpMessage httpMessage)
        {
            httpMessage.result = "0";
            if (!HttpRpc.GetParam(httpMessage, "1", "phone", out string phone))
            {
                httpMessage.result = "command error! \n";
                return;
            }

            using (DbSnapshot snapshot = NFTFaucetDBStore.GetSnapshot(0, false))
            {
                string data = snapshot.Get($"register_{phone}");
                if (string.IsNullOrEmpty(data))
                {

                }
                else
                {
                    JObject registerData = JObject.Parse(data);

                    var count = snapshot.List.GetCount($"invitation_{registerData["invitation"]}");
                    httpMessage.result = count.ToString();
                }
            }
        }




    }
}



















