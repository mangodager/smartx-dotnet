using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LevelDB;
using Microsoft.VisualBasic.CompilerServices;

namespace ETModel
{
    public class DbCache<TValue> where TValue : class
    {
        protected readonly DB db;
        protected readonly ReadOptions options;
        protected readonly WriteBatch  batch;
        protected readonly string      prefix;
        protected readonly DbUndo      undos;

        protected readonly Dictionary<string,TValue> currDic = new Dictionary<string, TValue>();
        bool isString = false;

        public class Slice
        {
            public TValue obj;
        }

        public DbCache(DB db, ReadOptions options, WriteBatch batch, DbUndo undos, string prefix)
        {
            this.db = db;
            this.options = options ?? DbSnapshot.ReadOptions_Default;
            this.batch   = batch;
            this.prefix  = prefix;
            this.undos   = undos;
            this.isString = typeof(TValue).Name == "String";
        }

        public void Add(string key, TValue value)
        {
            // 数据回退
            if (undos!=null&&!currDic.ContainsKey(key))
            {
                string old = db.Get($"{prefix}___{key}", options);
                batch?.Put($"{prefix}___{key}_undo_{undos.height}", old??"");
                undos.keys.Add($"{prefix}___{key}");
            }
            currDic.Remove(key);
            currDic.Add(key,value);
        }

        public TValue Get(string key)
        {
            TValue currValue = null;
            if (currDic.TryGetValue(key, out currValue))
                return currValue;

            string value = db.Get($"{prefix}___{key}", options);
            if (value != null)
            {
                if(!isString)
                    return JsonHelper.FromJson<Slice>(value).obj;
                return value as TValue;
            }
            return null;
        }

        public void Delete(string key)
        {
            batch?.Delete($"{prefix}___{key}");
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
                        batch?.Put($"{prefix}___{key}", JsonHelper.ToJson(slice));
                        slice.obj = null;
                    }
                }
                else
                {
                    batch?.Put($"{prefix}___{key}", value as string);
                }
            }
        }


    }


}
