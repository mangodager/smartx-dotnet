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
        bool openRecentNode = false;
        bool openContractTran = false;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

            if (jd["openRecentNode"] != null)
            {
                Boolean.TryParse(jd["openRecentNode"]?.ToString(), out openRecentNode);
                Log.Info($"SmartxRpc.openRecentNode = {openRecentNode}");
            }
            if (jd["openContractTran"] != null)
            {
                Boolean.TryParse(jd["openContractTran"]?.ToString(), out openContractTran);
                Log.Info($"SmartxRpc.openContractTran = {openContractTran}");
            }
        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"smartxRpc http://{networkHttp.ipEndPoint}/");

        }

        public string GetIPEndPoint()
        {
            return this.entity.GetComponent<ComponentNetworkHttp>().ipEndPoint.ToString();
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
                case "getaccounts":
                    GetAccounts(httpMessage);
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
                case "transferstatequick":
                    OnTransferStateQuick(httpMessage);
                    break;
                case "uniquetransfer":
                    UniqueTransfer(httpMessage);
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
                case "transfercount":
                    OnTransferCount(httpMessage);
                    break;
                case "transactionsquick": //根据地址查询该地址的交易记录
                    OnTransactionsQuick(httpMessage);
                    break;
                case "transfercountquick":
                    OnTransferCountQuick(httpMessage);
                    break;
                case "info":  //info
                    OnInfo(httpMessage);
                    break;
                case "latest-block-height"://latest-block-height
                    OnLatestBlockHeight(httpMessage);
                    break;
                case "miner":
                    OnMiner(httpMessage);
                    break;
                //case "mempool":
                //    GetPool(httpMessage);
                //    break;
                case "search":
                    OnSearch(httpMessage);
                    break;
                case "getlastblock":
                    GetNearBlock(httpMessage);
                    break;
                case "adressreg":
                    AdressReg(httpMessage);
                    break;
                case "getrecentblocknode":
                    getRecentNode(httpMessage);
                    break;
                case "getrecentblocknodetran":
                    getRecentNodeTran(httpMessage);
                    break;
                case "getproductinfo":
                    getProductInfo(httpMessage);
                    break;
                case "getfactorydata":
                    getDactoryData(httpMessage);
                    break;
                case "hello":
                    {
                        httpMessage.result = "welcome join SmartX.net";
                    }
                    break;
                case "getrankingdata":
                    getRankingData(httpMessage);
                    break;
                default:
                    break;
            }
        }

        private void getRankingData(HttpMessage httpMessage)
        {

            if (!GetParam(httpMessage, "1", "browserIndex", out string browserIndexStr))
            {
                httpMessage.result = "command error! \nexample: GetNearBlock browserIndex";
                return;
            }
            long.TryParse(browserIndexStr, out long browserIndex);
            var getdata = Entity.Root.GetComponent<Getdata>();
            int showCount = 20;//每页显示20条
            if (getdata.array.Any() && getdata.array != null)
            {
                JArray PageData = new JArray();
                for (int i = (showCount * (int)browserIndex) - showCount; i < showCount * (int)browserIndex; i++)
                {
                    PageData.Add(getdata.array[i]);
                }
                httpMessage.result = JsonHelper.ToJson(PageData);
            }
            else
            {
                httpMessage.result = "";
            }

        }

        public void getProductInfo(HttpMessage httpMessage)
        {
            if (!openRecentNode)
                return;

            try
            {
                //httpMessage.result = JsonHelper.ToJson(GetProduct.productInfo);
            }
            catch (Exception e)
            {
                httpMessage.result = "false";
            }
        }

        public void getRecentNodeTran(HttpMessage httpMessage)
        {
            if (!openRecentNode)
                return;

            try
            {
                string recentheight;
                string startheight;
                string endheight;
                if (!GetParam(httpMessage, "recentheight", "recentheight", out recentheight))
                {
                    recentheight = "20";
                }
                if (!GetParam(httpMessage, "startheight", "startheight", out startheight))
                {
                    startheight = "0";
                }
                if (!GetParam(httpMessage, "endheight", "endheight", out endheight))
                {
                    endheight = "0";
                }
                long searchHeight = long.Parse(recentheight);
                long searchHeightStart = long.Parse(startheight);
                long searchHeightEnd = long.Parse(endheight);
                var consensus = Entity.Root.GetComponent<Consensus>();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();
                Dictionary<string, long> res = new Dictionary<string, long>();
                var height = consensus.transferHeight;
                if (searchHeightStart != 0 && searchHeightEnd != 0 && searchHeightStart <= searchHeightEnd)
                {

                }
                else
                {
                    searchHeightStart = height - searchHeight + 1;
                    searchHeightEnd = height;
                }
                for (long i = searchHeightStart; i <= searchHeightEnd; i++)
                {
                    Block myblk = BlockChainHelper.GetMcBlock(i);
                    for (int ii = 0; ii < myblk.linksblk.Count; ii++)
                    {
                        var hash = myblk.linksblk[ii];
                        var linkBlock = blockMgr.GetBlock(hash);
                        var address = linkBlock.Address;
                        var tranCount = linkBlock.linkstran.Count;
                        long val;
                        if (res.TryGetValue(address, out val))
                        {
                            //如果指定的字典的键存在
                            res[address] = val + tranCount;
                        }
                        else
                        {
                            //不存在，则添加
                            res.Add(address, tranCount);
                        }
                    }
                }
                res.Add("startheight", searchHeightStart);
                res.Add("endheight", searchHeightEnd);
                var result = from pair in res orderby pair.Value descending select pair;
                httpMessage.result = JsonHelper.ToJson(result);
            }
            catch (Exception ex)
            {
                httpMessage.result = "";
            }
        }

        public void getRecentNode(HttpMessage httpMessage)
        {
            if (!openRecentNode)
                return;

            try
            {
                string recentheight;
                string startheight;
                string endheight;
                if (!GetParam(httpMessage, "recentheight", "recentheight", out recentheight))
                {
                    recentheight = "20";
                }
                if (!GetParam(httpMessage, "startheight", "startheight", out startheight))
                {
                    startheight = "0";
                }
                if (!GetParam(httpMessage, "endheight", "endheight", out endheight))
                {
                    endheight = "0";
                }
                long searchHeight = long.Parse(recentheight);
                long searchHeightStart = long.Parse(startheight);
                long searchHeightEnd = long.Parse(endheight);
                var consensus = Entity.Root.GetComponent<Consensus>();
                var blockMgr = Entity.Root.GetComponent<BlockMgr>();
                Dictionary<string, long> res = new Dictionary<string, long>();
                Dictionary<string, List<string>> res2 = new Dictionary<string, List<string>>();

                var height = consensus.transferHeight;
                if (searchHeightStart != 0 && searchHeightEnd != 0 && searchHeightStart < searchHeightEnd)
                {

                }
                else
                {
                    searchHeightStart = height - searchHeight + 1;
                    searchHeightEnd = height;
                }

                Dictionary<string, int> addressTransCount = new Dictionary<string, int>();

                // 得到节点ip
                var nodes = Entity.Root.GetComponent<NodeManager>().GetNodeList();
                nodes.Sort((a, b) => b.kIndex - a.kIndex);

                for (long i = searchHeightStart; i <= searchHeightEnd; i++)
                {
                    Block myblk = BlockChainHelper.GetMcBlock(i);
                    for (int ii = 0; ii < myblk.linksblk.Count; ii++)
                    {
                        Block linkblk = blockMgr.GetBlock(myblk.linksblk[ii]);
                        if (linkblk == null)
                        {
                            return;
                        }
                        if (linkblk.height != 1)
                        {
                            for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                            {
                                //addressTransCount.Add(linkblk.Address);
                                linkblk.linkstran[jj].height = 0;
                            }
                        }
                    }

                    for (int ii = 0; ii < myblk.linksblk.Count; ii++)
                    {
                        var hash = myblk.linksblk[ii];
                        var linkBlock = blockMgr.GetBlock(hash);
                        var address = linkBlock.Address;
                        long val;
                        if (res.TryGetValue(address, out val))
                        {
                            //如果指定的字典的键存在
                            res[address] = val + 1;
                            res2[address][1] = (val + 1).ToString();
                        }
                        else
                        {
                            for (int j = 0; j < nodes.Count; j++)
                            {
                                if (address == nodes[j].address)
                                {
                                    List<string> tmplist = new List<string>();
                                    tmplist.Add(nodes[j].ipEndPoint);
                                    tmplist.Add("1");
                                    tmplist.Add(searchHeightStart.ToString());
                                    tmplist.Add(searchHeightEnd.ToString());
                                    res2.TryAdd(address, tmplist);

                                    //不存在，则添加
                                    res.Add(address, 1);
                                    break;
                                }
                            }

                            
                        }
                    }
                }

                res.Add("startheight", searchHeightStart);
                res.Add("endheight", searchHeightEnd);

                //res2.Add("startheight", searchHeightStart);
                //res2.Add("endheight", searchHeightEnd);

                var result = from pair in res orderby pair.Value descending select pair;
                var result2 = from pair in res2 orderby pair.Value[1] descending select pair;
                //httpMessage.result = JsonHelper.ToJson(result);
                httpMessage.result = JsonHelper.ToJson(result2);
            }
            catch (Exception ex)
            {
                httpMessage.result = "";
            }
        }

        private void getDactoryData(HttpMessage httpMessage)
        {
            var cons = Entity.Root.GetComponent<Consensus>();
            Dictionary<string, string> res = new Dictionary<string, string>();
            res.Add("SatswapFactory", cons.SatswapFactory);
            res.Add("ERCSat", cons.ERCSat);
            res.Add("PledgeFactory", cons.PledgeFactory);
            res.Add("LockFactory", cons.LockFactory);
            httpMessage.result = JsonHelper.ToJson(res);
        }

        private void OnContractTran(HttpMessage httpMessage)
        {
            if (!openContractTran) {
                httpMessage.result = "!SmartxRpc.openContractTran";
                return;
            }

            string indexStr;
            if (!GetParam(httpMessage, "Index", "index", out indexStr))
            {
                indexStr = "0";
            }
            int.TryParse(indexStr,out int index);
            if (index > 10){
                index = 10;// limit 
            }

            int num = (12 * (index + 1));
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
                    for (int ii = TFA_Count; ii > 0 && ii >= (TFA_Count-num); ii--)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{address}{tokenAddress}", ii - 1);
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                transfers.Add(transfer);
                                transfer.temp.Remove(arr[0]);
                                transfer.temp.Add(arr[0]);
                            }
                        }
                    }
                }
            }

            List<BlockSub> SortList = transfers.OrderByDescending(it => it.timestamp).ToList().Skip(index*12).Take(12).ToList();
            if(SortList.Count>0)
            SortList[0].temp.Remove(transfers.Count.ToString());
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
                            symbol = LuaContract.GetSymbol(blockSub.addressOut);

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
                            var symbol = LuaContract.GetSymbol(transfer.addressOut);
                            transfer.temp = new List<string>();
                            transfer.temp.Add(symbol);
                        }
                    }
                }

                if (transfer == null) {
                    httpMessage.result = "false";
                }
                else {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                }
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

        private List<BlockSub> OnTransactions(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "index", out string index))
            {
                index = "0";
            }
            int Index = int.Parse(index);
            List<BlockSub> transactionList = new List<BlockSub>();
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                if (!GetParam(httpMessage, "2", "address", out string address))
                {
                    httpMessage.result = "please input address";
                }
                if (!GetParam(httpMessage, "3", "token", out string tokenAddress))
                {
                }

                //var account = dbSnapshot.Accounts.Get(address);
                //if (account != null)
                {
                    var key = string.IsNullOrEmpty(tokenAddress) ? $"TFA__{address}" : $"TFA__{address}{tokenAddress}";
                    var TFA_Count = dbSnapshot.List.GetCount(key);
                    Index = TFA_Count - Index * 12;

                    for (int ii = Index; ii > Index - 12 && ii > 0; ii--)
                    {
                        string hasht = dbSnapshot.List.Get(key, ii-1);
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
            return transactionList;

        }

        // FloatChat Quick confirmation
        private void GetTransactionsQuick(int Index,string address, string tokenAddress, ref List<BlockSub> transactionList,ref int TFA_Count)
        {
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var rule = Entity.Root.GetComponent<Rule>();
            var lastblk = rule.GetLastMcBlock();
            if (lastblk != null)
            {
                var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
                // 取最新高度
                long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);

                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    var key = string.IsNullOrEmpty(tokenAddress) ? $"TFA__{address}": $"TFA__{address}{tokenAddress}";
                    TFA_Count = dbSnapshot.List.GetCount(key);
                }

                List<Block> blks = new List<Block>();
                var curblk = lastblk;
                for (long height = curblk.height; height > transferHeight; height--)
                {
                    blks.Insert(0,curblk);
                    curblk = blockMgr.GetBlock(curblk.prehash);
                }

                for (var blks_index = 0; blks_index < blks.Count; blks_index++)
                {
                    var mcblk = blks[blks_index];
                    for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
                    {
                        Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                        if (linkblk == null)
                        {
                            continue;
                        }
                        if (linkblk.height != 1)
                        {
                            for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                            {
                                if (linkblk.linkstran[jj].addressIn==address||linkblk.linkstran[jj].data.IndexOf(address)!=-1)
                                {
                                    if (transactionList.IndexOf(linkblk.linkstran[jj]) == -1)
                                    {
                                        transactionList.Insert(0,linkblk.linkstran[jj]);
                                        TFA_Count++;
                                    }
                                }
                            }
                        }
                    }
                }

                List<Block> linksblks = blockMgr.GetBlock(lastblk.height);
                for (int ii = 0; ii < linksblks.Count; ii++)
                {
                    Block linkblk = linksblks[ii];
                    for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                    {
                        if (linkblk.linkstran[jj].addressIn==address||linkblk.linkstran[jj].data.IndexOf(address)!=-1)
                        {
                            if (transactionList.IndexOf(linkblk.linkstran[jj]) == -1)
                            {
                                transactionList.Insert(0,linkblk.linkstran[jj]);
                                TFA_Count++;
                            }
                        }
                    }
                }

            }

            transactionList = transactionList.TakeLast(12).ToList();

        }

        // FloatChat Quick confirmation
        private void OnTransactionsQuick(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "index", out string index))
            {
                index = "0";
            }
            if (!GetParam(httpMessage, "2", "address", out string address))
            {
                httpMessage.result = "please input address";
                return;
            }
            if (!GetParam(httpMessage, "3", "token", out string tokenAddress))
            {
            }

            List<BlockSub> transactionList = OnTransactions(httpMessage);
            int Index = int.Parse(index);
            if (Index != 0)
                return;

            int TFA_Count = 0;
            GetTransactionsQuick(Index, address, tokenAddress, ref transactionList, ref TFA_Count);

            httpMessage.result = JsonHelper.ToJson(transactionList);

        }

        // FloatChat Quick confirmation
        private void OnTransferCountQuick(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "address", out string address))
            {
                httpMessage.result = "please input address";
                return;
            }
            if (!GetParam(httpMessage, "2", "token", out string tokenAddress))
            {
            }
            int TFA_Count = 0;
            List<BlockSub> transactionList = new List<BlockSub>();
            GetTransactionsQuick(0, address, tokenAddress, ref transactionList, ref TFA_Count);

            httpMessage.result = "{\"count\":" + TFA_Count + "}";

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
                var miners = httpPool?.GetMinerRewardMin(out long miningHeight);
                result.Add("H", UndoHeight);
                result.Add("P", power2);
                result.Add("P1", power1);
                result.Add("Miner", $"{miners?.Count}/{httpPool?.minerLimit}");
                result.Add("Fee", $"{Pool.GetServiceFee() * 100}%");
                result.Add("RewardInterval", $"{pool.RewardInterval / 4}");
                result.Add("ignorePower", $"{HttpPool.ignorePower/1000}K");
                result.Add("Ver", Miner.version.Replace("Miner_", ""));
                httpMessage.result = JsonHelper.ToJson(result);
                //httpMessage.result = $"H:{UndoHeight} P:{power2} Miner:{miners?.Count}";
            }
        }

        public void OnPoolStats(HttpMessage httpMessage)
        {
            var HttpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (HttpPoolRelay == null)
            {
                OnStats(httpMessage);
                return;
            }

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
            result.Add("RewardInterval", $"{pool.RewardInterval / 4}");
            result.Add("ignorePower", $"{HttpPool.ignorePower / 1000}K");
            result.Add("Ver", Miner.version.Replace("Miner_", ""));
            httpMessage.result = JsonHelper.ToJson(result);
        }

        public void GetPoolList(HttpMessage httpMessage)
        {
            httpMessage.result = JsonHelper.ToJson(HttpPoolRelay.poolInfos);
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
            if (httpMessage.map.ContainsKey("depend"))
                transfer.depend  = System.Web.HttpUtility.UrlDecode(httpMessage.map["depend"]);
            if (httpMessage.map.ContainsKey("remarks"))
                transfer.remarks = System.Web.HttpUtility.UrlDecode(httpMessage.map["remarks"]);
            if (httpMessage.map.ContainsKey("extend"))
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
                if (transfer != null)
                {
                    httpMessage.result = JsonHelper.ToJson(transfer);
                    var temp = JsonHelper.FromJson<BlockSub>(httpMessage.result);
                    if (temp.temp == null)
                    {
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
                            for (int jj = 0; jj < linkblk.linkstran.Count && transfer == null; jj++)
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

        public void OnTransferStateQuick(HttpMessage httpMessage)
        {
            httpMessage.result = "";
            if (!GetParam(httpMessage, "1", "hash", out string hash))
            {
                httpMessage.result = "command error! \nexample: transferstate hash";
                return;
            }

            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var rule = Entity.Root.GetComponent<Rule>();
            var lastblk = rule.GetLastMcBlock();
            if (lastblk != null)
            {
                var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
                // 取最新高度
                long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);

                List<Block> blks = new List<Block>();
                var curblk = lastblk;
                for (long height = curblk.height; height > transferHeight; height--)
                {
                    blks.Insert(0, curblk);
                    curblk = blockMgr.GetBlock(curblk.prehash);
                }

                for (var blks_index = 0; blks_index < blks.Count; blks_index++)
                {
                    var mcblk = blks[blks_index];
                    for (int ii = 0; ii < mcblk.linksblk.Count; ii++)
                    {
                        Block linkblk = blockMgr.GetBlock(mcblk.linksblk[ii]);
                        if (linkblk == null)
                        {
                            continue;
                        }
                        if (linkblk.height != 1)
                        {
                            for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                            {
                                if (linkblk.linkstran[jj].hash == hash)
                                {
                                    httpMessage.result = JsonHelper.ToJson(linkblk.linkstran[jj]);
                                    return;
                                }
                            }
                        }
                    }
                }

                List<Block> linksblks = blockMgr.GetBlock(lastblk.height);
                for (int ii = 0; ii < linksblks.Count; ii++)
                {
                    Block linkblk = linksblks[ii];
                    for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                    {
                        if (linkblk.linkstran[jj].hash == hash)
                        {
                            httpMessage.result = JsonHelper.ToJson(linkblk.linkstran[jj]);
                            return;
                        }
                    }
                }
            }

        }

        public void UniqueTransfer(HttpMessage httpMessage)
        {
            if (!GetParam(httpMessage, "1", "unique", out string unique))
            {
                httpMessage.result = "command error! \nexample: transferstate hash";
                return;
            }

            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                string hasht = dbSnapshot.Get($"unique_{unique}");
                if (!string.IsNullOrEmpty(hasht))
                {
                    var transfer = dbSnapshot.Transfers.Get(hasht);
                    if (transfer != null)
                    {
                        httpMessage.result = JsonHelper.ToJson(transfer);
                    }
                    else
                        httpMessage.result = "";
                }
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