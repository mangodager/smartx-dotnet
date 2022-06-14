using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ETModel
{
    // 外网连接
    public class HttpRpc : Component
    {
        ComponentNetworkHttp networkHttp;
        List<string> whiteList = new List<string>();

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"HttpRpc http://{networkHttp.ipEndPoint}/");
            HttpService.ReplaceFunc = ReplaceFunc;
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

        public IPEndPoint GetHttpAdderrs()
        {
            return networkHttp.ipEndPoint;
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
                case "account":
                    OnAccount(httpMessage);
                    break;
                case "stats":
                    OnStats(httpMessage);
                    break;
                case "poolstats":
                    OnPoolStats(httpMessage);
                    break;
                case "getpoollist":
                    GetPoolList(httpMessage);
                    break;
                case "redirect":
                    OnRedirect(httpMessage);
                    break;
                case "rules":
                    OnRules(httpMessage);
                    break;
                case "transfer":
                    OnTransfer(httpMessage);
                    break;
                case "transferstate":
                    OnTransferState(httpMessage);
                    break;
                case "transferstate2":
                    OnTransferState2(httpMessage);
                    break;
                case "uniquetransfer":
                    UniqueTransfer(httpMessage);
                    break;
                case "beruler":
                    OnBeRuler(httpMessage);
                    break;
                //case "getmnemonicword":
                //    GetMnemonicWord(httpMessage);
                //    break;
                case "command":
                    Command(httpMessage);
                    break;
                case "gettransfers":
                    GetTransfers(httpMessage);
                    break;
                case "getaccounts":
                    GetAccounts(httpMessage);
                    break;
                case "getnearblock":
                    GetNearBlock(httpMessage);
                    break;
                case "node":
                    OnNode(httpMessage);
                    break;
                case "getblock":
                    GetBlock(httpMessage);
                    break;
                case "getproperty":
                    OnProperty(httpMessage);
                    break;
                case "getliquidity":
                    OnLiquidityOf(httpMessage);
                    break;
                case "balanceof":
                    balanceOf(httpMessage);
                    break;
                case "callfun":
                    callFun(httpMessage);
                    break;
                case "getlinkbad":
                    getLinkBad(httpMessage);
                    break;
                default:
                    break;
            }
        }

        // console 支持
        public void Command(HttpMessage httpMessage)
        {
            httpMessage.result = "command error!";

            string input = httpMessage.map["input"];
            input = input.Replace("%20", " ");
            input = input.Replace("  ", " ");
            input = input.Replace("   ", " ");
            input = input.Replace("  ", " ");

            string[] array = input.Split(' ');

            if (input.IndexOf("-") != -1)
            {
                for (int ii = 1; ii < array.Length; ii++)
                {
                    string[] arrayValue = array[ii].Split(':');
                    if (arrayValue != null && arrayValue.Length >= 1)
                    {
                        httpMessage.map.Remove(arrayValue[0].Replace("-", ""));
                        httpMessage.map.Add(arrayValue[0].Replace("-", ""), arrayValue.Length >= 2 ? arrayValue[1] : "");
                    }
                }
            }
            else
            {
                for (int ii = 1; ii < array.Length; ii++)
                {
                    string arrayValue = array[ii];
                    httpMessage.map.Remove("" + ii);
                    httpMessage.map.Add("" + ii, arrayValue);
                }
            }

            string cmd = array[0].ToLower();
            switch (cmd)
            {
                case "account":
                    OnAccount(httpMessage);
                    break;
                case "stats":
                    OnStats(httpMessage);
                    break;
                case "poolstats":
                    OnPoolStats(httpMessage);
                    break;
                case "rules":
                    OnRules(httpMessage);
                    break;
                case "transferstate":
                    OnTransferState(httpMessage);
                    break;
                case "transferstate2":
                    OnTransferState2(httpMessage);
                    break;
                case "uniquetransfer":
                    UniqueTransfer(httpMessage);
                    break;
                case "beruler":
                    OnBeRuler(httpMessage);
                    break;
                case "node":
                    OnNode(httpMessage);
                    break;
                case "miner":
                    OnMiner(httpMessage);
                    break;
                case "minertransfer":
                    OnMinerTransfer(httpMessage);
                    break;
                case "minertop":
                    OnMinerTop(httpMessage);
                    break;
                case "minerabstract":
                    OnMinerAbstract(httpMessage);
                    break;
                case "getblock":
                    GetBlock(httpMessage);
                    break;
                case "delblock":
                    DelBlock(httpMessage);
                    break;
                case "mergechain":
                    MergeChain(httpMessage);
                    break;
                case "search":
                    OnSearch(httpMessage);
                    break;
                case "callFun":
                    callFun(httpMessage);
                    break;
#if !RELEASE
                case "pool":
                    GetPool(httpMessage);
                    break;
                case "dbreset":
                    onDBReset(httpMessage);
                    break;
                case "outleveldb":
                    OutLeveldb(httpMessage);
                    break;
                case "poollist":
                    PoolList(httpMessage);
                    break;
                case "test":
                    Test(httpMessage);
                    break;
#endif
                case "hello":
                    {
                        httpMessage.result = "welcome join SmartX";
                    }
                    break;
                case "help":
                    {
                        httpMessage.result = "you can use Command menu";
                    }
                    break;
                case "pooltransfer":
                    PoolTransfer(httpMessage);
                    break;
                case "pledgereport":
                    PledgeReport(httpMessage);
                    break;
                default:
                    break;
            }

            //httpMessage.result = "ok";
        }

        private void OutLeveldb(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "passwords", out string password))
            {
                httpMessage.result = "command error! \nexample: wallet password";
                return;
            }

            if (Wallet.GetWallet().IsPassword(password))
            {
                if (!GetParam(httpMessage, "2", "height", out string height))
                {
                    httpMessage.result = "command error! \nexample: outleveldb password height filename";
                    return;
                }
                if (!GetParam(httpMessage, "3", "filename", out string filename))
                {
                    httpMessage.result = "command error! \nexample: outleveldb password height filename";
                    return;
                }

                //Entity.Root.GetComponent<Consensus>().AddRunAction(() => { LevelDBStore.test_ergodic2(long.Parse(height), filename); });
                LevelDBStore.test_ergodic2(long.Parse(height), filename);

                httpMessage.result = "正在导出";
            }
        }

        public void OnSearch(HttpMessage httpMessage)
        {
            OnBlockHash(httpMessage);
            if (httpMessage.result == "false")
            {
                OnBlocktranHash(httpMessage);
                if (httpMessage.result == "false")
                {
                    OnTransactions(httpMessage);
                    if(httpMessage.result == "[]")
                    httpMessage.result = "";
                }
            }
            
        }
        private void OnTransactions(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "Index", "index", out string index))
            {
                index = "0";
            }
            int Index = int.Parse(index);
            List<BlockSub> transactionList = new List<BlockSub>();
            string address = httpMessage.map["1"];
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}");
                    Index = TFA_Count - Index * 12;
                    for (int ii = Index; ii > Index - 12 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{account}", ii-1);
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                if (transfer.addressIn == "" && transfer.addressOut == "" || transfer.addressIn == null && transfer.addressOut == null)
                                {
                                    transfer.addressIn = "00000000000000000000000000000000";
                                    transfer.addressOut = address;
                                }
                                Block myblk = BlockChainHelper.GetMcBlock(transfer.height);
                                if (transfer.timestamp == 0)
                                {
                                    transfer.timestamp = myblk.timestamp;
                                }
                                transactionList.Add(transfer);
                            }
                        }
                    }
                }
                httpMessage.result = JsonHelper.ToJson(transactionList);
            }



        }
        private void OnBlockHash(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "please input hash";
                return;
            }
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var myblk = blockMgr.GetBlock(hash);

            if (myblk == null || myblk.timestamp == 0)
            {
                httpMessage.result = "false";
            }

            else httpMessage.result = JsonHelper.ToJson(myblk);
        }

        private void OnBlocktranHash(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "please input hash";
                return;
            }
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var transfer = dbSnapshot.Transfers.Get(hash);
                if (transfer != null)
                {
                    Block myblk = BlockChainHelper.GetMcBlock(transfer.height);
                    if (transfer.addressIn == "" && transfer.addressOut == "" || transfer.addressIn == null && transfer.addressOut == null)
                    {
                        transfer.addressIn = "00000000000000000000000000000000";
                        transfer.addressOut = myblk.Address;
                    }
                }
                if (transfer == null || (transfer.timestamp == 0 && transfer.type == ""))
                {
                    httpMessage.result = "false";
                }
                else httpMessage.result = JsonHelper.ToJson(transfer);
            }

        }

        public void GetPool(HttpMessage httpMessage)
        {
            Dictionary<string, BlockSub> transfers =  Entity.Root.GetComponent<Rule>().GetTransfers();
            httpMessage.result =  JsonHelper.ToJson(transfers);
        }

        public void OnStats(HttpMessage httpMessage)
        {
            var HttpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (HttpPoolRelay != null)
            {
                OnPoolStats(httpMessage);
                return;
            }

            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                style = "";
            }

            string address = Wallet.GetWallet().GetCurWallet().ToAddress();
            Account account = null;
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                account = dbSnapshot.Accounts.Get(address);
            }

            var rule = Entity.Root.GetComponent<Rule>();
            var pool = Entity.Root.GetComponent<Pool>();
            var amount = account != null ? account.amount : "0";
            long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out long UndoHeight);
            long PoolHeight = rule.height;
            string power1 = rule.calculatePower.GetPower();
            string power2 = Entity.Root.GetComponent<Consensus>().calculatePower.GetPower();

            int nodeCount = Entity.Root.GetComponent<NodeManager>().GetNodeCount();

            if (style == "")
            {
                httpMessage.result =    $"      AlppyHeight: {UndoHeight}\n" +
                                        $"       PoolHeight: {PoolHeight}\n" +
                                        $"  Calculate Power: {power1} of {power2}\n" +
                                        $"          Account: {address}, {amount}\n" +
                                        $"             Node: {nodeCount}\n" +
                                        $"    Rule.Transfer: {rule?.GetTransfersCount()}\n" +
                                        $"    Pool.Transfer: {pool?.GetTransfersCount()}\n" +
                                        $"     NodeSessions: {Program.jdNode["NodeSessions"][0]}\n" +
                                        $"           IsRule: {Entity.Root.GetComponent<Consensus>().IsRule(PoolHeight, address)}";
            }
            else
            if (style == "1")
            {
                httpMessage.result = $"H:{UndoHeight} P:{power2}";
            }
            else
            if (style == "2")
            {
                var httpPool = Entity.Root.GetComponent<HttpPool>();
                var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
                httpMessage.result = $"H:{UndoHeight} P:{power2} Miner:{miners?.Count}/{httpPool?.minerLimit} P1:{power1} Fee:{Pool.GetServiceFee()*100}% {pool?.RewardInterval/4}min {HttpPool.ignorePower/1000}K Ver:{Miner.version.Replace("Miner_","")}";
            }
            else
            if (style == "3")
            {
                httpMessage.result = $"{UndoHeight}";
            }
            else
            if (style == "5")
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("Address", address);
                result.Add("AlppyHeight", UndoHeight);
                result.Add("PoolHeight" , PoolHeight);
                result.Add("Rule", Entity.Root.GetComponent<Consensus>().IsRule(PoolHeight, address));
                result.Add("HardDisk", $"{DbSnapshot.GetHardDiskSpace()/1024}GB");
                httpMessage.result = JsonHelper.ToJson(result);
            }


        }

        public void OnPoolStats(HttpMessage httpMessage)
        {
            var HttpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (HttpPoolRelay == null) {
                OnStats(httpMessage);
                return;
            }

            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                style = "";
            }

            if (style == "")
            {
                var pool        = Entity.Root.GetComponent<Pool>();
                var address     = Wallet.GetWallet().GetCurWallet().ToAddress();
                var httpPool    = Entity.Root.GetComponent<HttpPool>();
                long PoolHeight = httpPool == null ? 0 : httpPool.height;
                string power1   = HttpPoolRelay.calculatePower.GetPower();
                var minerReward = httpPool?.GetMinerRewardMin(out long miningHeight);

                httpMessage.result =    $"       PoolHeight: {PoolHeight}\n" +
                                        $"  Calculate Power: {power1}\n" +
                                        $"          Account: {address}\n" +
                                        $"            Miner: {minerReward?.Count}\n" +
                                        $"    Pool.Transfer: {pool?.GetTransfersCount()}\n";
            }
            else
            if (style == "2")
            {
                long miningHeight = 0;
                var pool = Entity.Root.GetComponent<Pool>();
                var httpPool = Entity.Root.GetComponent<HttpPool>();
                var minerReward = httpPool?.GetMinerRewardMin(out miningHeight);
                string power1 = HttpPoolRelay.calculatePower.GetPower();

                httpMessage.result = $"H:{httpPool?.height} Miner:{minerReward?.Count}/{httpPool?.minerLimit} P1:{power1} Fee:{Pool.GetServiceFee() * 100}% {pool?.RewardInterval / 4}min {HttpPool.ignorePower / 1000}K Ver:{Miner.version.Replace("Miner_", "")}";
            }
            else
            if (style == "3")
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                long miningHeight = 0;
                var pool = Entity.Root.GetComponent<Pool>();
                var httpPool = Entity.Root.GetComponent<HttpPool>();
                var miners = httpPool?.GetMinerRewardMin(out miningHeight);
                string power1 = HttpPoolRelay.calculatePower.GetPower();

                result.Add("H", miningHeight);
                result.Add("P1", power1);
                result.Add("Miner", $"{miners?.Count}/{httpPool?.minerLimit}");
                result.Add("Fee", $"{Pool.GetServiceFee() * 100}%");
                result.Add("RewardInterval", $"{pool?.RewardInterval / 4}");
                result.Add("ignorePower", $"{HttpPool.ignorePower / 1000}K");
                result.Add("Ver", Miner.version.Replace("Miner_", ""));
                httpMessage.result = JsonHelper.ToJson(result);
            }

        }

        public void GetPoolList(HttpMessage httpMessage)
        {
            var httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (httpPoolRelay != null)
            {
                httpMessage.result = $"redirect {httpPoolRelay.rulerWeb}";
            }
            else
            {
                httpMessage.result = JsonHelper.ToJson(HttpPoolRelay.poolInfos);
            }
        }

        public void OnRedirect(HttpMessage httpMessage)
        {
            var httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (httpPoolRelay != null)
            {
                if(httpPoolRelay.rulerWeb.IndexOf("http://")==0)
                    httpMessage.result = "redirect " + httpPoolRelay.rulerWeb;
                else
                    httpMessage.result = "redirect " + "http://" + httpPoolRelay.rulerWeb;
            }
            else
            {
                httpMessage.result = "";
            }
        }

        public void OnRules(HttpMessage httpMessage)
        {
            var consensus = Entity.Root.GetComponent<Consensus>();
            Dictionary<string, RuleInfo> ruleInfos = consensus.GetRule(consensus.transferHeight);
            httpMessage.result = JsonHelper.ToJson(ruleInfos);
        }


        public void OnNode(HttpMessage httpMessage)
        {
#if !RELEASE
            var nodes = Entity.Root.GetComponent<NodeManager>().GetNodeList();
            nodes.Sort((a, b) => b.kIndex - a.kIndex);
            httpMessage.result = JsonHelper.ToJson(nodes);
#else
            var nodes = new List<NodeManager.NodeData>();
            Entity.Root.GetComponent<NodeManager>().GetNodeList().ForEach(x => nodes.Add(new NodeManager.NodeData()
            {
                //ipEndPoint = x.ipEndPoint.Substring(x.ipEndPoint.Length/3, x.ipEndPoint.Length-x.ipEndPoint.Length/3),
                ipEndPoint = "",
                kIndex = x.kIndex,
                nodeId = x.nodeId,
                state = x.state,
                version = x.version,
                address = x.address,
            }));
            nodes.Sort((a, b) => b.kIndex - a.kIndex);
            httpMessage.result = JsonHelper.ToJson(nodes);
#endif
        }

        public void OnMiner(HttpMessage httpMessage)
        {
            var httpPool = Entity.Root.GetComponent<HttpPool>();
            var pool = Entity.Root.GetComponent<Pool>();
            var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
            if (GetParam(httpMessage, "1", "Address", out string address))
            {
                GetParam(httpMessage, "2", "transferIndex", out string str_transferIndex);
                GetParam(httpMessage, "3", "transferColumn", out string str_transferColumn);
                GetParam(httpMessage, "4", "minerIndex", out string str_minerIndex);
                GetParam(httpMessage, "5", "minerColumn", out string str_minerColumn);

                long.TryParse(str_transferIndex, out long transferIndex);
                long.TryParse(str_transferColumn, out long transferColumn);
                long.TryParse(str_minerIndex, out long minerIndex);
                long.TryParse(str_minerColumn, out long minerColumn);

                var minerView = pool.GetMinerView(address,transferIndex, transferColumn, minerIndex, minerColumn);

                httpMessage.result = JsonHelper.ToJson(minerView);
            }
            else
            {
                httpMessage.result  = $"pool.style   : {pool.style}\n";
                httpMessage.result += $"miners.Count : {miners?.Count}\n";
            }
        }

        public void OnMinerTransfer(HttpMessage httpMessage)
        {
            var httpPool = Entity.Root.GetComponent<HttpPool>();
            var pool = Entity.Root.GetComponent<Pool>();
            var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
            if (GetParam(httpMessage, "1", "Address", out string address))
            {
                GetParam(httpMessage, "2", "transferIndex", out string str_transferIndex);
                GetParam(httpMessage, "3", "transferColumn", out string str_transferColumn);
                GetParam(httpMessage, "4", "minerIndex", out string str_minerIndex);
                GetParam(httpMessage, "5", "minerColumn", out string str_minerColumn);
                GetParam(httpMessage, "6", "unique", out string unique);

                long.TryParse(str_transferIndex, out long transferIndex);
                long.TryParse(str_transferColumn, out long transferColumn);
                long.TryParse(str_minerIndex, out long minerIndex);
                long.TryParse(str_minerColumn, out long minerColumn);

                var rel = pool.ReSendTansfer(address, transferIndex, transferColumn, minerIndex, minerColumn, unique);
                if (rel)
                {
                    httpMessage.result = "{\"success\":true}";
                }
                else
                {
                    httpMessage.result = "{\"success\":false,\"rel\":" + rel + "}";
                }
            }
        }

        public void OnMinerTop(HttpMessage httpMessage)
        {
            var httpPool = Entity.Root.GetComponent<HttpPool>();
            var pool = Entity.Root.GetComponent<Pool>();
            var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
            if (GetParam(httpMessage, "1", "minerIndex", out string str_minerIndex))
            {
                GetParam(httpMessage, "2", "minerColumn", out string str_minerColumn);

                long.TryParse(str_minerIndex, out long minerIndex);
                long.TryParse(str_minerColumn, out long minerColumn);

                var minerView = pool.GetMinerTop(minerIndex, minerColumn);

                httpMessage.result = JsonHelper.ToJson(minerView);
            }
            else
            {
                httpMessage.result = $"pool.style   : {pool.style}\n";
                httpMessage.result += $"miners.Count : {miners?.Count}\n";
            }
        }

        public void OnMinerAbstract(HttpMessage httpMessage)
        {
            var httpPool = Entity.Root.GetComponent<HttpPool>();
            var pool = Entity.Root.GetComponent<Pool>();
            var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
            if (GetParam(httpMessage, "1", "Address", out string address))
            {
                var minerView = pool.GetMinerViewAbstract(address);
                httpMessage.result = JsonHelper.ToJson(minerView);
            }
        }

        public void GetAccounts(HttpMessage httpMessage)
        {
            var buffer = Base58.Decode(httpMessage.map["List"]).ToStr();
            var list = JsonHelper.FromJson<List<string>>(buffer);

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var accounts = new Dictionary<string, Account>();
                for (int i = 0; i < list.Count; i++)
                {
                    Account account = dbSnapshot.Accounts.Get(list[i]);
                    if (account == null)
                    {
                        account = new Account() { address = list[i], amount = "0", nonce = 0 };
                    }
                    accounts.Remove(account.address);
                    accounts.Add(account.address, account);
                }
                httpMessage.result = JsonHelper.ToJson(accounts);
            }
        }

        public void OnAccount(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                    httpMessage.result = $"          Account: {account.address}, amount:{account.amount} , nonce:{account.nonce}";
                else
                    httpMessage.result = $"          Account: {address}, amount:0 , index:0";
            }
        }

        public void PoolList(HttpMessage httpMessage)
        {
            GetParam(httpMessage, "1", "style", out string style);

            var poollist = "";
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeRandomList();
            foreach( var value in nodeList )
            {
                try
                {
                    HttpMessage quest = new HttpMessage();
                    quest.map = new Dictionary<string, string>();
                    quest.map.Add("cmd", "stats");
                    quest.map.Add("style", "2");

                    string ipAddress = IPEndPoint.Parse(value.ipEndPoint).Address.ToString();
                    var awaiter = ComponentNetworkHttp.QueryString($"http://{ipAddress}:8101", quest).GetAwaiter();
                    while (!awaiter.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    var temp = $"{value.address} {ipAddress} {awaiter.GetResult()}\n";
                    if (style == "1")
                    {
                        File.AppendAllText("./Miners_no_confused.csv", temp.Replace(" ",",") );
                    }
                    poollist += temp;
                }
                catch (Exception)
                {
                }
            }

            httpMessage.result = poollist;
        }



        public void OnProperty(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var map = new Dictionary<string, string>();

                Account account = dbSnapshot.Accounts.Get(address);
                map.Add("1",  "SAT:" + (account!=null?account.amount:"0") + ":");

                var abc = dbSnapshot.ABC.Get(address);
                var blockSub = new BlockSub();
                var index = 2;
                if (abc != null)
                {
                    var consensus = Entity.Root.GetComponent<Consensus>();
                    var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
                    var amount = "";
                    var symbol = "";
                    object[] result = null;
                    bool rel;
                    for (int ii = 0; ii < abc.Count; ii++)
                    { 
                        blockSub.addressIn = address;
                        blockSub.addressOut = abc[ii];
                        try
                        {
                            if (!luaVMEnv.IsERC(dbSnapshot, blockSub.addressOut,"ERC20"))
                                continue;

                            symbol = LuaContract.GetSymbol(blockSub.addressOut);

                            blockSub.data = $"balanceOf(\"{address}\")";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                            if (rel && result != null && result.Length == 1)
                            {
                                amount = ((string)result[0]) ?? "0";
                                map.Add(index++.ToString(),$"{symbol}:{amount}:{blockSub.addressOut}");
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }
                }
                httpMessage.result = JsonHelper.ToJson(map);
            }
        }


        public void OnLiquidityOf(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: GetLiquidity address factory";
                return;
            }

            if (!GetParam(httpMessage, "2", "Factory", out string factory))
            {
                httpMessage.result = "command error! \nexample: GetLiquidity address factory";
                return;
            }

            if (!GetParam(httpMessage, "3", "Pair", out string pair))
            {
                httpMessage.result = "command error! \nexample: GetLiquidity address factory pair";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var map = new Dictionary<string, string>();

                var abc = dbSnapshot.ABC.Get(address);
                var blockSub = new BlockSub();
                var index = 1;
                if (abc != null)
                {
                    var consensus = Entity.Root.GetComponent<Consensus>();
                    var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
                    object[] result = null;
                    bool rel;
                    for (int ii = 0; ii < abc.Count; ii++)
                    {
                        blockSub.addressIn = address;
                        blockSub.addressOut = abc[ii];
                        try
                        {
                            if (!luaVMEnv.IsERC(dbSnapshot, blockSub.addressOut, pair))
                                continue;

                            blockSub.data = $"liquidityOf(\"{address}\",\"{factory}\")";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                            if (rel && result != null && result.Length >= 1)
                            {
                                map.Add(index++.ToString(), JsonHelper.ToJson(result));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                httpMessage.result = JsonHelper.ToJson(map);
            }
        }

        public void balanceOf(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            GetParam(httpMessage, "2", "token", out string token);

            var map = new Dictionary<string, string>();
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                map.Add("nonce", account==null?"0":account.nonce.ToString());

                if (string.IsNullOrEmpty(token))
                {
                    map.Add("amount", account==null?"0":account.amount);
                }
                else
                {
                    var consensus = Entity.Root.GetComponent<Consensus>();
                    var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();
                    var amount = "";
                    object[] result = null;
                    bool rel;
                    var blockSub = new BlockSub();
                    blockSub.addressIn = address;
                    blockSub.addressOut = token;
                    try
                    {
                        blockSub.data = $"balanceOf(\"{address}\")";
                        rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                        if (rel && result != null && result.Length >= 1)
                        {
                            amount = ((string)result[0]) ?? "0";
                            map.Add("amount", amount);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            httpMessage.result = JsonHelper.ToJson(map);
        }

        public void OnTransfer(HttpMessage httpMessage)
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
                transfer.depend  = System.Web.HttpUtility.UrlDecode(httpMessage.map["depend"]);
            if (httpMessage.map.ContainsKey("remarks"))
                transfer.remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);
            if(httpMessage.map.ContainsKey("extend"))
                transfer.extend  = JsonHelper.FromJson<List<string>>(System.Web.HttpUtility.UrlDecode(httpMessage.map["extend"]));

            //string hash = transfer.ToHash();
            //string sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet()).ToHexString();
            //Log.Info($"{sign} {hash}");

            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel == -1)
            {
                OnTransferAsync(transfer);
            }

            if (rel == -1 || rel == 1)
            {
                httpMessage.result = "{\"success\":true}";
            }
            else
            {
                httpMessage.result = "{\"success\":false,\"rel\":" + rel + "}";
            }
        }

        public static async void OnTransferAsync(BlockSub transfer)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();

            Q2P_Transfer q2p_Transfer = new Q2P_Transfer();
            q2p_Transfer.transfer = JsonHelper.ToJson(transfer);

            var networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeRandomList();

            // 遍历node提交交易，直到找个一个可以出块的节点
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                Session session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                if (session != null && session.IsConnect())
                {
                    var r2p_Transfer = (R2P_Transfer)await session.Query(q2p_Transfer, 5);
                    if (r2p_Transfer != null && r2p_Transfer.rel == "1")
                    {
                        break;
                    }
                }
            }
        }

        public static bool GetParam(HttpMessage httpMessage,string key1,string key2,out string value)
        {
            if (!httpMessage.map.TryGetValue(key1, out value))
            {
                if (!httpMessage.map.TryGetValue(key2, out value))
                    return false;
            }
            return true;
        }

        public void GetTransfers(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "address", out string address)) {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            if (!GetParam(httpMessage, "2", "index", out string indexStr)) {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            GetParam(httpMessage, "3", "token", out string tokenAddress);

            int.TryParse(indexStr, out int getIndex);

            var transfers = new List<BlockSub>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}{tokenAddress}");
                getIndex = TFA_Count - getIndex;
                for (int ii = getIndex; ii > getIndex - 20 && ii > 0; ii--)
                {
                    string hasht = dbSnapshot.List.Get($"TFA__{address}{tokenAddress}", ii-1);
                    if (hasht != null)
                    {
                        var transfer = dbSnapshot.Transfers.Get(hasht);
                        if (transfer != null)
                        {
                            transfers.Add(transfer);
                        }
                    }
                }

                httpMessage.result = JsonHelper.ToJson(transfers);
            }
        }

        public void OnBeRuler(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                httpMessage.result = "command error! \nexample: beRuler password";
                return;
            }
            if (!Wallet.GetWallet().IsPassword(password))
                return;

            OnBeRulerReal(httpMessage);
        }

        public void OnBeRulerReal(HttpMessage httpMessage)
        {
            var consensus = Entity.Root.GetComponent<Consensus>();
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();

            // 判断当前块高度是否接近主线
            if(blockMgr.newBlockHeight-consensus.transferHeight > 1000)
            {
                httpMessage.result = $"{consensus.transferHeight}:{blockMgr.newBlockHeight} The current block height is too low. command BeRuler have been ignore.";
                return;
            }
            Log.Info("AutoBeRuler 8");

            WalletKey key = Wallet.GetWallet().GetCurWallet();

            var  address = key.ToAddress();
            long notice  = 1;
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                {
                    notice = account.nonce + 1;
                }
            }

            BlockSub transfer = new BlockSub();
            transfer.addressIn = address;
            transfer.addressOut = consensus.consAddress;
            transfer.amount = "0";
            transfer.nonce = notice;
            transfer.type = "contract";

            LuaVMCall luaVMCall = new LuaVMCall();
            luaVMCall.fnName = "add";
            luaVMCall.args = new FieldParam[0];
            transfer.data = luaVMCall.Encode();
            transfer.timestamp = TimeHelper.Now();
            transfer.hash = transfer.ToHash();
            transfer.sign = transfer.ToSign(key);

            Log.Info("AutoBeRuler "+ transfer.hash);
            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel == -1)
            {
                OnTransferAsync(transfer);
            }
            httpMessage.result = $"accepted transfer:{transfer.hash}";
        }

        public void OnTransferState(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: transferstate hash";
                return;
            }

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var transfer = dbSnapshot.Transfers.Get(hash);
                if (transfer != null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                    return;
                }
            }

            httpMessage.result = "";
        }

        public void OnTransferState2(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: transferstate hash";
                return;
            }

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var transfer = dbSnapshot.Transfers.Get(hash);
                if (transfer != null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                    return;
                }
            }

            {
                var transfer = Entity.Root.GetComponent<Rule>().GetTransferState(hash);
                if(transfer!=null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                    var temp = JsonHelper.FromJson<BlockSub>(httpMessage.result);
                    if (temp.temp == null) {
                        temp.temp = new List<string>();
                    }
                    temp.temp.Add("Transfer In Queue");
                    httpMessage.result = JsonHelper.ToJson(temp);
                    return;
                }
            }

            var lastblk = Entity.Root.GetComponent<Rule>().GetLastMcBlock();
            if (lastblk != null)
            {
                BlockSub transfer = null;
                long transferHeight = Entity.Root.GetComponent<Consensus>().transferHeight;
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();

                List<Block> blks = new List<Block>();
                var curblk = lastblk;
                for (long height = curblk.height; height > transferHeight; height--)
                {
                    blks.Insert(0, curblk);
                    curblk = blockMgr.GetBlock(curblk.prehash);
                }

                for (var blks_index = 0; blks_index < blks.Count && transfer == null; blks_index++)
                {
                    var mcblk = blks[blks_index];
                    for (int ii = 0; ii < mcblk.linksblk.Count && transfer == null; ii++)
                    {
                        Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                        if (linkblk == null)
                        {
                            continue;
                        }
                        if (linkblk.height != 1)
                        {
                            for (int jj = 0; jj < linkblk.linkstran.Count && transfer == null ; jj++)
                            {
                                if (linkblk.linkstran[jj].hash == hash)
                                {
                                    transfer = linkblk.linkstran[jj];
                                }
                            }
                        }
                    }
                }

                List<Block> linksblks = blockMgr.GetBlock(lastblk.height);
                for (int ii = 0; ii < linksblks.Count && transfer == null; ii++)
                {
                    Block linkblk = linksblks[ii];
                    for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                    {
                        if (linkblk.linkstran[jj].hash == hash)
                        {
                            transfer = linkblk.linkstran[jj];
                        }
                    }
                }

                if (transfer != null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                    var temp = JsonHelper.FromJson<BlockSub>(httpMessage.result);
                    if (temp.temp == null)
                    {
                        temp.temp = new List<string>();
                    }
                    temp.temp.Add("Waiting for block confirmation");
                    httpMessage.result = JsonHelper.ToJson(temp);
                    return;
                }
            }

            httpMessage.result = "";
        }

        public void UniqueTransfer(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "unique", out string unique))
            {
                httpMessage.result = "command error! \nexample: UniqueTransfer hash";
                return;
            }

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                string hasht = dbSnapshot.Get($"unique_{unique}");
                var transfer = dbSnapshot.Transfers.Get(hasht);
                if (transfer != null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                }
                else
                    httpMessage.result = "";
            }
        }

        public void callFun(HttpMessage httpMessage)
        {
            httpMessage.result = "command error! \nexample: contract consAddress callFun";
            GetParam(httpMessage, "1", "consAddress", out string consAddress);
            if (!GetParam(httpMessage, "2", "data", out string data))
            {
                return;
            }
            data = System.Web.HttpUtility.UrlDecode(data);

            WalletKey key = Wallet.GetWallet().GetCurWallet();
            var sender = key.ToAddress();

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
            {
                var consensus = Entity.Root.GetComponent<Consensus>();
                var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

                var blockSub = new BlockSub();
                blockSub.addressIn  = sender;
                blockSub.addressOut = consAddress;
                blockSub.data       = data;

                try
                {
                    if (luaVMEnv.IsERC(dbSnapshot, consAddress, ""))
                    {
                        bool rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out object[] result);
                        if (rel)
                        {
                            if (result != null)
                            {
                                if (result.Length == 1)
                                    httpMessage.result = JsonHelper.ToJson(result[0]);
                                else
                                    httpMessage.result = JsonHelper.ToJson(result);
                            }
                        }
                        else
                        {
                            httpMessage.result = "error";
                        }
                    }
                }
                catch(Exception)
                {
                }
            }
        }

        //public void GetMnemonicWord(HttpMessage httpMessage)
        //{
        //    if (!GetParam(httpMessage, "1", "passwords", out string password))
        //    {
        //        httpMessage.result = "command error! \nexample: getmnemonic password";
        //        return;
        //    }

        //    if(Wallet.GetWallet().IsPassword(password))
        //    {
        //        var walletKey = Wallet.GetWallet().GetCurWallet();
        //        httpMessage.result = walletKey.random.ToHexString();
        //    }
        //    else
        //    {
        //        string randomSeed = CryptoHelper.Sha256(password);
        //        httpMessage.result = randomSeed;
        //    }
        //}

        public void GetNearBlock(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "browserIndex", out string browserIndexStr))
            {
                httpMessage.result = "command error! \nexample: GetNearBlock browserIndex";
                return;
            }
            long.TryParse(browserIndexStr, out long browserIndex);

            List<Block> list = new List<Block>();
            // 高度差太大的块直接忽略
            var consensus = Entity.Root.GetComponent<Consensus>();
            var blockMgr  = Entity.Root.GetComponent<BlockMgr>();
            var rule      = Entity.Root.GetComponent<Rule>();
            var showCount = 19;

            var height = browserIndex != 0 ? browserIndex : consensus.transferHeight;
            for (long ii = height; ii > height - showCount && ii > 0; ii--)
            {
                Block myblk = BlockChainHelper.GetMcBlock(ii);
                if(myblk!=null)
                    list.Add(myblk);
            }

            if (consensus.transferHeight <= height&& rule.state!=0)
            {
                var last = rule.GetLastMcBlock();
                if (last != null)
                {
                    var pre = blockMgr.GetBlock(last.prehash);
                    if (pre!=null&&pre.height != consensus.transferHeight)
                    {
                        list.Insert(0, pre);
                        if(list.Count> showCount)
                            list.RemoveAt( list.Count-1 );
                    }
                    list.Insert(0, last);
                    if (list.Count> showCount)
                        list.RemoveAt(list.Count - 1);
                }
            }

            List<Block> list2 = new List<Block>();
            for (int ii = 0; ii < list.Count ; ii++)
            {
                Block blk = list[ii].GetHeader();
                list2.Add(blk);
            }

            httpMessage.result = JsonHelper.ToJson(list2);
        }

        public void GetBlock(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: GetBlock hash";
                return;
            }
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var blk = blockMgr.GetBlock(hash);
            if(blk!=null)
                httpMessage.result = JsonHelper.ToJson(blk);
            else
                httpMessage.result = "";
        }

        public void Test(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                return;
            }
            if (!Wallet.GetWallet().IsPassword(password))
                return;

            if (!GetParam(httpMessage, "2", "style", out string style))
            {
                httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                return;
            }

            httpMessage.result = "";
            if (style == "1")
            {
                if (!GetParam(httpMessage, "3", "Address", out string Address))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "4", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }

                LevelDBStore.Export2CSV_Transfer($"{file}", Address);
                httpMessage.result = $"{file} 导出完成";
            }
            else
            if (style == "2")
            {
                OneThreadSynchronizationContext.Instance.Post(this.Test2Async, null);

            }
            else
            if (style == "3")
            {
                OneThreadSynchronizationContext.Instance.Post(this.Test3Async, null);

            }
            else
            if (style == "4")
            {
                LevelDBStore.Export2CSV_Accounts($"C:\\Accounts_test4.csv");
            }
            else
            if (style == "5")
            {
                if (!GetParam(httpMessage, "3", "address", out string address)
                  ||!GetParam(httpMessage, "4", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                GetParam(httpMessage, "5", "token", out string token);

                LevelDBStore.Export2CSV_Account(file, address, token);
            }
            else
            if (style == "1408123")
            {
                if (!GetParam(httpMessage, "3", "min", out string min)
                 || !GetParam(httpMessage, "4", "max", out string max)
                 || !GetParam(httpMessage, "5", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test passwaor 1408123 min max  C:\\name.csv";
                    return;
                }
                TestContract.Test1408123(file, long.Parse(min), long.Parse(max));
            }
            else
            if (style == "pledge")
            {
                TestContract.TestPledge(httpMessage);
            }
            else
            if (style == "transfer")
            {
                TestContract.TestTransfer(httpMessage);
            }
            else
            if (style.ToLower() == "transferall")
            {
                if (!GetParam(httpMessage, "3", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "4", "min", out string smin))
                {
                    return;
                }
                if (!GetParam(httpMessage, "5", "max", out string smax))
                {
                    return;
                }
                long.TryParse(smin, out long lmin);
                long.TryParse(smax, out long lmax);

                TestContract.TransferAll(file, lmin, lmax);
            }
        }

        public long DelBlock_min;
        public long DelBlock_max;
        public void DelBlock(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "password", out string password)
              ||!GetParam(httpMessage, "2", "from", out string from)
              ||!GetParam(httpMessage, "3", "to", out string to) )
            {
                httpMessage.result = "command error! \nexample: mergechain passwaord 110 100";
                return;
            }

            if (Wallet.GetWallet().IsPassword(password))
            {
                DelBlock_max = Math.Max(long.Parse(from), long.Parse(to));
                DelBlock_min = Math.Min(long.Parse(from), long.Parse(to));

                Entity.Root.GetComponent<Consensus>().AddRunAction(DelBlockAsync);
            }
        }

        public void DelBlockAsync()
        {
            long max = DelBlock_max;
            long min = DelBlock_min;

            Log.Info($"MergeChain {max} {min}");

            Entity.Root.GetComponent<Consensus>().transferHeight = min;
            Entity.Root.GetComponent<LevelDBStore>().UndoTransfers(min);
            for (long ii = max; ii > min; ii--)
            {
                Entity.Root.GetComponent<BlockMgr>().DelBlock(ii);
            }

            Log.Info("MergeChain finish");
        }

        public void MergeChain(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                httpMessage.result = "command error! \nexample: MergeChain password";
                return;
            }
            if (Wallet.GetWallet().IsPassword(password))
            {
                Log.Info("MergeChain");
                DelBlock_min = Entity.Root.GetComponent<Consensus>().transferHeight - 6;
                DelBlock_max = DelBlock_min + 13;

                Entity.Root.GetComponent<Consensus>().AddRunAction(DelBlockAsync);
            }
        }

        public void onDBReset(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                httpMessage.result = "command error! \nexample: DBReset password";
                return;
            }
            if (Wallet.GetWallet().IsPassword(password))
            {
                Log.Info("LevelDBStore.Reset");
                Entity.Root.GetComponent<LevelDBStore>().Reset();
            }
        }

        public void PoolTransfer(HttpMessage httpMessage)
        {
            httpMessage.result = "command error!";
            httpMessage.result += "\nexample 1: PoolTransfer password file.csv address";
            httpMessage.result += "\nexample 2: PoolTransfer password file.csv all";
            httpMessage.result += "\nexample 3: PoolTransfer password file.csv minheight maxheight";

            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                return;
            }

            if (Wallet.GetWallet().IsPassword(password))
            {
                if (!GetParam(httpMessage, "2", "file", out string file))
                {
                    return;
                }

                if (!GetParam(httpMessage, "3", "param2", out string param2))
                {
                    return;
                }

                try
                {

                    var addTransCount = 0;
                    var transferProcess = Entity.Root.GetComponent<TransferProcess>();
                    string allText = File.ReadAllText($"./{file}", System.Text.Encoding.UTF8);
                    allText = allText.Replace("}{", "}\n{");
                    string[] arr = allText.Split("\n");

                    var list = new List<TransferProcess.TransferHandle>();
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var transferHandle = JsonHelper.FromJson<TransferProcess.TransferHandle>(arr[i]);
                        if (transferHandle != null && !string.IsNullOrEmpty(transferHandle.unique))
                        {
                            list.Add(transferHandle);
                        }

                        var blockSub = JsonHelper.FromJson<BlockSub>(arr[i]);
                        if (blockSub != null && !string.IsNullOrEmpty(blockSub.data))
                        {
                            list.Add(new TransferProcess.TransferHandle() { addressIn = blockSub.addressIn, addressOut = blockSub.addressOut, amount = blockSub.amount, unique = blockSub.data });
                        }
                    }

                    if (Wallet.CheckAddress(param2))
                    {
                        var address = param2;
                        for (int i = 0; i < list.Count; i++)
                        {
                            var transferHandle = list[i];
                            if (transferHandle.addressOut == address)
                            {
                                addTransCount++;
                                transferProcess.AddTransferHandle(transferHandle.addressIn, transferHandle.addressOut, transferHandle.amount, transferHandle.unique, transferHandle.lastHeight);
                            }
                        }
                    }
                    else
                    if (param2.ToLower() == "all")
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var transferHandle = list[i];
                            addTransCount++;
                            transferProcess.AddTransferHandle(transferHandle.addressIn, transferHandle.addressOut, transferHandle.amount, transferHandle.unique, transferHandle.lastHeight);
                        }
                    }
                    else
                    if (long.TryParse(param2, out long minHeight))
                    {
                        GetParam(httpMessage, "4", "param3", out string param3);
                        if (!long.TryParse(param3, out long maxHeight))
                        {
                            maxHeight = long.MaxValue;
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            var transferHandle = list[i];
                            if (minHeight <= transferHandle.lastHeight && transferHandle.lastHeight <= maxHeight)
                            {
                                addTransCount++;
                                transferProcess.AddTransferHandle(transferHandle.addressIn, transferHandle.addressOut, transferHandle.amount, transferHandle.unique, transferHandle.lastHeight);
                            }
                        }
                    }
                    httpMessage.result = $"BadRecord: {arr.Length}, AddTransCount:{addTransCount}";
                }
                catch (Exception e)
                {
                    httpMessage.result = e.ToString();
                }
            }
        }

        public void PledgeReport(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "password", out string password))
            {
                return;
            }
            if (!GetParam(httpMessage, "2", "Address", out string address))
            {
                return;
            }

            if (Wallet.GetWallet().IsPassword(password))
            {
                LevelDBStore.PledgeReport(address);
            }

        }

        static Dictionary<string, long> AccountNotice = new Dictionary<string, long>();
        public static long GetAccountNotice(string address,bool reset=true)
        {
            long notice = 0;
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                if (!AccountNotice.TryGetValue(address, out notice))
                {
                    if (account != null)
                        notice = account.nonce;
                }

                // 中间有交易被丢弃了
                if (reset && account != null && account.nonce < notice - 20)
                {
                    notice = account.nonce;
                }

                notice += 1;

                AccountNotice.Remove(address);
                AccountNotice.Add(address, notice);
            }
            return notice;
        }

        public static async ETTask<Session> OnTransferAsync2(BlockSub transfer ,Session session2)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();

            Q2P_Transfer q2p_Transfer = new Q2P_Transfer();
            q2p_Transfer.transfer = JsonHelper.ToJson(transfer);

            var networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeRandomList();

            // 遍历node提交交易，直到找个一个可以出块的节点
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[ i ];
                Session session = session2 ?? await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
                if (session != null && session.IsConnect())
                {
                    var r2p_Transfer = (R2P_Transfer)await session.Query(q2p_Transfer, 5);
                    if (r2p_Transfer != null && r2p_Transfer.rel != "-1")
                    {
                        return session;
                    }
                }
            }
            await Task.Delay(10);
            return null;
        }

        public void getLinkBad(HttpMessage httpMessage)
        {
            try
            {
                var consensus = Entity.Root.GetComponent<Consensus>();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();
                var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();

                string _recentheight;
                string address;
                if (!GetParam(httpMessage, "height", "height", out _recentheight))
                {
                    _recentheight = consensus.transferHeight.ToString();
                }
                if (!GetParam(httpMessage, "address", "address", out address))
                {
                    return;
                }

                long recentheight = long.Parse(_recentheight)+1;
                List<string> res = new List<string>();

                Block myBlk = null;
                var hashs_1 = levelDBStore.Heights.Get((recentheight-1).ToString());
                for (int ii = 0; ii < hashs_1.Count; ii++)
                {
                    var linkBlock = blockMgr.GetBlock(hashs_1[ii]);
                    if (linkBlock!=null&& linkBlock.Address==address)
                    {
                        myBlk = linkBlock;
                    }
                }
                if (myBlk == null)
                {
                    httpMessage.result  = $"address: {address} , height: { myBlk.height}\n";
                    httpMessage.result += "no find blk\n";
                    return;
                }

                res.Add($"Query address: {address} , blk: {myBlk.hash} , pre: {myBlk.prehash} height: {myBlk.height}\n");
                Block mcblk2 = BlockChainHelper.GetMcBlock(recentheight-1);
                res.Add($"mcblk address: {mcblk2.Address} , blk: {mcblk2.hash} , pre: {mcblk2.prehash} height: {mcblk2.height}\n");

                Block mcblk = BlockChainHelper.GetMcBlock(recentheight);

                var hashs_2 = levelDBStore.Heights.Get((recentheight).ToString());
                for (int ii = 0; ii < hashs_2.Count; ii++)
                {
                    var linkBlock = blockMgr.GetBlock(hashs_2[ii]);
                    var first = linkBlock.linksblk.Values.FirstOrDefault( x => x == myBlk.hash);
                    var mcflag = "";

                    if (linkBlock.hash == mcblk.hash)
                    {
                        mcflag = $"<---- MCBlock height: {mcblk.height}";
                    }

                    if (!string.IsNullOrEmpty(first))
                    {
                        res.Add($"link {linkBlock.Address} , blk: {linkBlock.hash} {mcflag}\n");
                    }
                    else
                    {
                        res.Add($"bad  {linkBlock.Address} , blk: {linkBlock.hash} {mcflag}\n");

                    }
                }

                httpMessage.result = "";
                for (int ii = 0; ii < res.Count; ii++)
                {
                    httpMessage.result += res[ii];
                }
                //httpMessage.result = JsonHelper.ToJson(res);
            }
            catch (Exception ex)
            {
                httpMessage.result = "";
            }
        }

        public async void Test2Async(object o)
        {
            for (int ii = Wallet.GetWallet().keys.Count; ii < 1000; ii++)
            {
                Wallet.GetWallet().Create();
            }
            Wallet.GetWallet().SaveWallet();

            Log.Info("Test2Async start1");

            Session session2 = null;
            for (int ii = 1; ii < 1000; ii++)
            {
                int random1 = 0;
                int random2 = ii;
                int random3 = 1000;

                BlockSub transfer   = new BlockSub();
                transfer.type       = "transfer";
                transfer.addressIn  = Wallet.GetWallet().keys[random1].ToAddress();
                transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                transfer.amount     = random3.ToString();
                transfer.data       = "";
                transfer.depend     = "";
                transfer.nonce      = GetAccountNotice(transfer.addressIn, false);
                transfer.timestamp  = TimeHelper.Now();
                transfer.hash       = transfer.ToHash();
                transfer.sign       = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                session2 = await OnTransferAsync2(transfer, session2);
                while (session2 == null) {
                    session2 = await OnTransferAsync2(transfer, session2);
                };
            }

        }

        public async void Test3Async(object o)
        {
            Session session2 = null;

            var accountCount = Wallet.GetWallet().keys.Count;
            while (true)
            {
                Log.Info("Test2Async 200");
                session2 = null;
                for (int ii = 0; ii < 200; ii++)
                {
                    int random1 = RandomHelper.Range(0, accountCount);
                    int random2 = RandomHelper.Range(0, accountCount);
                    while(random1== random2)
                        random2 = RandomHelper.Range(0, accountCount);
                    int random3 = RandomHelper.Range(1, 100);

                    BlockSub transfer   = new BlockSub();
                    transfer.type       = "transfer";
                    transfer.addressIn  = Wallet.GetWallet().keys[random1].ToAddress();
                    transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                    transfer.amount = random3.ToString();
                    transfer.data   = "";
                    transfer.depend = "";
                    transfer.nonce = GetAccountNotice(transfer.addressIn);
                    transfer.timestamp = TimeHelper.Now();
                    transfer.hash   = transfer.ToHash();
                    transfer.sign   = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                    session2 = await OnTransferAsync2(transfer, session2);
                    while (session2 == null)
                    {
                        session2 = await OnTransferAsync2(transfer, session2);
                    };
                }
                await Task.Delay(1000);
            }
        }


    }


}