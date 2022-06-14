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

namespace ETModel
{

    public class TransferProcess : Component
    {
        public class TransferHandle
        {
            public long   lastHeight;
            public long   miniindex;
            public long   sendCount;
            public string addressIn;
            public string addressOut;
            public string amount;
            public string unique;   // Corresponding to data, unique identification
            public string hash;

        }
        public List<TransferHandle> transfers = new List<TransferHandle>();
        static HttpPoolRelay httpPoolRelay = null;

        public void AddTransferHandle(string addressIn, string addressOut, string amount, string unique,long height=0)
        {
            if(string.IsNullOrEmpty(unique))
                return ;
            if (BigHelper.Less(amount, "0", true))
                return;

            AddRunAction( ()=>{
                var old = transfers.Find((x) => x.unique == unique);
                if (old != null)
                    return;

                // 节点使用自己的地址挖矿
                if (addressIn == addressOut)
                    return;

                transfers.Add(new TransferHandle() { lastHeight = height, miniindex = 0, sendCount = 0, addressIn = addressIn, addressOut = addressOut, amount = amount, unique = unique });
                return;
            });
        }

        private Action runAction;
        public void AddRunAction(Action a)
        {
            lock (this)
            {
                runAction += a;
            }
        }

        public override void Start()
        {
            httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();

            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ThreadRun));
            thread.IsBackground = true;//设置为后台线程
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.Start(this);
        }

        public void ThreadRun(object data)
        {
            System.Threading.Thread.Sleep(10000);

            LoadTransferFromDB();

            List<TransferHandle> transfersDel = new List<TransferHandle>();
            Dictionary<string, Account> accounts = new Dictionary<string, Account>();
            var timePassInfo = new TimePass(15 * 6);

            while (true)
            {
                System.Threading.Thread.Sleep(1000);

                // Query success
                try
                {
                    lock (this)
                    {
                        if (runAction != null)
                        {
                            runAction?.Invoke();
                            runAction = null;
                            SaveTransferToDB();
                        }
                    }

                    if (!timePassInfo.IsPassSet())
                        continue;

                    transfersDel.Clear();
                    for (int ii = 0; ii < transfers.Count; ii++)
                    {
                        if (transfers[ii].sendCount <= 5)
                        {
                            if (string.IsNullOrEmpty(transfers[ii].unique)) {
                                transfersDel.Add(transfers[ii]);
                                continue;
                            }

                            var transfer = GetUniqueTransfer(transfers[ii].unique);
                            if (transfer != null)
                            {
                                if (transfer.data == transfers[ii].unique&&transfer.height!=0)
                                {
                                    transfers[ii].hash = transfer.hash;
                                    transfersDel.Add(transfers[ii]);
                                }
                            }
                        }
                        else
                        {
                            File.AppendAllText("./TransferBad.csv", JsonHelper.ToJson(transfers[ii]) + "\n", Encoding.UTF8);
                            transfersDel.Add(transfers[ii]);
                        }
                    }

                    using (DbSnapshot snapshot = Entity.Root.GetComponent<Pool>().PoolDBStore.GetSnapshot(0, true))
                    {
                        bool remove = transfersDel.Count != 0;
                        // Successfully deleted from table
                        foreach (var it in transfersDel)
                        {
                            if (!string.IsNullOrEmpty(it.unique) && !string.IsNullOrEmpty(it.hash))
                            {
                                snapshot.Add($"unique_{it.unique}", it.hash); // Add to transaction cross reference table
                            }
                            transfers.Remove(it);
                        }
                        if (remove) {
                            snapshot.Commit();
                        }
                    }

                    accounts.Clear();

                    long curHeight = GetHeight();
                    if (curHeight == 0) {
                        Log.Error($"poolUrl: {httpPoolRelay.poolUrl} can't connect");
                        continue;
                    }

                    // Start a new deal
                    bool bSaveDb = false;
                    for (int ii = transfers.Count - 1; ii >= 0; ii--)
                    {
                        if (transfers[ii].lastHeight < curHeight - 18 && transfers[ii].sendCount <= 5)
                        {
                            transfers[ii].lastHeight = curHeight;
                            transfers[ii].sendCount++;

                            if (BigHelper.Less(transfers[ii].amount, "0", true))
                            {
                                transfers.RemoveAt(ii);
                                continue;
                            }

                            Account account = null;
                            if (!accounts.TryGetValue(transfers[ii].addressIn, out account))
                            {
                                account = GetAccount(transfers[ii].addressIn);
                                if (account == null)
                                    continue;
                                accounts.Add(transfers[ii].addressIn, account);
                            }

                            BlockSub transfer = new BlockSub();
                            transfer.addressIn = transfers[ii].addressIn;
                            transfer.addressOut = transfers[ii].addressOut;
                            transfer.amount = transfers[ii].amount;
                            transfer.type = "transfer";
                            transfer.nonce = ++account.nonce;
                            transfer.timestamp = TimeHelper.Now();
                            transfer.data = transfers[ii].unique;
                            transfer.extend = new List<string>();
                            //transfer.extend.Add($"deadline:{curHeight + 16}");
                            transfer.extend.Add($"unique");

                            transfer.hash = transfer.ToHash();
                            transfer.sign = transfer.ToSign(Wallet.GetWallet().GetCurWallet());

                            //int rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer, false);
                            int rel = SendTransfer(transfer);
                            if (rel == -1) {
                                transfers[ii].sendCount--;
                                continue;
                            }
                            if (rel != 1)
                            {
                                File.AppendAllText("./TransferBad.csv", JsonHelper.ToJson(transfers[ii]) + "\n", Encoding.UTF8);
                                Log.Error($"TransferProcess: aAddTransfer  Error! {rel}");
                                transfers.RemoveAt(ii);
                            }
                            bSaveDb = true;
                        }
                    }
                    if (bSaveDb) {
                        SaveTransferToDB();
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"TransferProcess throw Exception: {e.ToString()}");
                    Log.Error(e.ToString());
                }
            }
        }

        public static string GetMinerTansfer(DbSnapshot dbSnapshot,string unique)
        {
            var hash = dbSnapshot.Get($"unique_{unique}");
            if (string.IsNullOrEmpty(hash))
            {
                if (httpPoolRelay == null)
                {
                    using (DbSnapshot dbSnapshot2 = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                    {
                        hash = dbSnapshot2.Get($"unique_{unique}");
                    }
                    if (!string.IsNullOrEmpty(hash))
                    {
                        dbSnapshot.Add($"unique_{unique}", hash);
                    }
                }
            }
            return hash;  // Query transaction cross reference table
        }

        public void SaveTransferToDB()
        {
            using (DbSnapshot snapshot = Entity.Root.GetComponent<Pool>().PoolDBStore.GetSnapshot(0,true))
            {
                snapshot.Add($"TransferProcess", JsonHelper.ToJson(transfers));
                snapshot.Commit();
            }
        }

        void LoadTransferFromDB()
        {
            using (DbSnapshot snapshot = Entity.Root.GetComponent<Pool>().PoolDBStore.GetSnapshot())
            {
                string str = snapshot.Get($"TransferProcess");
                if(str!=null)
                    transfers = JsonHelper.FromJson<List<TransferHandle>>(str);
            }
        }

        static Account GetAccount(string address)
        {
            try
            {
                if (httpPoolRelay == null)
                {
                    using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                    {
                        return dbSnapshot.Accounts.Get(address);
                    }
                }

                var list = new List<string>();
                list.Add(address);

                var quest = new PoolMessage();
                quest.map = new Dictionary<string, string>();
                quest.map.Add("cmd", "getaccounts");
                quest.map.Add("List", Base58.Encode(JsonHelper.ToJson(list).ToByteArray()));

                var accounts = httpPoolRelay.QueryPoolSync<Dictionary<string, Account>>(quest);
                accounts.TryGetValue(address, out Account acc);
                return acc;
            }
            catch (Exception )
            {
            }
            return null;
        }

        static BlockSub GetUniqueTransfer(string unique, float timeout = 5)
        {
            try
            {
                if (httpPoolRelay == null)
                {
                    using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                    {
                        string hasht = dbSnapshot.Get($"unique_{unique}");
                        if (!string.IsNullOrEmpty(hasht))
                        {
                            return dbSnapshot.Transfers.Get(hasht);
                        }
                    }
                    return null;
                }

                var quest = new PoolMessage();
                quest.map = new Dictionary<string, string>();
                quest.map.Add("cmd", "uniquetransfer");
                quest.map.Add("unique", unique);
                var result = httpPoolRelay.QueryPoolSync<BlockSub>(quest, timeout);
                return result;
            }
            catch (Exception )
            {
            }
            return null;
        }

        static public long GetHeight(float timeout = 5)
        {
            try
            {
                if(httpPoolRelay==null) {
                    return Entity.Root.GetComponent<Consensus>().transferHeight;
                }

                var quest = new PoolMessage();
                quest.map = new Dictionary<string, string>();
                quest.map.Add("cmd", "stats");
                quest.map.Add("style", "1");
                var result = httpPoolRelay.QueryPoolSync<Dictionary<string, string>>(quest, timeout);
                return result==null?0:long.Parse(result["H"]);
            }
            catch (Exception )
            {
            }
            return 0;
        }

        static int SendTransfer(BlockSub blockSub,float timeout= 30)
        {
            try
            {
                if (httpPoolRelay == null)
                {
                    return Entity.Root.GetComponent<Rule>().AddTransfer(blockSub, false);
                }

                var quest = new PoolMessage();
                quest.map = new Dictionary<string, string>();
                quest.map.Clear();
                quest.map.Add("cmd", "transfer");
                quest.map.Add("type", blockSub.type);
                quest.map.Add("hash", blockSub.hash);
                quest.map.Add("nonce", blockSub.nonce.ToString());
                quest.map.Add("addressIn", blockSub.addressIn);
                quest.map.Add("addressOut", blockSub.addressOut);
                quest.map.Add("amount", blockSub.amount);
                quest.map.Add("data", System.Web.HttpUtility.UrlEncode(blockSub.data));
                quest.map.Add("sign", blockSub.sign.ToHexString());
                quest.map.Add("fee", blockSub.gas.ToString());
                quest.map.Add("timestamp", blockSub.timestamp.ToString());
                quest.map.Add("remarks", System.Web.HttpUtility.UrlEncode(blockSub.remarks));
                quest.map.Add("depend", System.Web.HttpUtility.UrlEncode(blockSub.depend));
                quest.map.Add("extend", System.Web.HttpUtility.UrlEncode(JsonHelper.ToJson(blockSub.extend)));

                var result = httpPoolRelay.QueryPoolSync<Dictionary<string, string>>(quest, timeout);
                if (result["success"] == "false")
                {
                    return int.Parse(result["rel"]);
                }
            }
            catch (Exception )
            {
            }
            return -1;
        }


    }

}





















