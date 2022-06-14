using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;

namespace ETModel
{

    public class Miner : Component
    {
        public CalculatePower calculatePower = new CalculatePower();
        public ComponentNetworkOuter networkOuter;
        public string KCP;
        public string strJson = @"
    {
        'protocol': 'TCP',
        'RemoteAddress': '0:9101',
        'address': '0:0',
        'CheckHearBeat': 'true'
    }";

        public virtual void Init(Dictionary<string, string> param)
        {
            // 异步方法全部会回调到主线程
            System.Threading.SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            AssemblyHelper.AddAssembly("Base", typeof(AssemblyHelper).Assembly);
            AssemblyHelper.AddAssembly("App", typeof(Program).Assembly);

            param.TryGetValue("address", out address);
            param.TryGetValue("number", out number);
            param.TryGetValue("pool1", out poolUrl);
            param.TryGetValue("thread", out string sthread);
            param.TryGetValue("KCP", out KCP);
            int.TryParse(sthread, out thread);
            if (thread <= 0)
                thread = 1;

            Program.DisbleQuickEditMode();
            Console.Clear();
            Console.CursorVisible = false;

            poolUrl = poolUrl.Replace(" ", "");
            if (!string.IsNullOrEmpty(KCP))
            {
                strJson = strJson.Replace("TCP", "KCP");
            }
            strJson = strJson.Replace("0:9101", poolUrl);
            var newEntity = new Entity("Network");
            this.entity.AddChild(newEntity);
            newEntity.AddComponent<ComponentNetMsg>();
            networkOuter = newEntity.AddComponent<ComponentNetworkOuter>(JToken.Parse(strJson));

            Run();
        }

        public static string version = "Miner_3.3.1";
        public string number;
        public string address;
        public string poolUrl;
        public int thread;

        public long height = 1;
        public string hashmining = null;
        public string random;
        public string taskid = "";
        public double diff_max = 0;
        public double diff_max_lastSubmit = 0;

        public string poolPower = "";
        public int intervalTime = 1000;
        protected Action changeCallback;

        public long nodeTimeOffset;


        public class ThreadData
        {
            public Miner  miner;
            public int    index;
            public double diff_max;
            public string random;
            public string hashmining;
        }
        ThreadData[] ThreadDataList;

        // ========================================================
        static public long pooltime        = 15;
        static public long broadcasttime   = pooltime * 10 / 15;
        static public double diff_max_last = 0;
        static public string hashmining_last = null;
        static public string random_last = null;
        static public float submitCount    = 1;
        static public float effectiveShare = 0;

        public long GetNodeTime()
        {
            return TimeHelper.Now() + nodeTimeOffset;
        }
        // ========================================================
        protected virtual void SetTitle(string title)
        {
            Console.Title = title;
        }

        protected TimePass timePassInfo = new TimePass(7.5f);
        protected TimePass timePassDebug = new TimePass(10);
        protected TimePass timePass1 = new TimePass(0.3f);
        protected TimePass timePass2 = new TimePass(0.3f);

        public async void Run()
        {
            SetTitle($" address:{address},thread:{thread}, number:{number}, poolUrl:{poolUrl}, version:{version}");

            Log.Info($"start mining...");

            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

            //创建后台工作线程
            ThreadDataList = new ThreadData[thread];
            for (int ii = 0; ii < thread; ii++)
            {
                ThreadDataList[ii] = new ThreadData();
                ThreadDataList[ii].miner = this;
                ThreadDataList[ii].index = ii;
                ThreadDataList[ii].diff_max   = 0;
                ThreadDataList[ii].random     = "";
                ThreadDataList[ii].hashmining = "";

                System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Mining));
                thread.IsBackground = true;//设置为后台线程
                thread.Priority = System.Threading.ThreadPriority.Normal;
                thread.Start(ThreadDataList[ii]);
            }

            PoolMessage quest = new PoolMessage();
            quest.map = new Dictionary<string, string>();

