using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LevelDB;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class LevelDBStore : Component
    {
        public static bool db_MultiThread = true;

        private DB db;
        public DbCache<Block> Blocks;
        public DbCache<List<string>> Heights;
        public string db_path;
        public bool   db_Compression = true;

        public DB GetDB()
        {
            return db;
        }
        public override void Awake(JToken jd = null)
        {
            db_path = jd["db_path"]?.ToString();
            bool.TryParse(jd["db_Compression"]?.ToString(), out db_Compression);
            bool.TryParse(jd["db_MultiThread"]?.ToString(), out db_MultiThread);

            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
            Init(DatabasePath);
        }

        static public void Repair(string path)
        {
            Log.Debug($"Repair Start: {path}");

            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), path);

            var options = new Options()
            {
                BlockSize = 8 * 1024 * 1024, // 8M 
                Cache = new Cache(100 * 1024 * 1024), // 内存缓存 100M
                CompressionLevel = CompressionLevel.NoCompression,
                CreateIfMissing = true,
            };
            DB.Repair(options, DatabasePath);

            Log.Debug($"Repair End: {path}");
        }

        public override void Start()
        {

        }

        public void Reset()
        {
            var temp = db;
            lock (temp)
            {
                var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
                Init(DatabasePath);
            }

            System.Threading.Thread.Sleep(3000);

            lock (temp)
            {
                temp?.Dispose();
            }
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
                CompressionLevel = db_Compression ? CompressionLevel.SnappyCompression : CompressionLevel.NoCompression,
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
            if (height >= height_total)
                return;

            Log.Debug($"UndoTransfers {height_total} to {height}");

            lock (db)
            {
                WriteBatch batch = new WriteBatch();
                long ii = height_total;
                for (; ii > height; ii--)
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
                    else
                    {
                        break;
                    }
                }

                batch.Put("UndoHeight", System.Math.Min(ii, height_total).ToString());

                db.Write(batch, new WriteOptions { Sync = true });
                batch?.Dispose();
            }

            Entity.Root.GetComponent<Consensus>()?.cacheRule.Clear();
        }

        public DbSnapshot GetSnapshot(long height=0,bool bWrite=false)
        {
            return new DbSnapshot(db, height, false, bWrite);
        }

        public DbSnapshot GetSnapshotUndo(long height = 0)
        {
            return new DbSnapshot(db, height, true,true);
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

        public static void Export2CSV_Account(string filename, string address, string token=null)
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
            string account = "";
            for (long ii = 1; ii < transferHeight; ii++)
            {
                if (!string.IsNullOrEmpty(token)){
                    account = levelDBStore.Get($"StgMap___{token}__balances__{address}_undo_{ii}");
                }
                else {
                    account = levelDBStore.Get($"Accounts___{address}_undo_{ii}");
                }

                if (account != null)
                {
                    sw.WriteLine($"{ii},{account}");
                }
            }

            if (!string.IsNullOrEmpty(token)){
                account = levelDBStore.Get($"StgMap___{token}__balances__{address}");
            }
            else {
                account = levelDBStore.Get($"Accounts___{address}");
            }
            if (account != null)
            {
                sw.WriteLine($"{transferHeight+1},{account}");
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

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1, true))
            {
                snapshot.Blocks.Add("11", new Block() { Address = "11" });
                snapshot.Blocks.Add("22", new Block() { Address = "22" });
                var value1 = dbstore.Blocks.Get("11");
                var value2 = dbstore.Blocks.Get("22");
                snapshot.Commit();
                var result1 = dbstore.Blocks.Get("11");
                var result2 = dbstore.Blocks.Get("22");
            }

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1, true))
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
                        using (DbSnapshot snapshot = dbstore.GetSnapshot(i, true))
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

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1, true))
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

            using (DbSnapshot snapshot = dbstore.GetSnapshot(1, true))
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

        /// <summary>
        /// JSON格式化重新排序
        /// </summary>
        /// <param name="jobj">原始JSON JToken.Parse(string json);</param>
        /// <param name="obj">初始值Null</param>
        /// <returns></returns>
        public static string SortJson(JToken jobj, JToken obj)
        {
            if (obj == null)
            {
                obj = new JObject();
            }
            List<JToken> list = jobj.ToList<JToken>();
            if (jobj.Type == JTokenType.Object)//非数组
            {
                List<string> listsort = new List<string>();
                foreach (var item in list)
                {
                    string name = JProperty.Load(item.CreateReader()).Name;
                    listsort.Add(name);
                }
                listsort.Sort();
                List<JToken> listTemp = new List<JToken>();
                foreach (var item in listsort)
                {
                    listTemp.Add(list.Where(p => JProperty.Load(p.CreateReader()).Name == item).FirstOrDefault());
                }
                list = listTemp;
                //list.Sort((p1, p2) => JProperty.Load(p1.CreateReader()).Name.GetAnsi() - JProperty.Load(p2.CreateReader()).Name.GetAnsi());

                foreach (var item in list)
                {
                    JProperty jp = JProperty.Load(item.CreateReader());
                    if (item.First.Type == JTokenType.Object)
                    {
                        JObject sub = new JObject();
                        (obj as JObject).Add(jp.Name, sub);
                        SortJson(item.First, sub);
                    }
                    else if (item.First.Type == JTokenType.Array)
                    {
                        JArray arr = new JArray();
                        if (obj.Type == JTokenType.Object)
                        {
                            (obj as JObject).Add(jp.Name, arr);
                        }
                        else if (obj.Type == JTokenType.Array)
                        {
                            (obj as JArray).Add(arr);
                        }
                        SortJson(item.First, arr);
                    }
                    else if (item.First.Type != JTokenType.Object && item.First.Type != JTokenType.Array)
                    {
                        (obj as JObject).Add(jp.Name, item.First);
                    }
                }
            }
            else if (jobj.Type == JTokenType.Array)//数组
            {
                foreach (var item in list)
                {
                    List<JToken> listToken = item.ToList<JToken>();
                    List<string> listsort = new List<string>();
                    foreach (var im in listToken)
                    {
                        string name = JProperty.Load(im.CreateReader()).Name;
                        listsort.Add(name);
                    }
                    listsort.Sort();
                    List<JToken> listTemp = new List<JToken>();
                    foreach (var im2 in listsort)
                    {
                        listTemp.Add(listToken.Where(p => JProperty.Load(p.CreateReader()).Name == im2).FirstOrDefault());
                    }
                    list = listTemp;

                    listToken = list;
                    // listToken.Sort((p1, p2) => JProperty.Load(p1.CreateReader()).Name.GetAnsi() - JProperty.Load(p2.CreateReader()).Name.GetAnsi());
                    JObject item_obj = new JObject();
                    foreach (var token in listToken)
                    {
                        JProperty jp = JProperty.Load(token.CreateReader());
                        if (token.First.Type == JTokenType.Object)
                        {
                            JObject sub = new JObject();
                            (obj as JObject).Add(jp.Name, sub);
                            SortJson(token.First, sub);
                        }
                        else if (token.First.Type == JTokenType.Array)
                        {
                            JArray arr = new JArray();
                            if (obj.Type == JTokenType.Object)
                            {
                                (obj as JObject).Add(jp.Name, arr);
                            }
                            else if (obj.Type == JTokenType.Array)
                            {
                                (obj as JArray).Add(arr);
                            }
                            SortJson(token.First, arr);
                        }
                        else if (item.First.Type != JTokenType.Object && item.First.Type != JTokenType.Array)
                        {
                            if (obj.Type == JTokenType.Object)
                            {
                                (obj as JObject).Add(jp.Name, token.First);
                            }
                            else if (obj.Type == JTokenType.Array)
                            {
                                item_obj.Add(jp.Name, token.First);
                            }
                        }
                    }
                    if (obj.Type == JTokenType.Array)
                    {
                        (obj as JArray).Add(item_obj);
                    }

                }
            }
            string ret = obj.ToString(Formatting.None);
            return ret;
        }

        static public void test_ergodic2(long height, string filename)
        {
            // 
            //DBTests tests = new DBTests();
            //tests.SetUp();
            //tests.Snapshot();
            //var tempPath = System.IO.Directory.GetCurrentDirectory();
            //var randName = "Data\\LevelDB1";
            //var DatabasePath = System.IO.Path.Combine(tempPath, randName);
            //LevelDBStore dbstore = new LevelDBStore().Init(DatabasePath);
            LevelDBStore dbstore = Entity.Root.GetComponent<LevelDBStore>();
            // Create new iterator
            lock (dbstore.db)
            {
                string sum = "0";
                dbstore.UndoTransfers(height);
                using (var it = dbstore.db.CreateIterator())
                {
                    File.Delete("./" + filename + ".csv");
                    File.AppendAllText("./" + filename + ".csv", "height："+ height+"版本："+ NodeManager.networkIDCur + "\n");
                    // Iterate in reverse to print the values as strings
                    for (it.SeekToFirst(); it.IsValid(); it.Next())
                    {
                        //Log.Info($"Value as string: {it.KeyAsString()}");
                        if ((!it.KeyAsString().Contains("undo")) && (!it.KeyAsString().Contains("Undo")))
                        {
                            if (it.KeyAsString().IndexOf("Accounts___") == 0)
                            {
                                try
                                {
                                    Console.WriteLine($"Value as string: {it.ValueAsString()}");
                                    Dictionary<string, Dictionary<string, object>> kv = JsonHelper.FromJson<Dictionary<string, Dictionary<string, object>>>(it.ValueAsString());
                                    //all += long.Parse(kv["obj"]["amount"].ToString());
                                    File.AppendAllText("./" + filename + ".csv", kv["obj"]["address"].ToString() + "," + kv["obj"]["amount"].ToString() + "\n");
                                    BigHelper.Add(kv["obj"]["amount"].ToString(), sum);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(it.KeyAsString());
                                    Console.WriteLine($"出错了: {it.ValueAsString()}");
                                    Console.WriteLine(e.Message);
                                    break;
                                }
                            }
                            else
                            if (it.KeyAsString().Contains("Storages"))
                            {
                                var kv = JsonHelper.FromJson<Dictionary<string, Dictionary<string, byte[]>>>(it.ValueAsString());
                                var json = SortJson(JToken.Parse(kv["obj"]["jsonData"].ToStr()),null);

                                File.AppendAllText("./" + filename + ".csv", it.KeyAsString().Replace("Storages___", "") + "," + json + "\n");
                            }
                            else
                            if (it.KeyAsString().Contains("StgMap"))
                            {
                                Console.WriteLine($"Value as string: {it.ValueAsString()}");
                                File.AppendAllText("./" + filename + ".csv", $"{it.KeyAsString()},{it.ValueAsString()}\n");
                            }
                        }
                    }
                    File.AppendAllText("./" + filename + ".csv", "All" + "," + sum + "\n");

                    long posProduct = 0;
                    long powProduct = 0;
                    long posOriginally = 0;
                    long powOriginally = 0;
                    for (long i = 1; i <= height; i++)
                    {
                        Block block = BlockChainHelper.GetMcBlock(i);
                        long posNodeCount = block.linksblk.Count;
                        posProduct += posNodeCount * Consensus.GetRewardRule(i);
                        powProduct += Consensus.GetReward(i);
                        posOriginally += 25*Consensus.GetRewardRule(i);
                        powOriginally += Consensus.GetReward(i);

                    }
                    
                    long All_Product = posProduct + powProduct;
                    File.AppendAllText("./" + filename + ".csv", "All_Product" + "," + All_Product + "\n");
                    File.AppendAllText("./" + filename + ".csv", "posProduct" + "," + posProduct + "\n");
                    File.AppendAllText("./" + filename + ".csv", "posOriginally" + "," + posOriginally + "\n");
                    File.AppendAllText("./" + filename + ".csv", "powOriginally" + "," + powOriginally + "\n");

                    Console.WriteLine("导出完成");
                }
            }
        }

        static public void MakeSnapshot(Dictionary<string, string> param)
        {
            bool trans = false;
            if (param.ContainsKey("trans")) {
                bool.TryParse(param["trans"], out trans);
            }

            Console.WriteLine($"levelDB.Init {param["db"]}");
            LevelDBStore levelDB = new LevelDBStore();
            levelDB.Init(param["db"]);

            if ( param.ContainsKey("height") && long.TryParse(param["height"], out long height) )
            {
                levelDB.UndoTransfers(height);
            }
            long.TryParse(levelDB.Get("UndoHeight"), out long transferHeight);
            Console.WriteLine($"transferHeight: {transferHeight}");

            var DatabasePath = $"./Data/LevelDB_Snapshot_{transferHeight}";
            if (Directory.Exists(DatabasePath))
            {
                Console.WriteLine($"Directory LevelDB_Snapshot Exists");
                return;
            }
            LevelDBStore snapshotDB = new LevelDBStore();
            snapshotDB.Init(DatabasePath);

            int count = 0;
            using (var it = levelDB.db.CreateIterator())
            {
                for (it.SeekToFirst(); it.IsValid(); it.Next(),count++)
                {
                    //Log.Info($"Value as string: {it.KeyAsString()}");
                    if (it.KeyAsString().IndexOf("_undo_") == -1
                      &&it.KeyAsString().IndexOf("Blocks") != 0
                      &&it.KeyAsString().IndexOf("BlockChain") != 0
                      &&it.KeyAsString().IndexOf("Queue") != 0
                      &&it.KeyAsString().IndexOf("Heights") != 0
                      &&it.KeyAsString().IndexOf("Undos___") != 0)
                    {
                        if (it.KeyAsString().IndexOf("List___") == 0)
                        {
                            if (trans && it.KeyAsString().IndexOf("___TFA__") != -1)
                            {
                                snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                                Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                            }
                        }
                        else
                        if (it.KeyAsString().IndexOf("Trans___") == 0)
                        {
                            var slice = JsonHelper.FromJson< DbCache<BlockSub>.Slice >(it.ValueAsString());
                            // fix debug Snapshot(Over deduction of handling charges)
                            if (slice != null/* && slice.obj.height != 0*/)
                            {
                                snapshotDB.Put(it.KeyAsString(), $"{{\"obj\":{{\"height\":{slice.obj.height}}}}}");
                                Console.WriteLine($"Processed tran: {it.KeyAsString()}");
                            }
                            else
                            {
                                snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                                Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                            }
                        }
                        else
                        if (it.KeyAsString().IndexOf("Snap___") == 0)
                        {
                            if (it.KeyAsString().IndexOf("Snap___Rule_") == 0)
                            {
                                var key = it.KeyAsString();
                                var pos1 = "Snap___Rule_".Length;
                                var pos2 = key.Length;
                                var hegihtTemp1 = key.Substring(pos1, pos2 - pos1);
                                var hegihtTemp2 = long.Parse(hegihtTemp1);
                                if (hegihtTemp2 > transferHeight - 5 && hegihtTemp2 < transferHeight + 5)
                                {
                                    snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                                    Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                                }
                            }
                            else
                            if (it.KeyAsString().IndexOf("_Reward") != -1)
                            {
                                var key = it.KeyAsString();
                                var pos1 = "Snap___".Length;
                                var pos2 = key.IndexOf("_Reward");

                                var hegihtTemp1 = key.Substring(pos1, pos2 - pos1);
                                var hegihtTemp2 = long.Parse(hegihtTemp1);
                                if (hegihtTemp2 > transferHeight - 5 && hegihtTemp2 < transferHeight + 5)
                                {
                                    snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                                    Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                                }
                            }
                            else
                            {
                                snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                                Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                            }
                        }
                        else
                        {
                            snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                            Console.WriteLine($"Processed  key: {it.KeyAsString()}");
                        }
                    }

                    if(count%1000000==0)
                        Console.WriteLine($"Processed Count:{count}");
                }
            }

            using (DbSnapshot dbNew = snapshotDB.GetSnapshot(0,true))
            using (DbSnapshot dbOld = levelDB.GetSnapshot())
            {
                for (long ii = transferHeight - 3; ii <= transferHeight + 2; ii++)
                {
                    Console.WriteLine($"Processed height: {ii}");
                    var heights = dbOld.Heights.Get(ii.ToString());
                    for (int jj = 0; jj < heights.Count; jj++)
                    {
                        dbNew.Blocks.Add(heights[jj], dbOld.Blocks.Get(heights[jj]));
                    }

                    dbNew.Heights.Add(ii.ToString(), heights);
                    dbNew.BlockChains.Add(ii.ToString(), dbOld.BlockChains.Get(ii.ToString()));

                }

                dbNew.Commit();
            }

            Console.WriteLine($"MakeSnapshot Complete");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        static public void ExportBlock(Dictionary<string, string> param)
        {
            Console.WriteLine($"levelDB.Init {param["db"]}");
            LevelDBStore levelDB = new LevelDBStore();
            levelDB.Init(param["db"]);

            if (param.ContainsKey("height") && long.TryParse(param["height"], out long height))
            {
                levelDB.UndoTransfers(height);
            }
            long.TryParse(levelDB.Get("UndoHeight"), out long transferHeight);
            Console.WriteLine($"transferHeight: {transferHeight}");

            var DatabasePath = $"./Data/LevelDB_ExportBlock_{transferHeight}";
            if (Directory.Exists(DatabasePath))
            {
                Console.WriteLine($"Directory LevelDB_ExportBlock Exists");
                return;
            }
            LevelDBStore snapshotDB = new LevelDBStore();
            snapshotDB.Init(DatabasePath);

            int count = 0;
            using (var it = levelDB.db.CreateIterator())
            {
                for (it.SeekToFirst(); it.IsValid(); it.Next(), count++)
                {
                    //Log.Info($"Value as string: {it.KeyAsString()}");
                    if (it.KeyAsString().IndexOf("_undo_") == -1
                      && it.KeyAsString().IndexOf("Undos___") != 0
                      && (it.KeyAsString().IndexOf("Blocks") == 0 ||  it.KeyAsString().IndexOf("Heights") ==0 ))
                    {
                        snapshotDB.Put(it.KeyAsString(), it.ValueAsString());
                        Console.WriteLine($"Processed Block: {it.KeyAsString()}");
                    }
                    if (count % 1000000 == 0)
                        Console.WriteLine($"Processed Count:{count}");
                }
            }

            Console.WriteLine($"ExportBlock Complete");

            snapshotDB.Dispose();

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }

        }

        static public void PledgeReport(string address)
        {
            LevelDBStore dbstore = Entity.Root.GetComponent<LevelDBStore>();
            long.TryParse(dbstore.Get("UndoHeight"), out long UndoHeight);
            string filename = $"./{address}_{UndoHeight}.csv";
            string keyStgMap = $"StgMap___{address}";

            lock (dbstore.db)
            {
                int count = 0;
                string sum = "0";
                using (var it = dbstore.db.CreateIterator())
                {
                    File.Delete(filename);
                    for (it.SeekToFirst(); it.IsValid(); it.Next(), count++)
                    {
                        var key = it.KeyAsString();
                        if (key.IndexOf(keyStgMap) ==0)
                        {
                            var array = key.Split("_");
                            var conAddress = array[3];
                            var account    = array[5];

                            if (conAddress == address)
                            {
                                File.AppendAllText(filename, $"{account},{it.ValueAsString()}\n");
                            }
                        }
                        if (count % 1000000 == 0)
                            Console.WriteLine($"Processed Count:{count}");
                    }
                    File.AppendAllText(filename, "All" + "," + sum + "\n");
                }

            }

        }

        static public void SnapshotDebug(Dictionary<string, string> param)
        {
            string db1 = param["db1"];
            string db2 = param["db2"];
            if (string.IsNullOrEmpty(db1) || string.IsNullOrEmpty(db2)) {
                Console.WriteLine("db1 or db2 not find.");
                return;
            }

            LevelDBStore levelDB1 = new LevelDBStore();
            levelDB1.Init(param["db1"]);

            LevelDBStore levelDB2 = new LevelDBStore();
            levelDB2.Init(param["db2"]);

            long.TryParse(param["height"], out long height);

            while (true)
            {
                var it1 = levelDB1.Get($"Undos___{height}");
                var slice1 = JsonHelper.FromJson<DbCache<DbUndo>.Slice>(it1);

                var it2 = levelDB2.Get($"Undos___{height}");
                var slice2 = JsonHelper.FromJson<DbCache<DbUndo>.Slice>(it2);

                if (slice1 != null && slice1.obj.height != 0
                  &&slice2 != null && slice2.obj.height != 0)
                {

                }
                else
                {
                    Console.WriteLine($"stop at height: {height}");
                    break;
                }

                height++;
            }
        }

    }


}
