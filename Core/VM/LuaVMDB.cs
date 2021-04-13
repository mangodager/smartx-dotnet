using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using LevelDB;


namespace ETModel
{
    public class LuaVMDBCache<TValue> where TValue : class
    {
        public class Slice
        {
            public TValue obj;
        }

        protected readonly DbCache<TValue> dbCache;
        protected readonly Dictionary<string, TValue> currDic = new Dictionary<string, TValue>();
        protected readonly List<string> currDel = new List<string>();

        protected readonly Dictionary<string, TValue> copyDic = new Dictionary<string, TValue>();


        public LuaVMDBCache(DbCache<TValue> _dbCache)
        {
            dbCache = _dbCache;
        }

        public void Add(string key, TValue value)
        {
            currDic.Remove(key);
            currDic.Add(key, value);
        }
        public TValue Get(string key)
        {
            TValue currValue = null;
            if (currDic.TryGetValue(key, out currValue))
                return currValue;
            if (copyDic.TryGetValue(key, out currValue))
                return currValue;

            currValue = dbCache.Get(key);

            // 深度复制
            var sliceOld = new Slice();
            sliceOld.obj   = currValue;

            var sliceNew = JsonHelper.FromJson<Slice>(JsonHelper.ToJson(sliceOld));
            copyDic.Remove(key);
            copyDic.Add(key, sliceNew.obj);

            return sliceNew.obj;
        }
        public void Delete(string key)
        {
            currDel.Remove(key);
            currDel.Add(key);
        }
        public void Commit()
        {
            foreach (var key in currDic.Keys)
            {
                dbCache.Add(key, currDic[key]);
            }
            foreach (var key in currDel)
            {
                dbCache.Delete(key);
            }
        }
    }

    public class LuaVMDBList<TValue> where TValue : class
    {
        public class Slice
        {
            public TValue obj;
        }

        protected readonly DbList<TValue> dbList;
        protected readonly Dictionary<string, int>    currIndex = new Dictionary<string, int>();
        protected readonly Dictionary<string, TValue>   currDic = new Dictionary<string, TValue>();

        protected readonly Dictionary<string, TValue>   copyDic = new Dictionary<string, TValue>();

        public LuaVMDBList(DbList<TValue> _dbList)
        {
            dbList = _dbList;
        }

        public int GetCount(string table)
        {
            int index = 0;
            if (currIndex.TryGetValue(table, out index))
                return index;

            return dbList.GetCount(table);
        }

        public TValue Get(string table, int index)
        {
            TValue currValue = null;
            string table_index = $"{table}_#_{index}";
            if (currDic.TryGetValue(table_index, out currValue))
                return currValue;
            if (copyDic.TryGetValue(table_index, out currValue))
                return currValue;

            currValue = dbList.Get(table, index);

            // 深度复制
            var sliceOld = new Slice();
            sliceOld.obj = currValue;

            var sliceNew = JsonHelper.FromJson<Slice>(JsonHelper.ToJson(sliceOld));
            copyDic.Remove(table_index);
            copyDic.Add(table_index, sliceNew.obj);

            return sliceNew.obj;
        }

        public void Add(string table, TValue value)
        {
            int index = GetCount(table);
            SetCount(table, index + 1);

            Set(table, index, value);
        }

        void SetCount(string table, int count)
        {
            currIndex.Remove(table);
            currIndex.Add(table, count);
        }

        void Set(string table, int index, TValue value)
        {
            string table_index = $"{table}_#_{index}";
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
            foreach (var key in currDic.Keys)
            {
                var array = key.Split("_#_");
                dbList.Set(array[0], int.Parse(array[1]), currDic[key]);
            }

            foreach (var key in currIndex.Keys)
            {
                dbList.SetCount(key, currIndex[key]);
            }
        }
    }

    // LuaVM DB to Atomization
    public class LuaVMDB : IDisposable
    {
        public DbSnapshot dbSnapshot;

        public LuaVMDBCache<string>       Snap { get; }
        public LuaVMDBCache<Block>        Blocks { get; }
        public LuaVMDBCache<List<string>> Heights { get; }
        public LuaVMDBCache<BlockSub>     Transfers { get; }
        public LuaVMDBCache<Account>      Accounts { get; }
        public LuaVMDBCache<LuaVMScript>  Contracts { get; }
        public LuaVMDBCache<LuaVMContext> Storages { get; }
        public LuaVMDBCache<string>       StoragesMap { get; }
        public LuaVMDBCache<List<string>> ABC { get; } // Accounts Bind Contracts

        public LuaVMDBCache<BlockChain>   BlockChains { get; }
        public LuaVMDBList<string>        List { get; }


        public LuaVMDB(DbSnapshot _dbSnapshot)
        {
            dbSnapshot = _dbSnapshot;

            Snap      = new LuaVMDBCache<string>(dbSnapshot.Snap);
            Blocks    = new LuaVMDBCache<Block>(dbSnapshot.Blocks);
            Heights   = new LuaVMDBCache<List<string>>(dbSnapshot.Heights);
            Transfers = new LuaVMDBCache<BlockSub>(dbSnapshot.Transfers);
            Accounts  = new LuaVMDBCache<Account>(dbSnapshot.Accounts);
            Contracts = new LuaVMDBCache<LuaVMScript>(dbSnapshot.Contracts);
            Storages  = new LuaVMDBCache<LuaVMContext>(dbSnapshot.Storages);
            BlockChains = new LuaVMDBCache<BlockChain>(dbSnapshot.BlockChains);
            StoragesMap = new LuaVMDBCache<string>(dbSnapshot.StoragesMap);
            ABC         = new LuaVMDBCache<List<string>>(dbSnapshot.ABC);
            List        = new LuaVMDBList<string>(dbSnapshot.List);

        }

        public void Commit()
        {
            Snap.Commit();
            Blocks.Commit();
            Heights.Commit();
            Transfers.Commit();
            Accounts.Commit();
            Contracts.Commit();
            Storages.Commit();
            BlockChains.Commit();
            StoragesMap.Commit();
            ABC.Commit();
            List.Commit();
        }

        public virtual void Dispose()
        {

        }

        ~LuaVMDB()
        {

        }

        public void BindTransfer2Account(string account, string transfer)
        {
            List.Add($"TFA__{account}", transfer);
        }

        public void Add(string key, string value)
        {
            Snap.Add(key, value);
        }

        public string Get(string key)
        {
            return Snap.Get(key);
        }

        public void Delete(string key)
        {
            Snap.Delete(key);
        }

    }

}