using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                case "balanceof":
                    balanceOf(httpMessage);
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
                case "rules":
                    OnRules(httpMessage);
                    break;
                //case "transfer":
                //    OnTransfer(httpMessage);
                //    break;
                case "transferstate":
                    OnTransferState(httpMessage);
                    break;
                case "beruler":
                    OnBeRuler(httpMessage);
                    break;
                //case "getmnemonic":
                //    GetMnemonicWord(httpMessage);
                //    break;
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
                case "contract":
                    OnContract(httpMessage);
                    break;
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
                                        $"     NodeSessions: {Program.jdNode["NodeSessions"]}";
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
                var miners = httpPool?.GetMinerReward(out long miningHeight);
                httpMessage.result = $"H:{UndoHeight} P:{power2} Miner:{miners?.Count} P1:{power1}";
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
                            blockSub.data = "symbol()";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight,out result);
                            if (rel && result != null && result.Length >= 1)
                            {
                                symbol = ((string)result[0]) ?? "unknown";
                            }

                            blockSub.data = $"balanceOf(\"{address}\")";
                            rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight, out result);
                            if (rel && result != null && result.Length >= 1)
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
            transfer.depend  = System.Web.HttpUtility.UrlDecode(httpMessage.map["depend"]);
            transfer.remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);

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
            var consensus = Entity.Root.GetComponent<Consensus>();
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();

            // 判断当前块高度是否接近主线
            if(blockMgr.newBlockHeight-consensus.transferHeight > 1000)
            {
                httpMessage.result = $"{consensus.transferHeight}:{blockMgr.newBlockHeight} The current block height is too low. command BeRuler have been ignore.";
                return;
            }

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
            luaVMCall.args = new FieldParam[1];

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

        public void OnContract(HttpMessage httpMessage)
        {
            httpMessage.result = "command error! \nexample: contract consAddress callFun";
            GetParam(httpMessage, "1", "consAddress", out string consAddress);
            GetParam(httpMessage, "1", "depend", out string depend);
            if (!GetParam(httpMessage, "2", "callFun", out string callFun))
            {
                return;
            }

            WalletKey key = Wallet.GetWallet().GetCurWallet();
            var sender = key.ToAddress();

            if (callFun.IndexOf("create(")!=-1)
            {
                var consensus = Entity.Root.GetComponent<Consensus>();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();

                long notice = 1;
                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    var account = dbSnapshot.Accounts.Get(sender);
                    if (account != null)
                    {
                        notice = account.nonce + 1;
                    }
                }

                BlockSub transfer   = new BlockSub();
                transfer.addressIn  = sender;
                transfer.addressOut = null;
                transfer.amount = "0";
                transfer.nonce  = notice;
                transfer.type   = "contract";
                transfer.depend = depend;
                transfer.data   = callFun;
                var luaVMCall = LuaVMCall.Decode(transfer.data);
                Log.Info(JsonHelper.ToJson(luaVMCall));

                //transfer.timestamp = TimeHelper.Now();
                //transfer.hash = transfer.ToHash();
                //transfer.sign = transfer.ToSign(key);

                //var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
                //if (rel == -1)
                //{
                //    OnTransferAsync(transfer);
                //}
                //httpMessage.result = $"accepted transfer:{transfer.hash}";
            }
            else
            {
                using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
                {
                    var consensus = Entity.Root.GetComponent<Consensus>();
                    var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

                    var blockSub = new BlockSub();
                    blockSub.addressIn = sender;
                    blockSub.addressOut = consAddress;
                    blockSub.data = callFun;
                    bool rel = luaVMEnv.Execute(dbSnapshot, blockSub, consensus.transferHeight,out object[] result);
                    httpMessage.result = JsonHelper.ToJson(result);
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
            else
            if (style == "rule")
            {
                TestBeRule(httpMessage);
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

        Dictionary<string, long> AccountNotice = new Dictionary<string, long>();
        public long GetAccountNotice(string address,bool reset=true)
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

        public async ETTask<Session> OnTransferAsync2(BlockSub transfer ,Session session2)
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


        public async void Test2Async(object o)
        {
            NodeManager nodeManager = Entity.Root.GetComponent<NodeManager>();

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

                BlockSub transfer = new BlockSub();
                transfer.type = "transfer";
                transfer.addressIn = Wallet.GetWallet().keys[random1].ToAddress();
                transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                transfer.amount = random3.ToString();
                transfer.data = "";
                transfer.depend = "";
                transfer.nonce = GetAccountNotice(transfer.addressIn, false);
                transfer.timestamp = TimeHelper.Now();
                transfer.hash = transfer.ToHash();
                transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

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

        public async void TestBeRule(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            await Task.Delay(10);

        }

    }


}