using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using LevelDB;

namespace ETModel 
{
    public class DbSnapshot : IDisposable
    {
        public static readonly WriteOptions WriteOptions_Default = new WriteOptions();
        public static readonly ReadOptions  ReadOptions_Default  = new ReadOptions();

        private readonly DB db;
        private  SnapShot    snapshot;
        private  WriteBatch  batch;
        private  ReadOptions options;

        DbUndo Undos = null;    // 数据回退
        private DbCache<string>      Snap { get; }
        public DbCache<Block>        Blocks { get; }
        public DbCache<List<string>> Heights { get; }
        public DbCache<BlockSub>     Transfers { get; }
        public DbCache<Account>      Accounts { get; }
        public DbCache<LuaVMScript>  Contracts { get; }
        public DbCache<LuaVMContext> Storages { get; }
        public DbCache<string>       StoragesAccount { get; }
        public DbCache<string>       TFA { get; } // Accounts_TransfersIndex bing Transfers_hash
        public DbCache<BlockChain>   BlockChains { get; }

        public DbSnapshot(DB db, long height, bool bUndo)
        {
            Monitor.Enter(db);

            this.db = db;
            this.snapshot = db.CreateSnapshot();
            this.batch = new WriteBatch();
            this.options = new ReadOptions { FillCache = false, Snapshot = snapshot };

            if (bUndo)
            {
                Undos = new DbUndo();
                Undos.height = height;
                long.TryParse(db.Get("UndoHeight"), out long UndoHeight);
                if (UndoHeight + 1 != Undos.height)
                    throw new InvalidOperationException($"{UndoHeight} heightTotal+1 != Undos.height {Undos.height}");
            }

            Snap = new DbCache<string>(db, options, batch, Undos, "Snap");
            Blocks    = new DbCache<Block>(db, options, batch, null, "Blocks");
            Heights   = new DbCache<List<string>>(db, options, batch, null, "Heights");
            Transfers = new DbCache<BlockSub>(db, options, batch, Undos, "Trans");
            Accounts  = new DbCache<Account>(db, options, batch, Undos, "Accounts");
            Contracts = new DbCache<LuaVMScript>(db, options, batch, Undos, "Contracts");
            Storages  = new DbCache<LuaVMContext>(db, options, batch, Undos, "Storages");
            TFA       = new DbCache<string>(db, options, batch, Undos, "TFA");
            BlockChains = new DbCache<BlockChain>(db, options, batch, Undos, "BlockChain");
            StoragesAccount = new DbCache<string>(db, options, batch, Undos, "StgAcc");

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
            TFA.Commit();
            BlockChains.Commit();
            StoragesAccount.Commit();

            if (Undos != null)
            {
                batch?.Put($"Undos___{Undos.height}", JsonHelper.ToJson(new DbCache<DbUndo>.Slice() { obj = Undos }));
                batch.Put("UndoHeight", Undos.height.ToString());
            }
            db.Write(batch, new WriteOptions { Sync = true });
        }

        public void Delete(string key)
        {
            batch?.Delete("key");
        }

        public virtual void Dispose()
        {
            if (snapshot != null)
            {
                options?.Dispose();
                batch?.Dispose();
                snapshot?.Dispose();
                options = null;
                batch = null;
                snapshot = null;
                Monitor.Exit(db);
            }
        }

        ~DbSnapshot()
        {

        }

        public void BindTransfer2Account(string account, long transfersIndex, string transfer)
        {
            TFA.Add( $"{account}_{transfersIndex}", transfer);
        }

        public void Add(string key, string value)
        {
            Snap.Add(key, value);
        }

        public string Get(string key)
        {
            return Snap.Get(key);
        }

    }
}
