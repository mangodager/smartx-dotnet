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
#if MultiThread
        private readonly bool bWrite;
#endif

        DbUndo Undos = null;    // 数据回退
        public DbCache<string>       Snap { get; }
        public DbCache<Block>        Blocks { get; }
        public DbCache<List<string>> Heights { get; }
        public DbCache<BlockSub>     Transfers { get; }
        public DbCache<Account>      Accounts { get; }
        public DbCache<LuaVMScript>  Contracts { get; }
        public DbCache<LuaVMContext> Storages { get; }
        public DbCache<string>       StoragesMap { get; }
        public DbCache<List<string>> ABC { get; } // Accounts Bind Contracts

        public DbCache<BlockChain>   BlockChains { get; }
        public DbList<string>        List { get; }
        public DbQueue<string>       Queue { get; }

        public DbSnapshot(DB db, long height, bool bUndo, bool bWrite)
        {
#if MultiThread
            this.bWrite = bWrite;
            if(this.bWrite){
                Monitor.Enter(db);
            }
            this.db = db;
            this.snapshot = db.CreateSnapshot();
            this.batch    = bWrite ? new WriteBatch() : null;
#else
            Monitor.Enter(db);
            this.db = db;
            this.snapshot = db.CreateSnapshot();
            this.batch = new WriteBatch();
#endif

            this.options  = new ReadOptions { FillCache = false, Snapshot = snapshot };

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
            BlockChains = new DbCache<BlockChain>(db, options, batch, Undos, "BlockChain");
            StoragesMap = new DbCache<string>(db, options, batch, Undos, "StgMap");
            ABC       = new DbCache<List<string>>(db, options, batch, Undos, "ABC");
            List      = new DbList<string>(db, options, batch, Undos, "List");
            Queue     = new DbQueue<string>(db, options, batch, Undos, "Queue", 1260);  //  (60/8) * 24Hour * 7Day

        }

        public void Commit()
        {
#if MultiThread
            if (!this.bWrite) {
                throw new Exception("Subthread Commit Error!");
            }
#endif

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
            Queue.Commit();

            if (Undos != null)
            {
                batch?.Put($"Undos___{Undos.height}", JsonHelper.ToJson(new DbCache<DbUndo>.Slice() { obj = Undos }));
                batch?.Put("UndoHeight", Undos.height.ToString());
            }
            db.Write(batch, new WriteOptions { Sync = true });
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

#if MultiThread
                if (this.bWrite) {
                    Monitor.Exit(db);
                }
#else
                Monitor.Exit(db);
#endif
            }
        }

        ~DbSnapshot()
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
