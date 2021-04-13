using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ETModel
{
    // 外网连接
    public class SmartxRpc : Component
    {
        ComponentNetworkHttp networkHttp;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"smartxRpc http://{networkHttp.ipEndPoint}/");

        }
        
        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || networkHttp == null
             || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;

            string cmd = null;
            //httpMessage.map.TryGetValue("v1.0.0/global-info", out cmd);
            httpMessage.map.TryGetValue("cmd", out cmd);
            cmd = cmd.ToLower();

            switch (cmd)
            {
                case "contract":
                    OnContractTran(httpMessage);
                    break;
                case "checktran":
                    checkTran(httpMessage);
                    break;
                case "account":
                    OnAccount(httpMessage);
                    break;
                case "stats":
                    OnStats(httpMessage);
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
                case "beruler":
                    OnBeRuler(httpMessage);
                    break;
                case "gettransfers":
                    GetTransfers(httpMessage);
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
                case "balance":
                    OnBalance(httpMessage);
                    break;
                case "balanceof":
                    balanceOf(httpMessage);//获取代币信息
                    break;
                case "callfun"://创建代币
                    callFun(httpMessage);
                    break;
                case "global-info": //信息
                    OnGlobal_info(httpMessage);
                    break;
                case "latest-block": //获取交易的列表
                    OnLatest_block(httpMessage);
                    break;
                case "block-by-hash": //根据主块查询信息
                    OnBlockHash(httpMessage);
                    break;
                case "getnonce": //根据主块查询信息
                    OnGetNonce(httpMessage);
                    break;
                case "getproperty": //根据地址查询所拥有的代币
                    OnProperty(httpMessage);
                    break;
                case "getliquidity"://获取质押可约
                    OnLiquidityOf(httpMessage);
                    break;
                case "block-by-tranhash": //根据交易查询 交易的信息
                    OnBlocktranHash(httpMessage);
                    break;
                case "transactions": //根据地址查询该地址的交易记录
                    OnTransactions(httpMessage);
                    break;
                case "info":  //info
                    OnInfo(httpMessage);
                    break;
                case "latest-block-height"://latest-block-height
                    OnLatestBlockHeight(httpMessage);
                    break;
                case "transfercount":
                    OnTransferCount(httpMessage);
                    break;
                case "miner":
                    OnMiner(httpMessage);
                    break;
                case "mempool":
                    GetPool(httpMessage);
                    break;
                case "search":
                    OnSearch(httpMessage);
                    break;
                case "getlastblock":
                    GetNearBlock(httpMessage);
                    break;
                case "adressreg":
                    AdressReg(httpMessage);
                    break;
                case "outleveldb":
                    OutLeveldb(httpMessage);
                    break;
                case "hello":
                    {
                        httpMessage.result = "welcome join SmartX.net";
                    }
                    break;
                default:
                    break;
            }
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
                LevelDBStore.test_ergodic2(long.Parse(height), filename);
                httpMessage.result = "导出完成";
            }
        }

        private void OnContractTran(HttpMessage httpMessage)
        {
            string indexStr;
            if (!GetParam(httpMessage, "Index", "index", out  indexStr))
            {
                indexStr = "0";
            }
            int index = int.Parse(indexStr);
            long num = 0;
            OnProperty(httpMessage);
            Dictionary<string, string> res = new Dictionary<string, string>();
            res = JsonHelper.FromJson<Dictionary<string, string>>(httpMessage.result);
            
            string address = httpMessage.map["address"];
            var transfers = new List<BlockSub>();

            foreach (KeyValuePair<string, string> kv in res)
            {
                string[] arr = kv.Value.Split(":");
                if (arr[2] == "")
                {
                    continue;
                }
                string tokenAddress = arr[2];

                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}{tokenAddress}");
                    if (num > (12 * (index+1)))
                    {
                        break;
                    }
                    for (int ii = TFA_Count; ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{address}{tokenAddress}", ii - 1);
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                transfers.Add(transfer);
                                transfer.temp.Add(arr[0]);
                            }
                        }
                    }
                }
            }

            List<BlockSub> SortList = transfers.OrderByDescending(it => it.timestamp).ToList().Skip(index*12).Take(12).ToList();
            if(SortList.Count>0)
            SortList[0].temp.Add(transfers.Count.ToString());
            httpMessage.result = JsonHelper.ToJson(SortList);
        }

        public void GetPool(HttpMessage httpMessage)
        {
            Dictionary<string, BlockSub> transfers = Entity.Root.GetComponent<Rule>().GetTransfers();
            httpMessage.result = JsonHelper.ToJson(transfers);
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
                    if (httpMessage.result == "[]")
                        httpMessage.result = "";
                }
            }

        }

        private void OnTransferCount(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "please input address";
                return;
            }
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}");
                httpMessage.result = "{\"count\":" + TFA_Count + "}";
            }
        }
        public void OnProperty(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var map = new Dictionary<string, string>();

                Account account = dbSnapshot.Accounts.Get(address);
                map.Add("1", "SAT:" + (account != null ? account.amount : "0") + ":");

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
                            if (!luaVMEnv.IsERC(dbSnapshot, blockSub.addressOut, "ERC20"))
                                continue;
                            blockSub.data = "symbol()";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                            if (rel && result != null && result.Length >= 1)
                            {
                                symbol = ((string)result[0]) ?? "unknown";
                            }

                            blockSub.data = $"balanceOf(\"{address}\")";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                            if (rel && result != null && result.Length == 1)
                            {
                                amount = ((string)result[0]) ?? "0";
                                map.Add(index++.ToString(), $"{symbol}:{amount}:{blockSub.addressOut}");
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
        private void OnGetNonce(HttpMessage httpMessage)
        {
            string address = httpMessage.map["address"];
            List<string> addr = new List<string>();
            addr.Add(address);
            List<string> list = new List<string>();
            list.Add(Base58.Encode(System.Text.Encoding.UTF8.GetBytes("" + JsonHelper.ToJson(addr))));
            httpMessage.map.Clear();
            httpMessage.map.Add("List", list[0]);//JsonHelper.ToJson(list);

            var buffer = Base58.Decode(httpMessage.map["List"]).ToStr();
            var list2 = JsonHelper.FromJson<List<string>>(buffer);
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(list2[0]);
                if (account != null)
                {
                    httpMessage.result = $"{{\"success\":true,\"message\":\"successful operation\",\"nonce\":{account.nonce}}}";
                }
                else
                {
                    httpMessage.result = $"{{\"success\":true,\"message\":\"successful operation\",\"nonce\":{0}}}";
                }
            }
        }

        private void OnBalance(HttpMessage httpMessage)
        {
            //Balance balance = new Balance();
            Dictionary<string, object> balance = new Dictionary<string, object>();
            balance.Add("success",true);
            balance.Add("message","successful operation");
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (!GetParam(httpMessage, "Address", "address", out string address))
            {
                httpMessage.result = "please input address.";
                return;
            }

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                {
                    result.Add("address", address);
                    result.Add("available" , account.amount);
                    balance.Add("result",result);
                    httpMessage.result = JsonHelper.ToJson(balance);
                    return;
                }
                httpMessage.result = "false";
            }
            httpMessage.result = "false";
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
                        transfer.addressOut = "00000000000000000000000000000000";
                    }
                   
                    if (transfer.type == "contract")
                    {
                        string[] arr=transfer.data.Replace("\"","").Replace("\\","").Replace("transfer(","").Replace(")","").Split(",");
                        transfer.amount = arr[arr.Length-1];
                        if (transfer.data.Contains("transfer("))
                        {
                            httpMessage.map.Clear();
                            httpMessage.map.Add("address", transfer.addressIn);
                            OnProperty(httpMessage);
                            var res = JsonHelper.FromJson<Dictionary<string, string>>(httpMessage.result);
                            foreach (KeyValuePair<string, string> kv in res)
                            {
                                if (kv.Value.Contains(transfer.addressOut))
                                {
                                    transfer.temp = new List<string>();
                                    transfer.temp.Add( kv.Value.Split(":")[0]);
                                    break;
                                }
                            }
                        }
                    }
                }
                
                if (transfer == null||transfer.timestamp == 0)
                    {
                        httpMessage.result = "false";
                    }
                else httpMessage.result = JsonHelper.ToJson(transfer);
            }
            
        }

        private void OnLatestBlockHeight(HttpMessage httpMessage)
        {
            var consensus = Entity.Root.GetComponent<Consensus>();
            var height = consensus.transferHeight;
            //LatestBlockHeight latestBlockHeight = new LatestBlockHeight();
            Dictionary<string, object> latestBlockHeight = new Dictionary<string, object>();
            latestBlockHeight.Add("success", true);
            latestBlockHeight.Add("message", "successful operation");
            latestBlockHeight.Add("height" ,height.ToString());
            httpMessage.result = JsonHelper.ToJson(latestBlockHeight);
        }

        private void OnInfo(HttpMessage httpMessage)
        {
            //Infomsg infomsg = new Infomsg();
            Dictionary<string, object> infomsg = new Dictionary<string, object>();
            infomsg.Add("success", true);
            infomsg.Add("message", "successful operation");
            Dictionary<string, object> result = new Dictionary<string, object>();

            var consensus = Entity.Root.GetComponent<Consensus>();
            var height = consensus.transferHeight;
            Block myblk = BlockChainHelper.GetMcBlock(height);
            result.Add("network ", "testnet");
            string[] capabilities = { "SMARTX", "FAST_SYNC" };
            result.Add("capabilities", capabilities);
            result.Add("clientId", "Smartx/v1.0.0-Windows/amd64");
            result.Add("coinbase", "0xcc1ca38eda5dd60d243633e95b893a14c44b8df8");
            result.Add("activePeers",2);
            result.Add("pendingTransactions",10);
            result.Add("latestBlockNumber", height.ToString());
            result.Add("latestBlockHash" , myblk.hash);
            infomsg.Add("result",result);
            httpMessage.result = JsonHelper.ToJson(infomsg);
        }

        private void OnTransactions(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "Index", "index", out string index))
            {
                index = "0";
            }
            int Index = int.Parse(index);
            List<BlockSub> transactionList = new List<BlockSub>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                if (!GetParam(httpMessage, "1", "address", out string address))
                {
                    httpMessage.result = "please input address";
                }
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}");
                    Index = TFA_Count - Index * 12;

                    for (int ii = Index; ii > Index - 12 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{address}", ii-1);
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

        private void checkTran(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "accindex", out string index))
            {
                httpMessage.result = "please input account.index";
            }
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                if (!GetParam(httpMessage, "1", "address", out string address))
                {
                    httpMessage.result = "please input address";
                }
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    httpMessage.result = "false";
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}");
                    for (int ii = TFA_Count; ii >= int.Parse(index) && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{address}",ii-1);
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                if (transfer.hash == httpMessage.map["hash"])
                                {
                                    httpMessage.result = "true";
                                    break;
                                }

                            }
                        }
                    }
                }
                
            }



        }

        private void OnBlockHash(HttpMessage httpMessage)
        {
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "please input hash";
            }
            var myblk = blockMgr.GetBlock(hash);
            if (myblk == null || myblk.timestamp ==0 ) 
            {
                httpMessage.result = "false";
            }

            else httpMessage.result = JsonHelper.ToJson(myblk);
        }
        
        private void OnLatest_block(HttpMessage httpMessage)
        {
            var consensus = Entity.Root.GetComponent<Consensus>();
            var showCount = 10;

            var height = consensus.transferHeight;

            if (!GetParam(httpMessage, "Index", "index", out string index))
            {
                index = "0";
            }

            List<Block> BlockList = new List<Block>();
            for (long ii = height - showCount * int.Parse(index); ii > height - showCount * (int.Parse(index) + 1) && ii > 0; ii--)
            {
                Block myblk = BlockChainHelper.GetMcBlock(ii);

                if (myblk != null)
                {
                    BlockList.Add(myblk);
                }
            }
            httpMessage.result = JsonHelper.ToJson(BlockList);
        }

        public void OnGlobal_info(HttpMessage httpMessage)
        {
            long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out long UndoHeight);
            var blk = BlockChainHelper.GetMcBlock(UndoHeight);

            Dictionary<string, object> globalInfo = new Dictionary<string, object>();
            globalInfo.Add("success", true);
            globalInfo.Add("message", "successful operation");
            globalInfo.Add("currentBlockHeight" , UndoHeight.ToString());
            globalInfo.Add("globalDifficulty" , blk.GetDiff().ToString());
            globalInfo.Add("globalHashRate" , Entity.Root.GetComponent<Consensus>().calculatePower.GetPower());
            httpMessage.result = JsonHelper.ToJson(globalInfo);
        }

        // console 支持
       
        public void OnStats(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                style = "";
            }
            Dictionary<string, object> result = new Dictionary<string, object>();
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

            int NodeCount = Entity.Root.GetComponent<NodeManager>().GetNodeCount();

            if (style == "")
            {
                result.Add("AlppyHeight", UndoHeight);
                result.Add("PoolHeight", PoolHeight);
                result.Add("Calculate Power",$"{power1}of{power2}");
                result.Add("Account", $"{ address},{amount}");
                result.Add("Node", NodeCount);
                result.Add("Rule.Transfer", rule?.GetTransfersCount());
                result.Add("Pool.Transfer", pool?.GetTransfersCount());
                httpMessage.result = JsonHelper.ToJson(result);
                //httpMessage.result = $"      AlppyHeight: {UndoHeight}\n" +
                //                        $"       PoolHeight: {PoolHeight}\n" +
                //                        $"  Calculate Power: {power1} of {power2}\n" +
                //                        $"          Account: {address}, {amount}\n" +
                //                        $"             Node: {NodeCount}\n" +
                //                        $"    Rule.Transfer: {rule?.GetTransfersCount()}\n" +
                //                        $"    Pool.Transfer: {pool?.GetTransfersCount()}";

            }
            else
            if (style == "1")
            {
                result.Add("H", UndoHeight);
                result.Add("P", power2);
                httpMessage.result = JsonHelper.ToJson(result);
                //httpMessage.result = $"H:{UndoHeight} P:{power2}";
            }
            else
            if (style == "2")
            {
                var httpPool = Entity.Root.GetComponent<HttpPool>();
                var miners = httpPool.GetMinerReward(out long miningHeight);
                result.Add("H", UndoHeight);
                result.Add("P", power2);
                result.Add("P1", power1);
                result.Add("Miner", miners?.Count);
                httpMessage.result = JsonHelper.ToJson(result);
                //httpMessage.result = $"H:{UndoHeight} P:{power2} Miner:{miners?.Count}";
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
            var nodes = Entity.Root.GetComponent<NodeManager>().GetNodeList();
            nodes.Sort((a, b) => b.kIndex - a.kIndex);
            httpMessage.result = JsonHelper.ToJson(nodes);
        }

        public void OnMiner(HttpMessage httpMessage)
        {
            var httpPool = Entity.Root.GetComponent<HttpPool>();
            var pool = Entity.Root.GetComponent<Pool>();
            var miners = httpPool.GetMinerReward(out long miningHeight);
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
        
        public void OnAccount(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "Address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            Dictionary<string, object> result = new Dictionary<string, object>();
            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                Account account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    result.Add("Account", account.address);
                    result.Add("amount", account.amount);
                    result.Add("nonce", account.nonce);
                    httpMessage.result = JsonHelper.ToJson(result);
                }
                else
                {
                    result.Add("Account", address);
                    result.Add("amount", 0);
                    result.Add("nonce", 0);
                    httpMessage.result = JsonHelper.ToJson(result);
                }
                   // httpMessage.result = $"          Account: {address}, amount:0 , index:0";
            }
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
            transfer.depend = httpMessage.map["depend"];
            transfer.remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);

            //string hash = transfer.ToHash();
            //string sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet()).ToHexString();

            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel == -1)
            {
                OnTransferAsync(transfer);
            }

            if (rel == -1 || rel == 1)
            {
                using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    long index = dbSnapshot.List.GetCount($"TFA__{transfer.addressIn}{""}");
                    httpMessage.result = "{\"success\":true,\"accindex\":" + index + "} ";

                }
                
            }
            else
            {
                httpMessage.result = "{\"success\":false,\"rel\":" + rel + "}";
            }
        }

        public async void OnTransferAsync(BlockSub transfer)
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
                    if (r2p_Transfer != null && r2p_Transfer.rel != "-1")
                    {
                        break;
                    }
                }
            }
        }

        public bool GetParam(HttpMessage httpMessage, string key1, string key2, out string value)
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
            if (!GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "command error! \nexample: account address";
                return;
            }
            if (!GetParam(httpMessage, "2", "index", out string indexStr))
            {
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
                    string hasht = dbSnapshot.List.Get($"TFA__{address}{tokenAddress}", ii - 1);
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
            Consensus consensus = Entity.Root.GetComponent<Consensus>();
            WalletKey key = Wallet.GetWallet().GetCurWallet();

            var address = key.ToAddress();
            long notice = 1;
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
                    httpMessage.result = JsonHelper.ToJson(transfer);
                else
                    httpMessage.result = "";
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
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var rule = Entity.Root.GetComponent<Rule>();
            var showCount = 19;

            var height = browserIndex != 0 ? browserIndex : consensus.transferHeight;
            for (long ii = height; ii > height - showCount && ii > 0; ii--)
            {
                Block myblk = BlockChainHelper.GetMcBlock(ii);
                if (myblk != null)
                    list.Add(myblk);
            }

            if (consensus.transferHeight <= height && rule.state != 0)
            {
                var last = rule.GetLastMcBlock();
                if (last != null)
                {
                    var pre = blockMgr.GetBlock(last.prehash);
                    if (pre.height != consensus.transferHeight)
                    {
                        list.Insert(0, pre);
                        if (list.Count > showCount)
                            list.RemoveAt(list.Count - 1);
                    }
                    list.Insert(0, last);
                    if (list.Count > showCount)
                        list.RemoveAt(list.Count - 1);
                }
            }

            List<Block> list2 = new List<Block>();
            for (int ii = 0; ii < list.Count; ii++)
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
            if (blk != null)
                httpMessage.result = JsonHelper.ToJson(blk);
            else
                httpMessage.result = "";
        }

        public void AdressReg(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "adress1", out string adress1))
            {
                httpMessage.result = "command error! \nexample: AdressReg adress1 adress2 publicKey";
                return;
            }
            if (!GetParam(httpMessage, "2", "adress2", out string adress2))
            {
                httpMessage.result = "command error! \nexample: AdressReg adress1 adress2 publicKey";
                return;
            }
            if (!GetParam(httpMessage, "3", "publicKey", out string publicKey))
            {
                httpMessage.result = "command error! \nexample: AdressReg adress1 adress2 publicKey";
                return;
            }

            bool rel = Wallet.CheckPulblicKeyToAddress_Old_Java_V1(adress1, adress2, publicKey);
            if (rel)
            {
                var tempPath = System.IO.Directory.GetCurrentDirectory();
                var DatabasePath = System.IO.Path.Combine(tempPath, "./Data/AdressReg.csv");
                FileStream fs = new FileStream(DatabasePath, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                sw.WriteLine($"{adress1};{adress2};{publicKey}");
                sw.Close();
                fs.Close();
                httpMessage.result = "{\"success\":true}";
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
                map.Add("nonce", account == null ? "0" : account.nonce.ToString());

                if (string.IsNullOrEmpty(token))
                {
                    map.Add("amount", account == null ? "0" : account.amount);
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
                blockSub.addressIn = sender;
                blockSub.addressOut = consAddress;
                blockSub.data = data;

                try
                {
                    if (luaVMEnv.IsERC(dbSnapshot, consAddress, ""))
                    {
                        bool rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out object[] result);
                        if (rel)
                        {
                            if (result.Length == 1)
                                httpMessage.result = JsonHelper.ToJson(result[0]);
                            else
                                httpMessage.result = JsonHelper.ToJson(result);
                        }
                        else
                        {
                            httpMessage.result = "error";
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

       


    }

}