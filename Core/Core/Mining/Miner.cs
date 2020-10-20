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

        public void Init(Dictionary<string, string> param)
        {
            param.TryGetValue("address", out address);
            param.TryGetValue("number" , out number);
            param.TryGetValue("poolUrl", out poolUrl);
            param.TryGetValue("thread" , out string sthread);
            int.TryParse(sthread, out thread);
            if (thread <= 0)
                thread = 1;

            Run();
        }

        public string number;
        public string address;
        public string poolUrl;
        public int    thread;

        public long   height = 1;
        public string hashmining = "";
        public string random;
        public string taskid = "";
        double diff_max = 0;

        TimePass timePassInfo  = new TimePass(10);
        TimePass timePassDebug = new TimePass(10);
        public async void Run()
        {
            Program.DisbleQuickEditMode();
            Console.Clear();
            Console.CursorVisible = false;
            Console.Title = $" address:{address},thread:{thread}, number:{number}, poolUrl:{poolUrl}";

            Log.Info($"start mining...");

            await Task.Delay(1000);

            //创建后台工作线程
            for (int ii = 0; ii < thread; ii++)
            {
                System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Mining));
                thread.IsBackground = true;//设置为后台线程
                thread.Start(this);
            }

            HttpMessage quest = new HttpMessage() ;
            quest.map = new Dictionary<string, string>();

            while (true)
            {
                try
                {
                    if (timePassInfo.IsPassSet())
                    {
                        string hash = CryptoHelper.Sha256(hashmining + random);
                        Log.Info($"\n height:{height}, taskid:{taskid}, random:{random}, diff:{diff_max}, power:{calculatePower.GetPower()} hash:{hash}");
                    }

                    quest.map.Clear();
                    quest.map.Add("cmd", "submit");
                    quest.map.Add("height" , height.ToString());
                    quest.map.Add("address", address);
                    quest.map.Add("number" , number);
                    quest.map.Add("random" , random);
                    quest.map.Add("taskid", taskid);
                    quest.map.Add("average", calculatePower.GetPowerDouble().ToString());
                    HttpMessage result = null;
                    try
                    { 
                        result = await ComponentNetworkHttp.Query($"http://{poolUrl}/mining", quest);
                    }
                    catch(Exception )
                    {
                        if (timePassDebug.IsPassSet())
                        {
                            Log.Warning($"\n Unable to open the network connection http://{poolUrl}/mining");
                        }
                    }
                    if (result!=null && result.map!=null)
                    {
                        if (result.map.ContainsKey("taskid"))
                        {
                            if (result.map.ContainsKey("number"))
                            {
                                number = result.map["number"];
                                Console.Title = $" address:{address},thread:{thread}, number:{number}, poolUrl:{poolUrl}";
                            }

                            long.TryParse(result.map["height"], out long tempheight);
                            taskid = result.map["taskid"];
                            string temphash = result.map["hashmining"];

                            if (temphash == null || temphash == "" || temphash != hashmining)
                            {
                                if (diff_max!=0)
                                    calculatePower.Insert(diff_max);

                                diff_max = 0;
                                hashmining = temphash;
                                height = tempheight;
                                random = "";
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                await Task.Delay(1000);
            }
        }

        static public void Mining(object data)
        {
            Miner This = data as Miner;
            while (true)
            {
                if (This.hashmining != "")
                {
                    string randomTemp;
                    if (This.taskid != "")
                        randomTemp = This.taskid + System.Guid.NewGuid().ToString("N").Substring(0, 13);
                    else
                        randomTemp = System.Guid.NewGuid().ToString("N").Substring(0, 16);

                    if (randomTemp == "0")
                        Log.Debug("random==\"0\"");

                    string hash = CryptoHelper.Sha256(This.hashmining + randomTemp);

                    double diff = Helper.GetDiff(hash);
                    if (diff > This.diff_max)
                    {
                        This.diff_max = diff;
                        This.random = randomTemp;
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