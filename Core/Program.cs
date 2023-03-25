using System;
using System.Threading;
//using Base;
//using NLog;
using JsonFx;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;
using XLua;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;

namespace ETModel
{
	internal static class Program
	{
        public static JToken jdNode;
        private static void Main(string[] args)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            for (int i=0;i<args.Length;i++)
            {
                int jj = args[i].IndexOf(':');
                if(jj!=-1)
                    param.TryAdd(args[i].Substring(0,jj).Replace("-",""), args[i].Substring(jj+1, args[i].Length-(jj+1)));
                else
                    param.TryAdd(args[i].Replace("-", ""), "true");
            }

            param.TryAdd("configure", "");
            param.TryAdd("node", "All");
            param.TryAdd("wallet", "./Data/wallet.json");
            param.TryAdd("db", "./Data/LevelDB");
            param.TryAdd("node", "All");

            if (param.TryGetValue("index", out string index))
            {
                param["wallet"] = param["wallet"].Replace(".json", $"{index}.json");
                param["db"] = param["db"] + index;
            }

            if (param.ContainsKey("miner"))
            {
                RandomXSharp.RandomX.randomx_init(param.ContainsKey("fullmem"));
                Entity.Root.AddComponent<Miner>().Init(param);
                Update();
                return;
            }

            RandomXSharp.RandomX.randomx_init(false);

            if (Test(args))
            {
                return;
            }

            if (param.TryGetValue("makeSnapshot", out string _))
            {
                LevelDBStore.MakeSnapshot(param);
                return;
            }

            if (param.TryGetValue("ExportBlock", out string _))
            {
                LevelDBStore.ExportBlock(param);
                return;
            }

            if (param.TryGetValue("DBRepair", out string _path))
            {
                LevelDBStore.Repair(_path);
                return;
            }

            if (param.TryGetValue("makeGenesis", out string _))
            {
                string walletFile = param["wallet"];
                var wallet1 = Wallet.GetWallet(walletFile);
                if (wallet1 == null)
                {
                    return;
                }
                Consensus.MakeGenesis();
                return;
            }

            string NodeKey = param["node"];
            // 异步方法全部会回调到主线程
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            AssemblyHelper.AddAssembly("Base",typeof(AssemblyHelper).Assembly);
            AssemblyHelper.AddAssembly("App" ,typeof(Program).Assembly);

