using System;
using System.Collections.Generic;
using System.IO;
using LevelDB;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class LevelDBStore : Component
    {
        private DB db;
        public DbCache<Block> Blocks;
        public DbCache<List<string>> Heights;

        public override void Awake(JToken jd = null)
        {
            string db_path = jd["db_path"]?.ToString();
            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
            Init(DatabasePath);
        }

        public override void Start()
        {

        }

        public override void Dispose()
        {
            base.Dispose();
            db?.Dispose();
            db = null;
        }

        public LevelDBStore Init(string path)
        {
            var options = new Options()
            {
                BlockSize = 8 * 1024 * 1024, // 8M 
                Cache = new Cache(100 * 1024 * 1024), // 内存缓存 100M
                CompressionLevel = CompressionLevel.SnappyCompression,
                CreateIfMissing = true,
            };

            this.db = new DB(options, path);
            Blocks  = new DbCache<Block>(db, null, null, null, "Blocks");
            Heights = new DbCache<List<string>>(db, null, null, null, "Heights");

            return this;
        }

        public string Get(string key)
        {
            return db.Get(key);
        }

        public void Put(string key, string value)
        {
            db.Put(key, value);
        }

        public void PutSync(string key, string value)
        {
            db.Put(key, value, new WriteOptions { Sync = true });
        }
        
        // ------------------------------------------
        public void UndoTransfers(long height)
        {
            height = Math.Max(height,1);
            long.TryParse(Get("UndoHeight"),out long height_total);
            if (height == height_total)
                return;

            Log.Debug($"UndoTransfers {height_total} to {height}");

            WriteBatch batch = new WriteBatch();
            for (long ii = height_total; ii > height; ii--)
            {
                string Undos_Value = db.Get($"Undos___{ii}");
                if (Undos_Value != null)
                {
                    DbUndo undos = JsonHelper.FromJson<DbCache<DbUndo>.Slice>(Undos_Value).obj;
                    for (int jj = 0; jj < undos.keys.Count; jj++)
                    {
                        string key = $"{undos.keys[jj]}_undo_{undos.height}";
                        string value = db.Get(key);
                        if (value != null && value != "")
                            batch.Put(undos.keys[jj], value);
                        else
                            batch.Delete(undos.keys[jj]);
                        batch.Delete(key);
                    }
                    batch.Delete($"Undos___{ii}");
                }
            }

            batch.Put("UndoHeight", System.Math.Min(height, height_total).ToString() );

            db.Write(batch, new WriteOptions { Sync = true });
            batch?.Dispose();

            Entity.Root.GetComponent<Consensus>()?.cacheRule.Clear();
        }

        public DbSnapshot GetSnapshot(long height=0)
        {
            return new DbSnapshot(db, height, false);
        }

        public DbSnapshot GetSnapshotUndo(long height = 0)
        {
            return new DbSnapshot(db, height, true);
        }

        public static void Export2CSV_Block(string[] args)
        {
            var randName = "LevelDB";
            if(args[2]!=null)
                randName = args[2];
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            // file open
            string fullPath = "C:\\blocks.csv";
            FileInfo fi = new FileInfo(args[0]);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            FileStream fs = new FileStream(fullPath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            string data = $"height;hash;diff;prehash;prehashmkl;Address;timestamp;random;sign;linksblk;linkstran";
            sw.WriteLine(data);

            long.TryParse(dbstore.Get("UndoHeight"), out long curHeight);
            for (long ii = 1; ii < curHeight + 10000; ii++)
            {
                List<string> list = dbstore.Heights.Get(ii.ToString());
                if (list != null)
                {
                    foreach (string hash in list)
                    {
                        Block blk = dbstore.Blocks.Get(hash);
                        if (blk != null)
                        {
                            string str_linksblk = "";
                            string str_linkstran = "";

                            foreach (string value in blk.linksblk.Values)
                            {
                                str_linksblk = $"{str_linksblk}#{value}";
                            }

                            foreach (var value in blk.linkstran.Values)
                            {
                                str_linkstran = $"{str_linkstran}#{value.ToString()}";
                            }

                            string temp = $"{blk.height};{blk.hash};#{blk.GetDiff()};{blk.prehash};{blk.prehashmkl};{blk.Address};{blk.timestamp};{blk.random};{blk.sign};{str_linksblk};{str_linkstran}";
                            sw.WriteLine(temp);
                        }
                    }
                }
                else
                {
                    //break;
                }
            }
            sw.Close();
            fs.Close();
            dbstore.Dispose();
        }

        public static void Export2CSV_Height(string[] args)
        {

        }

        public static void Export2CSV_Transfer(string filename, string address)
        {
            using (var dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                var account = dbSnapshot.Accounts.Get(address);
                // file open
                string fullPath = filename;
                FileInfo fi = new FileInfo(fullPath);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                FileStream fs = new FileStream(fullPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
                fs.Seek(0, SeekOrigin.Begin);
                fs.SetLength(0);

                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                string data = $"index,height,transfer,type,data";
                sw.WriteLine(data);

                if (account != null)
                {
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{address}");
                    for (int ii = 0; ii < TFA_Count; ii++)
                    {
                        string hasht = dbSnapshot.List.Get($"TFA__{address}", ii);
                        if (hasht != null)
                        {
                            var transfer = dbSnapshot.Transfers.Get(hasht);
                            if (transfer != null)
                            {
                                string symbols = transfer.addressIn == address ? "-" : "";
                                sw.WriteLine($"{ii},{transfer.height},{hasht},{transfer.type},{transfer.data},{symbols+transfer.amount}");
                            }
                        }
                    }
                }
                sw.Close();
                fs.Close();
            }
        }

        public static void Export2CSV_Account(string filename, string address)
        {
            var levelDBStore = Entity.Root.GetComponent<LevelDBStore>();
            // file open
            string fullPath = filename;
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            FileStream fs = new FileStream(fullPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);

            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            string data = $"height,account_json";
            sw.WriteLine(data);

            long.TryParse(levelDBStore.Get("UndoHeight"), out long transferHeight);

            for (long ii = 1; ii < transferHeight; ii++)
            {
                var account = levelDBStore.Get($"Accounts___{address}_undo_{ii}");
                if (account != null)
                {
                    sw.WriteLine($"{ii},{account}");
                }
            }

            var accountCur = levelDBStore.Get($"Accounts___{address}");
            if (accountCur != null)
            {
                sw.WriteLine($"{transferHeight+1},{accountCur}");
            }

            sw.Close();
            fs.Close();
        }

        public static void Export2CSV_Accounts(string filename)
        {
            // file open
            string fullPath = filename;
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            FileStream fs = new FileStream(fullPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);

            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            string data = $"sequnce,address,amount,index,notice";
            sw.WriteLine(data);

            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
            {
                for (int ii = 0; ii < Wallet.GetWallet().keys.Count; ii++)
                {
                    string addressIn = Wallet.GetWallet().keys[ii].ToAddress();
                    Account account = dbSnapshot.Accounts.Get(addressIn);
                    if (account == null)
                    {
                        account = new Account() { address = addressIn, amount = "0", nonce = 0 };
                    }
                    int TFA_Count = dbSnapshot.List.GetCount($"TFA__{addressIn}");

                    sw.WriteLine($"{ii},{account.address},{account.amount},{TFA_Count},{account.nonce}");

                }

                sw.Close();
                fs.Close();
            }
        }

        public static bool Test(string[] args)
        {
            // 
            //DBTests tests = new DBTests();
            //tests.SetUp();
            //tests.Snapshot();
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "LevelDB";
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1))
            {
                snapshot.Blocks.Add("11", new Block() { Address = "11" });
                snapshot.Blocks.Add("22", new Block() { Address = "22" });
                var value1 = dbstore.Blocks.Get("11");
                var value2 = dbstore.Blocks.Get("22");
                snapshot.Commit();
                var result1 = dbstore.Blocks.Get("11");
                var result2 = dbstore.Blocks.Get("22");
            }

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1))
            {
                snapshot.Blocks.Add("11", new Block() { Address = "11" });
                snapshot.Blocks.Add("22", new Block() { Address = "22" });
                var value1 = dbstore.Blocks.Get("11");
                var value2 = dbstore.Blocks.Get("22");
                snapshot.Commit();
                var result1 = dbstore.Blocks.Get("11");
                var result2 = dbstore.Blocks.Get("22");
            }

            return true;
        }

        public static void test_undo(string[] args)
        {
            System.Console.WriteLine($"test_undo ...");

            // 
            //DBTests tests = new DBTests();
            //tests.SetUp();
            //tests.Snapshot();
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "LevelDB";
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            for (int rr = 1; rr <= 30; rr++)
            {
                long.TryParse(dbstore.Get("UndoHeight"), out long UndoHeight);
                int random1 = 1000 + RandomHelper.Random() % 1000;

                if (UndoHeight < random1)
                {
                    for (long i = UndoHeight + 1; i <= random1; i++)
                    {
                        using (DbSnapshot snapshot = dbstore.GetSnapshot(i))
                        {
                            snapshot.Transfers.Add("undos_test", new BlockSub() { hash = $"Address_{i}" });
                            snapshot.Commit();
                        }
                    }
                }

                {
                    using (DbSnapshot snapshot = dbstore.GetSnapshot(0))
                    {
                        var result1 = snapshot.Transfers.Get("undos_test");

                        long.TryParse(dbstore.Get("UndoHeight"), out long UndoHeight2);
                        if (result1.hash != $"Address_{UndoHeight2.ToString()}")
                            System.Console.WriteLine($"dbstore.Undo {random1} error1: {result1.hash}");
                        //System.Console.WriteLine($"dbstore.Undo {random1} error1: {result1.txid}");
                    }
                }

                if (UndoHeight > random1)
                {
                    dbstore.UndoTransfers(random1);
                }

                using (DbSnapshot snapshot = dbstore.GetSnapshot(0))
                {
                    var result2 = snapshot.Transfers.Get("undos_test");

                    long.TryParse(dbstore.Get("UndoHeight"), out long UndoHeight2);
                    if (result2.hash != $"Address_{UndoHeight2.ToString()}")
                        System.Console.WriteLine($"dbstore.Undo {random1} error2: {result2.hash}");
                    //System.Console.WriteLine($"dbstore.Undo {random1} error2: {result2.txid}");
                }

            }

        }

        public static void test_delete(string[] args)
        {
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "LevelDB";
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1))
            {
                snapshot.Blocks.Add("11", new Block() { Address = "11" });
                snapshot.Blocks.Add("22", new Block() { Address = "22" });
                var value1 = dbstore.Blocks.Get("11");
                var value2 = dbstore.Blocks.Get("22");
                System.Console.WriteLine($"dbstore.test_delete value1: {value1}");
                snapshot.Commit();
                var result1 = dbstore.Blocks.Get("11");
                var result2 = dbstore.Blocks.Get("22");
                System.Console.WriteLine($"dbstore.test_delete value1: {result1}");
            }

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1))
            {
                snapshot.Blocks.Delete("11");
                snapshot.Blocks.Delete("22");
                snapshot.Commit();
                var result1 = dbstore.Blocks.Get("11");
                var result2 = dbstore.Blocks.Get("22");
                System.Console.WriteLine($"dbstore.test_delete value1: {result1}");
            }

        }

        public static void test_put(string[] args)
        {
            // 
            //DBTests tests = new DBTests();
            //tests.SetUp();
            //tests.Snapshot();
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "LevelDB";
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            //数据写入测试
            System.Diagnostics.Stopwatch sp = new System.Diagnostics.Stopwatch();
            sp.Reset();
            sp.Start();
            int mCount = 0;
            while (true)
            {
                dbstore.Put(mCount.ToString(), "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaeraaaaaaaaaaaaaaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
                if (System.Threading.Interlocked.Increment(ref mCount) % 10000 == 0)
                {
                    System.Console.WriteLine($"{mCount} has inserted. time use {sp.ElapsedMilliseconds}ms.");
                }

            }
        }

        static public void test_ergodic(string[] args)
        {
            // 
            //DBTests tests = new DBTests();
            //tests.SetUp();
            //tests.Snapshot();
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "Data\\LevelDB4";
            var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);

            // Create new iterator
            using (var it = dbstore.db.CreateIterator())
            {
                // Iterate in reverse to print the values as strings
                for (it.SeekToFirst(); it.IsValid(); it.Next())
                {
                    Log.Info($"Value as string: {it.KeyAsString()}");
                }
            }
        }


    }
}
