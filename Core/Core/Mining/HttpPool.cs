using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETModel
{
    public class MinerTask
    {
        public long   height;
        public string address;
        public string number;
        public List<string> random = new List<string>();
        public double diff;
        public float  time;
        public string taskid;

        public double power;
        public string power_cur;
        public string power_average;
    }

    // Http连接
    public class HttpPool : Component
    {
        protected Dictionary<long, Dictionary<string, MinerTask>> Miners = new Dictionary<long, Dictionary<string, MinerTask>>();
        public        int    minerLimit  = 2000;
        public static double ignorePower = 600;  // 最小算力
        public static string poolPassword  = "";

        ComponentNetworkHttp networkHttp;
        public override void Awake(JToken jd = null)
        {
            if (jd["minerLimit"] != null)
                int.TryParse(jd["minerLimit"]?.ToString(), out minerLimit);
            if (jd["ignorePower"] != null)
                double.TryParse(jd["ignorePower"]?.ToString(), out ignorePower);

            poolPassword = jd["poolPassword"]?.ToString();

            Log.Info($"HttpPool.minerLimit   = {minerLimit}");
            Log.Info($"HttpPool.ignorePower  = {ignorePower}");

            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);
        }

        public IPEndPoint GetHttpAdderrs()
        {
            return networkHttp.ipEndPoint;
        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"HttpMiner http://{networkHttp.ipEndPoint}/");

        }

        public long height = 2;
        protected string hashmining = "";
        protected string power = "";
        public void SetMinging(long h, string hash, string p)
        {
            lock (this)
            {
                height = h;
                hashmining = hash;
                power = p;
            }
        }

        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || networkHttp == null
                || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;
            switch (httpMessage.map["cmd"].ToLower())
            {
                case "submit":
                    OnSubmit(httpMessage);
                    break;
                case "registerpool":
                    HttpPoolRelay.OnRegisterPool(httpMessage);
                    break;
                case "getmcblock":
                    HttpPoolRelay.OnGetMcBlock(httpMessage);
                    break;
                default:
                    break;
            }
            //TestOnSubmit(httpMessage);
        }

        public void TestOnSubmit(HttpMessage httpMessage)
        {
            if (httpMessage.map["cmd"].ToLower() != "submit")
            {
                return;
            }

            var resultMap = JsonHelper.FromJson<Dictionary<string, string>>(httpMessage.result);
            resultMap.TryGetValue("hashmining", out string hashmining);

            if (Pool.registerPool || string.IsNullOrEmpty(hashmining))
            {
                return;
            }

            string address = httpMessage.map["address"];
            string number  = httpMessage.map["number"];
            for (int i = 0; i < 20000; i++)
            {
                MinerTask minerTask = NewNextTakID(height, address, number);
                if (minerTask != null)
                {
                    string randomTemp;
                    randomTemp = minerTask.taskid + System.Guid.NewGuid().ToString("N").Substring(0, 13);

                    string hash = BlockDag.ToHash(minerTask.height, hashmining, randomTemp);
                    double diff = Helper.GetDiff(hash);
                    double power = CalculatePower.Power(diff);

                    minerTask.diff = diff;
                    minerTask.random.Add(randomTemp);
                    minerTask.power = power;
                    minerTask.power_average = power.ToString();
                }
            }

        }

        // http://127.0.0.1:8088/mining?cmd=Submit
        public void OnSubmit(HttpMessage httpMessage)
        {
            // submit
            Dictionary<string, string> map = new Dictionary<string, string>();
            try
            {
                map.Add("report", "refuse");

                // 版本检查
                string version = httpMessage.map["version"];
                if (version != Miner.version) {
                    map.Remove("report");
                    map.Add("report", "error");
                    map.Add("tips", $"Miner.version: {Miner.version}");
                    httpMessage.result = JsonHelper.ToJson(map);
                    return;
                }

                // 矿池登记检查
                if (Pool.registerPool&&HttpPoolRelay.poolIpAddress.IndexOf(httpMessage.request.RemoteEndPoint.Address.ToString())==-1)
                {
                    map.Add("tips", "need register Pool");
                    httpMessage.result = JsonHelper.ToJson(map);
                    return;
                }

                long minerHeight = long.Parse(httpMessage.map["height"]);
                string address = httpMessage.map["address"];
                string number = httpMessage.map["number"];
                MinerTask minerTask = GetMyTaskID(minerHeight, address, number, httpMessage.map["taskid"]);
                if (minerHeight == height && !string.IsNullOrEmpty(hashmining)
                 && minerTask != null && TimeHelper.time - minerTask.time > 0.1)
                {
                    minerTask.time = TimeHelper.time;
                    string random  = httpMessage.map["random"];
                    string hash    = BlockDag.ToHash(height, hashmining, random);
                    double diff    = Helper.GetDiff(hash);
                    double power   = CalculatePower.Power(diff);
                    if (power>=HttpPool.ignorePower
                        &&diff>minerTask.diff
                        &&!string.IsNullOrEmpty(random)
                        &&(Pool.registerPool ||random.IndexOf(minerTask.taskid)==0))
                    {
                        ///Log.Info($"OnSubmit: Height:{height} A:{address} P:{power} H:{hash}");

                        minerTask.diff = diff;
                        minerTask.random.Add(random);
                        minerTask.power  = power;
                        minerTask.power_average = httpMessage.map["average"];
                        map.Remove("report");
                        map.Add("report", "accept");
                    }
                }

                if (minerHeight != height && !string.IsNullOrEmpty(hashmining))
                {
                    bool limitNewNext = true;
                    // 矿池人数限制
                    if ( !Miners.TryGetValue(height - 1, out Dictionary<string, MinerTask> list_1) || list_1.Count < minerLimit || list_1.TryGetValue($"{address}_{number}", out MinerTask temp) )
                    {
                        limitNewNext = false;
                    }
                    if (Miners.TryGetValue(height, out Dictionary<string, MinerTask> list_0)&&list_0.Count >= minerLimit)
                    {
                        limitNewNext = true;
                    }

                    if (!limitNewNext)
                    {
                        MinerTask newMinerTask = NewNextTakID(height, address, number);
                        // new task
                        map.Add("height"    , height.ToString());
                        map.Add("hashmining", hashmining);
                        map.Add("taskid"    , newMinerTask.taskid);
                        map.Add("power"     , minerTask?.power.ToString());
                        map.Add("nodeTime"  , GetNodeTime().ToString());
                        if (newMinerTask.number != number)
                        {
                            map.Add("number", newMinerTask.number);
                        }
                    }
                    else
                    {
                        map.Add("minerLimit", minerLimit.ToString());
                    }
                }

            }
            catch (Exception)
            {
                map.Remove("report");
                map.Add("report", "error");
            }
            httpMessage.result = JsonHelper.ToJson(map);
        }

        public long GetNodeTime()
        {
            var nodeManager = Entity.Root.GetComponent<NodeManager>();
            if (nodeManager != null)
            {
                return nodeManager.GetNodeTime();
            }
            var httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (httpPoolRelay != null)
            {
                return httpPoolRelay.GetNodeTime();
            }
            throw new Exception("GetNodeTime error");
        }

        public Dictionary<string, MinerTask> GetMiner(long height)
        {
            lock (this)
            {
                Miners.TryGetValue(height, out Dictionary<string, MinerTask> value);
                return value;
            }
        }

        public Dictionary<string, MinerTask> GetMinerRewardMin(out long height)
        {
            lock (this)
            {
                if (Miners.Keys.Count > 0)
                {
                    height = Miners.Keys.Min(t => t);
                    Miners.TryGetValue(height, out Dictionary<string, MinerTask> value);
                    return value;
                }
                height = 0;
                return null;
            }
        }

        public Dictionary<string, MinerTask> GetMinerRewardMax(out long height)
        {
            lock (this)
            {
                if (Miners.Keys.Count > 0)
                {
                    height = Miners.Keys.Max(t => t);
                    Miners.TryGetValue(height, out Dictionary<string, MinerTask> value);
                    return value;
                }
                height = 0;
                return null;
            }
        }

        public void DelMiner(long height)
        {
            lock (this)
            {
                Miners.Remove(height);
            }
        }

        public MinerTask NewNextTakID(long height, string address, string number)
        {
            lock (this)
            {
                Miners.TryGetValue(height, out Dictionary<string, MinerTask> list);
                if (list == null)
                {
                    list = new Dictionary<string, MinerTask>();
                    Miners.Add(height, list);
                }

                var minerTask = new MinerTask();
                minerTask.height = height;
                minerTask.address = address;
                minerTask.number = number;
                minerTask.taskid = System.Guid.NewGuid().ToString("N").Substring(0, 3);

                while (true)
                {
                    string address_number = $"{minerTask.address}_{minerTask.number}";
                    if (list.TryGetValue(address_number, out MinerTask temp))
                    {
                        minerTask.number = System.Guid.NewGuid().ToString("N").Substring(0, 6);
                    }
                    else
                    {
                        list.Add(address_number, minerTask);
                        break;
                    }
                }
                return minerTask;
            }
        }

        public MinerTask GetMyTaskID(long height, string address, string number, string taskid)
        {
            lock (this)
            {
                Miners.TryGetValue(height, out Dictionary<string, MinerTask> list);
                if (list != null)
                {
                    string address_number = $"{address}_{number}";
                    list.TryGetValue(address_number, out MinerTask minerTask);
                    if (minerTask != null && minerTask.address == address && minerTask.height == height && minerTask.number == number && minerTask.taskid == taskid)
                    {
                        return minerTask;
                    }
                }
                return null;
            }
        }

    }


}