            while (true)
            {
                try
                {
                    long time = (GetNodeTime() / 1000) % pooltime;
                    if (string.IsNullOrEmpty(hashmining) && timePassInfo.IsPassSet())
                    {
                        string hash  = BlockDag.ToHash(height, hashmining_last, random_last);
                        var sharePer = Math.Round(effectiveShare * 100 / submitCount,2);
                        var power    = CalculatePower.GetPowerCompany(calculatePower.GetPowerDouble());

                        Log.Info($"\n height:{height}, taskid:{taskid},random:{random_last}, diff:{diff_max_last}, share:{sharePer}%, power:{power} hash:{hash}");
                    }

                    if ( string.IsNullOrEmpty(hashmining) && time >= 1 && time < broadcasttime && timePass1.IsPassSet())
                    {
                        Get_Random_diff_max();
                        await Submit(quest);
                        //Log.Info($"Task New {hashmining}");
                    }

                    if (!string.IsNullOrEmpty(hashmining) && time > broadcasttime-3 && time <= broadcasttime && timePass2.IsPassSet())
                    {
                        Get_Random_diff_max();
                        if (diff_max > diff_max_lastSubmit)
                        {
                            random_last = random;
                            hashmining_last = hashmining;

                            diff_max_last = diff_max;
                            diff_max_lastSubmit = diff_max;
                            await Submit(quest);
                            //Log.Info($"Task Submit {height}");
                        }
                    }

                    if (!string.IsNullOrEmpty(hashmining) && time > broadcasttime)
                    {
                        hashmining = null;
                        changeCallback?.Invoke();
                    }

                }
                catch (Exception )
                {
                    await Task.Delay(15000);
                }
                await Task.Delay(10);
            }
        }

        public virtual void Get_Random_diff_max()
        {
            for (int ii = 0; ii < thread; ii++)
            {
                lock (ThreadDataList[ii])
                {
                    if (ThreadDataList[ii].diff_max > diff_max && !string.IsNullOrEmpty(ThreadDataList[ii].random))
                    {
                        diff_max = ThreadDataList[ii].diff_max;
                        random   = ThreadDataList[ii].random;
                    }
                }
            }
        }

        public async Task Submit(PoolMessage quest)
        {
            quest.map.Clear();
            quest.map.Add("cmd", "submit");
            quest.map.Add("version", version);
            quest.map.Add("height", height.ToString());
            quest.map.Add("address", address);
            quest.map.Add("number", number);
            quest.map.Add("random", random);
            quest.map.Add("taskid", taskid);
            quest.map.Add("average", calculatePower.GetPowerDouble().ToString());

            long sendTime = TimeHelper.Now();
            PoolMessage result = await QueryPool(quest);
            if ((result == null || result.map == null) && timePassDebug.IsPassSet())
            {
                Log.Warning($"\n Unable to open the network connection {poolUrl}");
                StopMining();
                await Task.Delay(15000);
                return;
            }

            if (result.map.ContainsKey("tips"))
            {
                Log.Warning($"{result.map["tips"]}");
                await Task.Delay(5000);
            }

            if (result.map["report"] == "error")
            {
                Log.Warning($"{result.map["tips"]}");
                await Task.Delay(5000);
            }

            if (result.map.ContainsKey("nodeTime"))
            {
                long timeNow = TimeHelper.Now();
                long.TryParse(result.map["nodeTime"], out long nodeTime);
                nodeTimeOffset = (timeNow - sendTime) / 2 + nodeTime - timeNow;
            }

            if (result.map.ContainsKey("minerLimit"))
            {
                if (timePassDebug.IsPassSet())
                {
                    Log.Warning($"\n http://{poolUrl}/mining is full");
                    StopMining();
                    await Task.Delay(15000);
                }
            }
            else
            if (result.map.ContainsKey("taskid"))
            {
                if (result.map.ContainsKey("number"))
                {
                    number = result.map["number"];
                    SetTitle($" address:{address},thread:{thread}, number:{number}, poolUrl:{poolUrl}, version:{version}");
                }

                long.TryParse(result.map["height"], out long tempheight);
                taskid = result.map["taskid"];
                string temphash = result.map["hashmining"];

                if (temphash == null || temphash == "" || temphash != hashmining)
                {
                    if (result.map.TryGetValue("power", out string smypower) && double.TryParse(smypower, out double dmypower))
                    {
                        submitCount++;
                        calculatePower.InsertPower(dmypower);
                        if (dmypower != 0) {
                            effectiveShare++;
                        }
                        else {
                            //calculatePower.Clear();
                        }
                    }

                    hashmining = temphash;
                    height = tempheight;
                    diff_max = 0;
                    diff_max_lastSubmit = 0;
                    random = "";

                    for (int ii = 0; ii < thread; ii++)
                    {
                        lock (ThreadDataList[ii])
                        {
                            ThreadDataList[ii].diff_max   = 0;
                            ThreadDataList[ii].random     = "";
                            ThreadDataList[ii].hashmining = hashmining;
                        }
                    }

                    result.map.TryGetValue("poolPower", out poolPower);
                    changeCallback?.Invoke();

                }
            }
        }

        public void StopMining()
        {
            for (int ii = 0; ii < thread; ii++)
            {
                lock (ThreadDataList[ii])
                {
                    ThreadDataList[ii].diff_max = 0;
                    ThreadDataList[ii].random = "";
                    ThreadDataList[ii].hashmining = null;
                }
            }
        }

        static public void Mining(object data)
        {
            ThreadData This = data as ThreadData;
            while (true)
            {
                if (!string.IsNullOrEmpty(This.hashmining))
                {
                    try
                    {
                        string randomTemp;
                        if (This.miner.taskid != "")
                            randomTemp = This.miner.taskid + System.Guid.NewGuid().ToString("N").Substring(0, 13);
                        else
                            randomTemp = System.Guid.NewGuid().ToString("N").Substring(0, 16);

                        if (randomTemp == "0")
                            Log.Debug("random==\"0\"");

                        string hash = BlockDag.ToHash(This.miner.height, This.hashmining, randomTemp);
                        double diff = Helper.GetDiff(hash);
                        lock (This)
                        {
                            if (diff > This.diff_max)
                            {
                                This.diff_max = diff;
                                This.random = randomTemp;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        public async Task<PoolMessage> QueryPool(PoolMessage msg,float timeout = 5f)
        {
            Q2P_Pool q2P_Pool = new Q2P_Pool();
            q2P_Pool.josn = JsonHelper.ToJson(msg.map);
            R2P_Pool r2P_Pool = await networkOuter.Query(q2P_Pool, timeout) as R2P_Pool;
            var poolMessage = new PoolMessage();
            if (r2P_Pool!=null)
            {
                poolMessage.map = JsonHelper.FromJson<Dictionary<string, string>>(r2P_Pool.josn);
            }
            return poolMessage;
        }

        public T QueryPoolSync<T>(PoolMessage msg, float timeout = 5f)
        {
            Q2P_Pool q2P_Pool = new Q2P_Pool();
            q2P_Pool.josn = JsonHelper.ToJson(msg.map);

            var timepass = new TimePass(timeout);
            var awaiter = networkOuter.Query(q2P_Pool, timeout).GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
                if (timepass.IsPassOnce())
                    return default;
            }

            R2P_Pool r2P_Pool = awaiter.GetResult() as R2P_Pool;
            return JsonHelper.FromJson<T>(r2P_Pool.josn);
        }

    }

}