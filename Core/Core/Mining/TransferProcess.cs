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

        public bool AddTransferHandle(string addressIn, string addressOut, string amount, string unique)
        {
            var old = transfers.Find((x) => x.unique == unique);
            if (old != null)
                return false;

            // 节点使用自己的地址挖矿
            if (addressIn == addressOut)
                return false;

            transfers.Add(new TransferHandle() { lastHeight = 0, miniindex = 0, sendCount = 0, addressIn = addressIn ,addressOut = addressOut, amount = amount , unique = unique });
            return true;
        }

        public override void Start()
        {


            Run();
        }

        public async void Run()
        {
            await Task.Delay(1000);

            LoadTransferFromDB();

            List<TransferHandle> transfersDel = new List<TransferHandle>();
            var rule =Entity.Root.GetComponent<Rule>();

            while (true)
            {
                await Task.Delay(15000 * 6);

                // Query success
                using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                {
                    transfersDel.Clear();
                    for (int ii = 0; ii < transfers.Count; ii++)
                    {
                        if (transfers[ii].sendCount <= 5)
                        {
                            string hasht = dbSnapshot.Get($"unique_{transfers[ii].unique}");
                            if (!string.IsNullOrEmpty(hasht))
                            {
                                var transfer = dbSnapshot.Transfers.Get(hasht);
                                if (transfer != null)
                                {
                                    if (transfer.data == transfers[ii].unique)
                                    {
                                        transfers[ii].hash = hasht;
                                        transfersDel.Add(transfers[ii]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            transfersDel.Add(transfers[ii]);
                        }
                    }

                    using (DbSnapshot snapshot = Entity.Root.GetComponent<Pool>().PoolDBStore.GetSnapshot())
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

                    // Start a new deal
                    for (int ii = transfers.Count-1; ii >= 0; ii--)
                    {
                        if (transfers[ii].lastHeight < rule.height + 6 && transfers[ii].sendCount <= 5)
                        {
                            transfers[ii].lastHeight = rule.height;
                            transfers[ii].sendCount++;

                            var account = dbSnapshot.Accounts.Get(transfers[ii].addressIn);
                            BlockSub transfer = new BlockSub();
                            transfer.addressIn = transfers[ii].addressIn;
                            transfer.addressOut = transfers[ii].addressOut;
                            transfer.amount = transfers[ii].amount;
                            transfer.type  = "transfer";
                            transfer.nonce = ++account.nonce;
                            transfer.timestamp = TimeHelper.Now();
                            transfer.data  = transfers[ii].unique;
                            transfer.hash  = transfer.ToHash();
                            transfer.sign  = transfer.ToSign(Wallet.GetWallet().GetCurWallet());
                            dbSnapshot.Accounts.Add(transfers[ii].addressIn, account); // account.nonce Count accumulation

                            int rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
                            if (rel != 1)
                            {
                                Log.Error($"TransferProcess: aAddTransfer  Error! {transfers[ii]}");
                                transfers.RemoveAt(ii);
                            }
                        }
                    }

                }

            }
        }

        public string GetMinerTansfer(DbSnapshot dbSnapshot,string unique)
        {
            return dbSnapshot.Get($"unique_{unique}"); // Query transaction cross reference table
        }

        public void SaveTransferToDB()
        {
            using (DbSnapshot snapshot = Entity.Root.GetComponent<Pool>().PoolDBStore.GetSnapshot())
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

    }

}





















