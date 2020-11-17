using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LevelDB;
using Microsoft.VisualBasic.CompilerServices;

namespace ETModel
{
    public class DbList<TValue> where TValue : class
    {
        protected readonly DB db;
        protected readonly ReadOptions options;
        protected readonly WriteBatch batch;
        protected readonly string prefix;
        protected readonly DbUndo undos;

        protected readonly Dictionary<string, int> currIndex = new Dictionary<string, int>();
        protected readonly Dictionary<string, TValue> currDic   = new Dictionary<string, TValue>();
        bool isString = false;

        public class Slice
        {
            public TValue obj;
        }

        public DbList(DB db, ReadOptions options, WriteBatch batch, DbUndo undos, string prefix)
        {
            this.db = db;
            this.options = options ?? DbSnapshot.ReadOptions_Default;
            this.batch = batch;
            this.prefix = prefix;
            this.undos = undos;
            this.isString = typeof(TValue).Name == "String";

        }

        public int GetCount(string table)
        {
            int index = 0;
            string table_key = $"{prefix}___{table}__Count";
            if (currIndex.TryGetValue(table_key, out index))
                return index;

            string value = db.Get(table_key, options);
            if (value != null)
            {
                int.TryParse(value,out index);
            }
            return index;
        }

        public TValue Get(string table,int index)
        {
            TValue currValue = null;
            string table_index = $"{prefix}___{table}__{index}";
            if (currDic.TryGetValue(table_index, out currValue))
                return currValue;

            string value = db.Get(table_index, options);
            if (value != null)
            {
                if (!isString)
                    return JsonHelper.FromJson<Slice>(value).obj;
                return value as TValue;
            }
            return null;
        }

        public void Add(string table, TValue value)
        {
            int index = GetCount(table);
            SetCount(table, index + 1);

            Set(table, index, value);
        }

        void SetCount(string table, int count)
        {
            string table_key = $"{prefix}___{table}__Count";

            // 数据回退
            if (undos != null && !currIndex.ContainsKey(table_key))
            {
                string old = db.Get(table_key, options);
                batch?.Put($"{table_key}_undo_{undos.height}", old ?? "");
                undos.keys.Add(table_key);
            }
            currIndex.Remove(table_key);
            currIndex.Add(table_key, count);
        }

        void Set(string table, int index, TValue value)
        {
            string table_index = $"{prefix}___{table}__{index}";
            // 数据回退
            if (undos != null && !currDic.ContainsKey(table_index))
            {
                string old = db.Get(table_index, options);
                batch?.Put($"{table_index}_undo_{undos.height}", old ?? "");
                undos.keys.Add(table_index);
            }
            currDic.Remove(table_index);
            currDic.Add(table_index, value);
        }

        public TValue Del(string table, int index)
        {
            int count = GetCount(table);
            SetCount(table, count - 1);
            TValue value = Get(table, count - 1);
            Set(table, index, value);
            Set(table, count - 1, null);
            return null;
        }

        public void Commit()
        {
            Slice slice = new Slice();
            foreach (var key in currDic.Keys)
            {
                var value = currDic[key];
                if (!isString)
                {
                    lock (slice)
                    {
                        slice.obj = value;
                        batch?.Put(key, JsonHelper.ToJson(slice));
                        slice.obj = null;
                    }
                }
                else
                {
                    batch?.Put(key, value as string);
                }
            }

            foreach (var key in currIndex.Keys)
            {
                var value = currIndex[key];
                batch?.Put(key, value.ToString());
            }
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




    }




}
