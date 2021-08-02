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

        public virtual void Init(Dictionary<string, string> param)
        {
            param.TryGetValue("address", out address);
            param.TryGetValue("number", out number);
            param.TryGetValue("poolUrl", out poolUrl);
            param.TryGetValue("thread", out string sthread);
            int.TryParse(sthread, out thread);
            if (thread <= 0)
                thread = 1;

            Program.DisbleQuickEditMode();
            Console.Clear();
            Console.CursorVisible = false;

            Run();
        }

        public static string version = "Miner_3.1.0";
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

        TimePass timePassInfo = new TimePass(10);
        TimePass timePassDebug = new TimePass(10);

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

        protected TimePass timePass1 = new TimePass(1f);
        protected TimePass timePass2 = new TimePass(0.5f);

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

            HttpMessage quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();

            while (true)
            {
                try
                {
                    if (timePassInfo.IsPassSet())
                    {
                        Get_Random_diff_max();
                        string hash  = BlockDag.ToHash(height, hashmining_last, random);
                        var sharePer = Math.Round(effectiveShare * 100 / submitCount,2);
                        var power    = CalculatePower.GetPowerCompany(calculatePower.GetPowerDouble());

                        Log.Info($"\n height:{height}, taskid:{taskid},random:{random}, diff:{diff_max_last}, share:{sharePer}%, power:{power} hash:{hash}");
                    }

                    long time = (GetNodeTime() / 1000) % pooltime;
                    if ( string.IsNullOrEmpty(hashmining) && time >= 1 && time < broadcasttime && timePass1.IsPassSet())
                    {
                        Get_Random_diff_max();
                        await Submit(quest);
                        //Log.Info("Task New");
                    }

                    if (!string.IsNullOrEmpty(hashmining) && time > broadcasttime-3 && time <= broadcasttime && timePass2.IsPassSet())
                    {
                        Get_Random_diff_max();
                        if (diff_max > diff_max_lastSubmit)
                        {
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

        public async Task Submit(HttpMessage quest)
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
            HttpMessage result = null;
            try
            {
                result = await ComponentNetworkHttp.Query($"http://{poolUrl}/mining", quest);
            }
            catch (Exception)
            {
                if (timePassDebug.IsPassSet())
                {
                    Log.Warning($"\n Unable to open the network connection http://{poolUrl}/mining");
                    StopMining();
                    await Task.Delay(15000);
                }
            }
            if (result != null && result.map != null)
            {
                if (result.map.ContainsKey("tips"))
                {
                    Log.Warning($"{result.map["tips"]}");
                    await Task.Delay(5000);
                }

                if (result.map["report"]== "error")
                {
                    Log.Warning($"{result.map["tips"]}");
                    await Task.Delay(5000);
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
                        hashmining_last = temphash;
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

                        if (result.map.ContainsKey("nodeTime")) {
                            long.TryParse(result.map["nodeTime"], out nodeTimeOffset);
                        }

                        result.map.TryGetValue("poolPower", out poolPower);
                        changeCallback?.Invoke();

                    }
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
                if ( !string.IsNullOrEmpty(This.hashmining) )
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
                else
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }


    }

}