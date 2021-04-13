using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LevelDB;
using Microsoft.VisualBasic.CompilerServices;

namespace ETModel
{
    public class DbQueue<TValue> where TValue : class
    {
        protected readonly DB db;
        protected readonly ReadOptions options;
        protected readonly WriteBatch batch;
        protected readonly string prefix;
        protected readonly DbUndo undos;

        protected readonly Dictionary<string, int> currIndex = new Dictionary<string, int>();
        protected readonly Dictionary<string, TValue> currDic   = new Dictionary<string, TValue>();
        bool isString = false;

        protected readonly int maxCount;

        public class Slice
        {
            public TValue obj;
        }

        public DbQueue(DB db, ReadOptions options, WriteBatch batch, DbUndo undos, string prefix, int _maxCount)
        {
            this.db = db;
            this.options = options ?? DbSnapshot.ReadOptions_Default;
            this.batch = batch;
            this.prefix = prefix;
            this.undos = undos;
            this.maxCount = _maxCount;
            this.isString = typeof(TValue).Name == "String";

        }

        public int GetTopIndex(string table)
        {
            int index = 0;
            string table_key = $"{prefix}___{table}__TopIndex";
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

        public void Push(string table, TValue value)
        {
            int index = GetTopIndex(table);
            SetTopIndex(table, index + 1);

            Set(table, index, value);

            if(index>=maxCount)
            {
                string table_index = $"{prefix}___{table}__{index-maxCount}";
                batch?.Delete(table_index);
            }
        }

        protected void SetTopIndex(string table, int count)
        {
            string table_key = $"{prefix}___{table}__TopIndex";

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

        protected void Set(string table, int index, TValue value)
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


    }




}
