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

            if (param.TryGetValue("index", out string index))
            {
                param["wallet"] = param["wallet"].Replace(".josn", $"{index}.josn");
                param["db"] = param["db"] + index;
            }

            if (param.TryGetValue("miner",out string tmp1))
            {
                Entity.Root.AddComponent<Miner>().Init(param);
                Update();
                return;
            }

            //CalculatePower.Test();
            //return;

            //Wallet.Test2();
            //return;

            // 测试代码
            //LuaVMEnv.TestRapidjson(args);
            //LuaVMEnv.TestLib(args);
            //LuaVMEnv.TestCoroutine(args);
            //LuaVMEnv.Test_number(args);
            //LevelDBStore.test_delete(args);
            //LevelDBStore.test_undo(args);
            //LevelDBStore.Export2CSV_Block(args);
            //return;

            //Log.Info(Environment.CurrentDirectory);
            string walletFile = param["wallet"];
            Wallet wallet = Wallet.GetWallet(walletFile);
            if (wallet == null)
            {
                return;
            }

            if (param.TryGetValue("makeGenesis", out string tmp2))
            {
                Consensus.MakeGenesis();            
                return;
            }

            //DisbleQuickEditMode();
            Console.Clear();
            Console.CursorVisible = false;
            Console.Title = $"SmartX 配置： {param["configure"]} {index} Address: {wallet.GetCurWallet().ToAddress()}";
            Log.Debug($"address: {wallet.GetCurWallet().ToAddress()}");

            string NodeKey = param["node"];
            // 异步方法全部会回调到主线程
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            AssemblyHelper.AddAssembly("Base",typeof(AssemblyHelper).Assembly);
            AssemblyHelper.AddAssembly("App" ,typeof(Program).Assembly);
            // 读取配置文件
            StreamReader sr = new StreamReader(new FileStream(param["configure"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite), System.Text.Encoding.UTF8);
            string strTxt = sr.ReadToEnd();
            sr.Close(); sr.Dispose();
            JToken jd = JToken.Parse(strTxt);

            if (jd[NodeKey] !=null)
            {
                jdNode = jd[NodeKey];
                Log.Debug("启动： " + jdNode["appType"]);

                if (index!=null)
                {
                    jdNode["HttpRpc"]["ComponentNetworkHttp"]["address"]  = ((string)jdNode["HttpRpc" ]["ComponentNetworkHttp"]["address"]).Replace("8001", (8000 + int.Parse(index)).ToString() );
                    jdNode["HttpPool"]["ComponentNetworkHttp"]["address"] = ((string)jdNode["HttpPool"]["ComponentNetworkHttp"]["address"]).Replace("9001", (9000 + int.Parse(index)).ToString());
                    jdNode["ComponentNetworkInner"]["address"] = ((string)jdNode["ComponentNetworkInner"]["address"]).Replace("58601", (58600 + int.Parse(index)).ToString());
                    jdNode["Pool"]["db_path"] = ((string)jdNode["Pool"]["db_path"]) + index;
                }

                // 数据库路径
                if (jdNode["LevelDBStore"] != null && args.Length >= 3)
                {
                    jdNode["LevelDBStore"]["db_path"] = param["db"];
                }

                Entity.Root.AddComponent<ComponentStart>(jd[NodeKey]);
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

                    Monitor.Enter(Entity.Root);
                    OneThreadSynchronizationContext.Instance.Update();
                    Entity.Root.Update();
                    CoroutineMgr.UpdateCoroutine();
                    Monitor.Exit(Entity.Root);
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

    }



}

