            // 读取配置文件
            try
            {
                StreamReader sr = new StreamReader(new FileStream(param["configure"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite), System.Text.Encoding.UTF8);
                string strTxt = sr.ReadToEnd();
                strTxt = strTxt.Replace("0.0.0.0", NodeManager.GetIpV4());
                sr.Close(); sr.Dispose();
                JToken jd = JToken.Parse(strTxt);
                jdNode = jd[NodeKey];
            }
            catch (Exception e)
            {
                Log.Info(e.ToString());
                Log.Error($"configure file: {param["configure"]} on exists ro json foramt error.");
                Console.ReadKey();
                return;
            }

            Wallet wallet = null;
            if (jdNode!=null)
            {
                // check contract
                if (jdNode["UseWallet"] == null || jdNode["UseWallet"].ToString()=="true" )
                {
                    string walletFile = param["wallet"];
                    wallet = Wallet.GetWallet(walletFile);
                    if (wallet == null)
                    {
                        return;
                    }

                    var contractHash = FileHelper.HashDirectory("Data/Contract", CryptoHelper.Sha256);
                    if (contractHash != "64cbbe81160d15d7a1d49d815303d03d142e673119a3c827cd15c623525b855b")
                    {
                        Log.Debug($"contractHash error: {contractHash}");
#if RELEASE
                        return;
#endif
                    }
                }

                //DisbleQuickEditMode();
                Console.Clear();
                Console.CursorVisible = false;
#if !RELEASE
            Console.Title = $"SmartX 配置： {param["configure"]} {index} Address: {wallet?.GetCurWallet().ToAddress()} Debug";
#else
                Console.Title = $"SmartX 配置： {param["configure"]} {index} Address: {wallet?.GetCurWallet().ToAddress()} Release";
#endif
                Log.Debug($"address: {wallet?.GetCurWallet().ToAddress()}");

                Log.Debug("启动： " + jdNode["appType"]);

                if (!string.IsNullOrEmpty(index))
                {
                    if(jdNode["HttpRpc"]!=null)
                        jdNode["HttpRpc"]["ComponentNetworkHttp"]["address"]  = ((string)jdNode["HttpRpc" ]["ComponentNetworkHttp"]["address"]).Replace("8101", (8100 + int.Parse(index)).ToString() );
                    if(jdNode["HttpPool"] !=null)
                        jdNode["HttpPool"]["ComponentNetworkInner"]["address"] = ((string)jdNode["HttpPool"]["ComponentNetworkInner"]["address"]).Replace("9101", (9100 + int.Parse(index)).ToString());
                    if(jdNode["SmartxRpc"] !=null)
                        jdNode["SmartxRpc"]["ComponentNetworkHttp"]["address"] = ((string)jdNode["SmartxRpc"]["ComponentNetworkHttp"]["address"]).Replace("5000", ((5000 - 1) + int.Parse(index)).ToString());
                    if(jdNode["ComponentNetworkInner"] !=null)
                        jdNode["ComponentNetworkInner"]["address"] = ((string)jdNode["ComponentNetworkInner"]["address"]).Replace("58601", (58600 + int.Parse(index)).ToString());
                    if (jdNode["publicIP"] != null)
                        jdNode["publicIP"] = ((string)jdNode["publicIP"]).Replace("58601", (58600 + int.Parse(index)).ToString());
                    if (jdNode["RelayNetwork"] !=null)
                        jdNode["RelayNetwork"]["ComponentNetworkInner"]["address"] = ((string)jdNode["RelayNetwork"]["ComponentNetworkInner"]["address"]).Replace("57601", (57600 + int.Parse(index)).ToString());
                    if(jdNode["Pool"] != null)
                        jdNode["Pool"]["db_path"] = ((string)jdNode["Pool"]["db_path"]) + index;
                    if (jdNode["HttpPoolRelay"] != null)
                        jdNode["HttpPoolRelay"]["number"] = jdNode["HttpPoolRelay"]["number"].ToString().Replace("Pool1","Pool"+index);
                }

                // 数据库路径
                if (jdNode["LevelDBStore"] != null && args.Length >= 3)
                {
                    jdNode["LevelDBStore"]["db_path"] = param["db"];
                }

                Entity.Root.AddComponent<ComponentStart>(jdNode);
            }

            Update();

        }

        #region 关闭控制台 快速编辑模式、插入模式
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_INSERT_MODE = 0x0020;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int hConsoleHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        public static void DisbleQuickEditMode()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr hStdin = GetStdHandle(STD_INPUT_HANDLE);
                uint mode;
                GetConsoleMode(hStdin, out mode);
                mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
                mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
                SetConsoleMode(hStdin, mode);
            }
        }
        #endregion

        static public void Update()
        {
            //应用程序域下未处理的错误
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            float lasttime = TimeHelper.time;
            while (true)
            {
                try
                {
                    TimeHelper.deltaTime = TimeHelper.time - lasttime;
                    lasttime = TimeHelper.time;

                    Thread.Sleep(1);

                    //lock (Entity.Root)
                    {
                        OneThreadSynchronizationContext.Instance.Update();
                        Entity.Root.Update();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(e.ExceptionObject.ToString());
        }

        private static bool Test(string[] args)
        {
            //RandomXSharp.RandomX.Test1(args);
            //return;

            //Wallet.Import("");
            //return;

            //BigHelper.Test();
            //return;

            //CalculatePower.Test();
            //return;

            //Wallet.Test2();
            //return;

            //Wallet.Test3();
            //return;

            // 测试代码
            //LuaVMEnv.TestRapidjson(args);
            //LuaVMEnv.TestLib(args);
            //LuaVMEnv.TestCoroutine(args);
            //LuaVMEnv.Test_number(args);
            //LevelDBStore.test_delete(args);
            //LevelDBStore.test_undo(args);
            //LevelDBStore.Export2CSV_Block(args);
            //LevelDBStore.test_ergodic(args);
            //return;
            //Log.Info(Environment.CurrentDirectory);

            return false;
        }

    }



}

















