using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        
        //v1.0.0/global-info
        
        public string GetBalance(String address)
        {
            return "";
        }

        public Block ShowBlock(String hash)
        {
            return null;
        }

        public List<Block> GetBlocks(long height, String address)
        {
            return null;
        }

        public List<Block> GetBlocks(long height)
        {
            return null;
        }

        public long GetLatestHeight()
        {
            return 0;
        }

        public bool Transfer(String rawjson)
        {
            return true;
        }

        public Block GetMCBlock(long height)
        {
            return null;
        }

        public string GetCommand(HttpMessage message)
        {
            string cmd = null;
            message.map.TryGetValue("v1.0.0/global-info", out cmd);
            if (cmd == null)
            {
                
            }
            
            return "";
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
                case "account":
                    OnAccount(httpMessage);
                    break;
                case "balance":
                    OnBalance(httpMessage);
                    break;
                case "stats":
                    OnStats(httpMessage);
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
                case "block-by-tranhash": //根据交易查询 交易的信息
                    OnBlocktranHash(httpMessage);
                    break;
                case "transactions": //根据地址查询该地址的交易记录
                    OnTransactions(httpMessage);
                    break;
                case "transfer":
                    OnTransfer(httpMessage);
                    break;
                case "command":
                    Command(httpMessage);
                    break;                
                case "info":  //info
                    OnInfo(httpMessage);
                    break;
                case "latest-block-height"://latest-block-height
                    OnLatestBlockHeight(httpMessage);
                    break;
                case "node":
                    OnNode(httpMessage);
                    break;
                case "rules":
                    OnRules(httpMessage);
                    break;
                case "creat":
                    OnCreat(httpMessage);
                    break;
                case "importwallet":
                    Onimport(httpMessage);
                    break;
                case "transfercount":
                    OnTransferCount(httpMessage);
                    break;
                default:
                    break;
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
                var account = dbSnapshot.Accounts.Get(address);
                if (account != null)
                {
                    httpMessage.result = "{\"count\":" +account.index+ "}";
                    return;
                }
                httpMessage.result = "false";
            }
        }

        private void Onimport(HttpMessage httpMessage)
        {
            Console.Write("please input your password:");
            //string passwd = Console.ReadLine();
            
            string walletFile = "./wallet.json";
            Wallet wallet =  Wallet.GetWallet(walletFile);
            if (wallet == null) 
            {
                httpMessage.result = "please check your password";            
            }
            string address = Wallet.ToAddress(wallet.keys[0].publickey);
            httpMessage.result = $"publickey   {wallet.keys[0].publickey.ToHexString()}\nprivatekey {wallet.keys[0].privatekey.ToHexString()}\nAddress    {address}";
        }

        private void OnCreat(HttpMessage httpMessage)
        {
            Console.Write("please input your password:");
            string passwd = Console.ReadLine();
            Wallet wallet = new Wallet();
            wallet = wallet.NewWallet(passwd);
            wallet.walletFile = "./wallet.json";
            wallet.SaveWallet();
            if (wallet.keys.Count > 0)
            {
                httpMessage.result = wallet.keys[0].ToAddress();
                return;
            }
            httpMessage.result = "create account error";
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
                    httpMessage.result = "false";
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
            if (!GetParam(httpMessage, "hash", "hash", out string hash))
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
                    if (transfer.timestamp == 0)
                    {
                        transfer.timestamp = myblk.timestamp;
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
            long Index = int.Parse(index);
            List<BlockSub> transactionList = new List<BlockSub>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(httpMessage.map["address"]);

                if (account != null)
                {
                    Index = account.index - Index * 12;

                    for (long ii = Index; ii > Index - 12 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.TFA.Get($"{httpMessage.map["address"]}_{ii}");
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                if (transfer.addressIn == "" && transfer.addressOut == "" || transfer.addressIn == null && transfer.addressOut == null)
                                {
                                    transfer.addressIn = "00000000000000000000000000000000";
                                    transfer.addressOut = httpMessage.map["address"];
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
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var myblk = blockMgr.GetBlock(httpMessage.map["hash"]);

            if (myblk == null || myblk.timestamp ==0 ) 
            {
                httpMessage.result = "false";
            }

            else httpMessage.result = JsonHelper.ToJson(myblk);
        }

        private List<string> GetBlockTranHash(string address, string height)
        {
            List<string> res = new List<string>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    long getIndex = 0;
                    getIndex = account.index - getIndex;
                    for (long ii = getIndex; ii > getIndex - 100 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.TFA.Get($"{address}_{ii}");
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer.height == long.Parse(height))
                            {
                                res.Add(hasht);
                            }
                        }
                    }
                }
            }
            return res;
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
                case "getmnemonic":
                    GetMnemonicWord(httpMessage);
                    break;
                case "node":
                    OnNode(httpMessage);
                    break;
                case "miner":
                    OnMiner(httpMessage);
                    break;
                case "getblock":
                    GetBlock(httpMessage);
                    break;
                case "test":
                    Test(httpMessage);
                    break;
                case "delblock":
                    DelBlock(httpMessage);
                    break;
                case "pool":
                    GetPool(httpMessage);
                    break;
                case "search":
                    OnSearch(httpMessage);
                    break;
                case "hello":
                    {
                        httpMessage.result = "welcome join SmartX.net";
                    }
                    break;
                case "help":
                    {
                        httpMessage.result = "you can use Command menu";
                    }
                    break;
                default:
                    break;
            }

            //httpMessage.result = "ok";
        }

        public void OnSearch(HttpMessage httpMessage)
        {
            OnBlockHash(httpMessage);
            if (httpMessage.result == "false")
            {
                OnBlocktranHash(httpMessage);
                if (httpMessage.result == "false")
                {
                    httpMessage.result = "";
                }
            }
            
        }
        
        public void GetPool(HttpMessage httpMessage)
        {
            Dictionary<string,BlockSub> transfers =  Entity.Root.GetComponent<Rule>().GetTransfers();
            httpMessage.result =  JsonHelper.ToJson(transfers);
        }

        public void OnStats(HttpMessage httpMessage)
        {
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
            var amount = account != null ? BigInt.Div(account.amount , "10000").ToString() : "0";
            long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out long UndoHeight);
            long PoolHeight = rule.height;
            string power1 = rule.calculatePower.GetPower();
            string power2 = Entity.Root.GetComponent<Consensus>().calculatePower.GetPower();

            int NodeCount = Entity.Root.GetComponent<NodeManager>().GetNodeCount();

            if (style == "")
            {
                httpMessage.result =    $"      AlppyHeight: {UndoHeight}\n" +
                                        $"       PoolHeight: {PoolHeight}\n" +
                                        $"  Calculate Power: {power1} of {power2}\n" +
                                        $"          Account: {address}, {amount}\n" +
                                        $"             Node: {NodeCount}\n" +
                                        $"    Rule.Transfer: {rule?.GetTransfersCount()}\n" +
                                        $"    Pool.Transfer: {pool?.GetTransfersCount()}";
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
                var miners = httpPool.GetMinerReward(out long miningHeight);
                httpMessage.result = $"H:{UndoHeight} P:{power2} Miner:{miners?.Count}";
            }
        }

        public void OnRules(HttpMessage httpMessage)
        {
            Dictionary<string, RuleInfo> ruleInfos = Entity.Root.GetComponent<Consensus>().ruleInfos;
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
                        account = new Account() { address = list[i], amount = "0", index = 0, nonce = 0 };
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
                    httpMessage.result = $"          Account: {account.address}, amount:{BigInt.Div(account.amount, "10000")} , index:{account.index}";
                else
                    httpMessage.result = $"          Account: {address}, amount:0 , index:0";
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

            //string hash = transfer.ToHash();
            //string sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet()).ToHexString();

            var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
            if (rel==-1)
            {
                OnTransferAsync(transfer);
            }

            if (rel == -1 || rel == 1)
            {
                httpMessage.result = "{\"success\":true}";
            }
            else
            {
                httpMessage.result = "{\"success\":false,\"rel\":{"+ rel + "}}";
            }
        }

        public async void OnTransferAsync(BlockSub transfer)
        {
            Consensus consensus = Entity.Root.GetComponent<Consensus>();

            Q2P_Transfer q2p_Transfer = new Q2P_Transfer();
            q2p_Transfer.transfer = JsonHelper.ToJson(transfer);

            var networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
            var nodeList = Entity.Root.GetComponent<NodeManager>().GetNodeList();

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

        public bool GetParam(HttpMessage httpMessage,string key1,string key2,out string value)
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

            long.TryParse(indexStr, out long getIndex);

            var transfers = new List<BlockSub>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);

                if (account != null)
                {
                    getIndex = account.index - getIndex;
                    for (long ii = getIndex; ii > getIndex - 20 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.TFA.Get($"{address}_{ii}");
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                transfers.Add(transfer);
                            }
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
            luaVMCall.fnName = "Add";
            luaVMCall.args = new FieldParam[0];
            //luaVMCall.args[0] = new FieldParam();
            //luaVMCall.args[0].type = "Int64";
            //luaVMCall.args[0].value = "999";
            //long aaa = (long)luaVMCall.args[0].GetValue();
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


        public void GetMnemonicWord(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "passwords", out string passwords))
            {
                httpMessage.result = "command error! \nexample: getmnemonic password";
                return;
            }

            var walletKey = Wallet.GetWallet().GetCurWallet();
            string randomSeed = CryptoHelper.Sha256(walletKey.random.ToHexString() + "#" + passwords);
            httpMessage.result = randomSeed;
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
                    if (pre.height != consensus.transferHeight)
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
            if (!GetParam(httpMessage, "1", "style", out string style))
            {
                httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                return;
            }

            httpMessage.result = "";
            if (style == "1")
            {
                if (!GetParam(httpMessage, "2", "Address", out string Address))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "3", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 1 Address C:\\Address.csv";
                    return;
                }

                LevelDBStore.Export2CSV_Transfer($"{file}", Address);

            }
            else
            if (style == "4")
            {
                LevelDBStore.Export2CSV_Accounts($"C:\\Accounts_test4.csv");
            }
            else
            if (style == "5")
            {
                if (!GetParam(httpMessage, "2", "Address", out string Address))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                if (!GetParam(httpMessage, "3", "file", out string file))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                LevelDBStore.Export2CSV_Account($"{file}", Address);
            }

        }

        public long DelBlock_min;
        public long DelBlock_max;
        public void DelBlock(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "from", out string from))
            {
                httpMessage.result = "command error! \nexample: DelBlock 100 1";
                return;
            }
            if (!GetParam(httpMessage, "2", "to", out string to))
            {
                httpMessage.result = "command error! \nexample: DelBlock 1 100 ";
                return;
            }

            DelBlock_max = Math.Max(long.Parse(from), long.Parse(to));
            DelBlock_min = Math.Min(long.Parse(from), long.Parse(to));

            Entity.Root.GetComponent<Consensus>().AddRunAction(DelBlockAsync);
        }

        public void DelBlockAsync()
        {
            long max = DelBlock_max;
            long min = DelBlock_min;

            Log.Info($"DelBlock {min} {max}");

            Entity.Root.GetComponent<Consensus>().transferHeight = min;
            Entity.Root.GetComponent<LevelDBStore>().UndoTransfers(min);
            for (long ii = max; ii > min; ii--)
            {
                Entity.Root.GetComponent<BlockMgr>().DelBlock(ii);
            }

            Log.Info("DelBlock finish");
        }
    }


}