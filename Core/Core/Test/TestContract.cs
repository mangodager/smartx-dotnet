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

    public class TestContract
    {
        public static async void TestPledge(HttpMessage httpMessage)
        {
            if (Wallet.GetWallet().keys.Count < 1000)
            {
                for (int ii = Wallet.GetWallet().keys.Count; ii < 1000; ii++)
                {
                    Wallet.GetWallet().Create("123");
                }
                Wallet.GetWallet().SaveWallet();

                Log.Info("TestPledge create");

                Session session2 = null;
                for (int ii = 1; ii < 1000; ii++)
                {
                    int random1 = 0;
                    int random2 = ii;
                    int random3 = 100000;

                    BlockSub transfer = new BlockSub();
                    transfer.type = "transfer";
                    transfer.addressIn  = Wallet.GetWallet().keys[random1].ToAddress();
                    transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                    transfer.amount = random3.ToString();
                    transfer.data = $"";
                    transfer.depend = "";
                    transfer.nonce = HttpRpc.GetAccountNotice(transfer.addressIn, false);
                    transfer.timestamp = TimeHelper.Now();
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                    session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    while (session2 == null)
                    {
                        session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    };
                }
            }
            else
            {
                var     consensus    = Entity.Root.GetComponent<Consensus>();
                var     accountCount = Wallet.GetWallet().keys.Count;
                Session session2 = null;
                while (true)
                {
                    Log.Info("TestPledge start");
                    session2 = null;
                    for (int ii = 0; ii < 200; ii++)
                    {
                        int random1 = RandomHelper.Range(1, accountCount);
                        int random2 = RandomHelper.Range(1, accountCount);
                        while (random1 == random2)
                            random2 = RandomHelper.Range(1, accountCount);

                        BlockSub transfer   = new BlockSub();
                        transfer.type       = "contract";
                        transfer.addressIn  = Wallet.GetWallet().keys[random1].ToAddress();
                        transfer.amount     = "";
                        transfer.depend     = "";
                        transfer.nonce      = HttpRpc.GetAccountNotice(transfer.addressIn);
                        transfer.timestamp  = TimeHelper.Now();

                        var rules = consensus.GetRule(consensus.transferHeight).Select( x=>x.Value.Contract ).ToList();
                        rules.Remove("");
                        rules.Remove(null);

                        int callFunIndex = RandomHelper.Range(0, 3);
                        if (callFunIndex == 0)
                        {
                            int random3 = RandomHelper.Range(1, 1000);
                            transfer.data = $"addLiquidity(\"{random3}\",\"{random3}\")";

                            transfer.addressOut = rules[RandomHelper.Range(0, rules.Count) % rules.Count];
                        }
                        else
                        if (callFunIndex == 1)
                        {
                            int random3 = RandomHelper.Range(1, 1000);
                            transfer.data = $"removeLiquidity(\"{random3}\")";
                            transfer.addressOut = rules[RandomHelper.Range(0, rules.Count) % rules.Count];
                        }
                        else
                        if (callFunIndex == 2)
                        {
                            int random3 = (int)consensus.transferHeight - RandomHelper.Range(1, 10);
                            transfer.data = $"retrieved(\"{random3}\",\"{random3}\")";
                            transfer.addressOut = rules[RandomHelper.Range(0, rules.Count) % rules.Count];
                        }
                        else
                        if (callFunIndex == 3)
                        {
                            int random3 = (int)consensus.transferHeight - RandomHelper.Range(1, 10);
                            transfer.data = $"diversionLiquidity(\"{random3}\",\"{rules[RandomHelper.Range(0, rules.Count) % rules.Count]}\")";
                            transfer.addressOut = rules[RandomHelper.Range(0, rules.Count) % rules.Count];
                        }
                        else
                        if (callFunIndex == 4)
                        {
                            int random3 = (int)consensus.transferHeight - RandomHelper.Range(1, 10);
                            transfer.data = $"cancel(\"{rules[RandomHelper.Range(0, rules.Count) % rules.Count]}\",\"{random3}\")";
                            transfer.addressOut = consensus.PledgeFactory;
                        }

                        transfer.hash = transfer.ToHash();
                        transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                        session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                        while (session2 == null)
                        {
                            session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                        };
                    }
                    await Task.Delay(1000);
                }
            }
        }

        public static async void TestTransfer(HttpMessage httpMessage)
        {
            if (Wallet.GetWallet().keys.Count < 1000)
            {
                for (int ii = Wallet.GetWallet().keys.Count; ii < 1000; ii++)
                {
                    Wallet.GetWallet().Create("123");
                }
                Wallet.GetWallet().SaveWallet();

                Log.Info("TestTransfer create");

                Session session2 = null;
                for (int ii = 1; ii < 1000; ii++)
                {
                    int random1 = 0;
                    int random2 = ii;
                    int random3 = 100000;

                    BlockSub transfer = new BlockSub();
                    transfer.type = "transfer";
                    transfer.addressIn = Wallet.GetWallet().keys[random1].ToAddress();
                    transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                    transfer.amount = random3.ToString();
                    transfer.data = $"";
                    transfer.depend = "";
                    transfer.nonce = HttpRpc.GetAccountNotice(transfer.addressIn, false);
                    transfer.timestamp = TimeHelper.Now();
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                    session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    while (session2 == null)
                    {
                        session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    };
                }
            }
            else
            {
                if (!HttpRpc.GetParam(httpMessage, "3", "Count", out string strCount))
                {
                    httpMessage.result = "command error! \nexample: test 5 Address C:\\Address.csv";
                    return;
                }
                TestTransfersyncCount = int.Parse(strCount);

                var consensus = Entity.Root.GetComponent<Consensus>();
                while (true)
                {
                    Log.Info($"Test2Async {TestTransfersyncCount}");
                    consensus.AddRunAction(TestTransfersync);
                    await Task.Delay(1000);
                }
            }
        }

        static int TestTransfersyncCount = 20;
        public static async void TestTransfersync()
        {
            Session session2 = null;
            var accountCount = Wallet.GetWallet().keys.Count;
            //while (true)
            {
                session2 = null;
                for (int ii = 0; ii < TestTransfersyncCount; ii++)
                {
                    int random1 = RandomHelper.Range(1, accountCount);
                    int random2 = RandomHelper.Range(1, accountCount);
                    while (random1 == random2)
                        random2 = RandomHelper.Range(1, accountCount);
                    int random3 = RandomHelper.Range(1, 100);

                    BlockSub transfer = new BlockSub();
                    transfer.type = "transfer";
                    transfer.addressIn = Wallet.GetWallet().keys[random1].ToAddress();
                    transfer.addressOut = Wallet.GetWallet().keys[random2].ToAddress();
                    transfer.amount = random3.ToString();
                    transfer.data = "";
                    transfer.depend = "";
                    transfer.nonce = HttpRpc.GetAccountNotice(transfer.addressIn);
                    transfer.timestamp = TimeHelper.Now();
                    transfer.hash = transfer.ToHash();
                    transfer.sign = transfer.ToSign(Wallet.GetWallet().keys[random1]);

                    session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    while (session2 == null)
                    {
                        session2 = await HttpRpc.OnTransferAsync2(transfer, session2);
                    };
                }
                //await Task.Delay(1000);
            }

        }

        public static void TransferAll(string file,long min, long max)
        {
            var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);

            Log.Debug("Do TransferAll Start");
            System.IO.File.Delete($"./{file}.csv");

            using (DbSnapshot dbSnapshot = levelDBStore.GetSnapshot(transferHeight))
            {
                for (long ii = min; ii <= max; ii++)
                {
                    var heights = dbSnapshot.Heights.Get(ii.ToString());
                    for (int jj = 0; jj < heights.Count; jj++)
                    {
                        System.IO.File.AppendAllText($"./{file}.csv", $"{heights[jj]} \n");
                    }
                }

            }
            Log.Debug("Do TransferAll End");
        }


        public static void Test1408123(string file, long min, long max)
        {
            // file open
            string fullPath = file;
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            FileStream fs = new FileStream(fullPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);

            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            string data = $"height,transfer_json";
            sw.WriteLine(data);


            var blockMgr = Entity.Root.GetComponent<BlockMgr>();
            var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();

            for (long height = min; height <= max; height++)
            {
                var chain = BlockChainHelper.GetBlockChain(height);
                List<Block> linksblks = blockMgr.GetBlock(chain.height);
                for (int ii = 0; ii < linksblks.Count; ii++)
                {
                    Block linkblk = linksblks[ii];
                    for (int jj = 0; jj < linkblk.linkstran.Count; jj++)
                    {
                        var transfer = levelDBStore.Get($"Trans___{linkblk.linkstran[jj].hash}");

                        sw.WriteLine($"{height},{transfer}");
                    }
                }
            }

        }

    }

}





















