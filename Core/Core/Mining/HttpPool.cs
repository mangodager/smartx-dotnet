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
        public string random;
        public double diff;
        public float  time;
        public string taskid;

        public string power_cur;
        public string power_average;
    }

    // Http连接
    public class HttpPool : Component
    {
        protected Dictionary<long, Dictionary<string, MinerTask>> Miners = new Dictionary<long, Dictionary<string, MinerTask>>();

        ComponentNetworkHttp networkHttp;
        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);
        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"HttpMiner http://{networkHttp.ipEndPoint}/");

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
                default:
                    break;
            }
        }

        public long height = 2;
        protected string hashmining = "";
        protected string power = "";
        public void SetMinging(long h, string hash, string p)
        {
            height = h;
            hashmining = hash;
            power = p;
        }

        // http://127.0.0.1:8088/mining?cmd=Submit
        public virtual void OnSubmit(HttpMessage httpMessage)
        {
            // submit
            Dictionary<string, string> map = new Dictionary<string, string>();
            try
            {
                map.Add("report", "refuse");

                long minerHeight = long.Parse(httpMessage.map["height"]);
                string address = httpMessage.map["address"];
                string number = httpMessage.map["number"];
                if (minerHeight == height && hashmining != "")
                {
                    MinerTask minerTask = GetMyTaskID(minerHeight, address, number, httpMessage.map["taskid"]);
                    if (minerTask != null && TimeHelper.time - minerTask.time > 0.1)
                    {
                        minerTask.time = TimeHelper.time;
                        string random = httpMessage.map["random"];
                        string hash = CryptoHelper.Sha256(hashmining + random);
                        double diff = Helper.GetDiff(hash);
                        if (diff > minerTask.diff)
                        {
                            minerTask.diff = diff;
                            minerTask.random = random;
                            minerTask.power_average = httpMessage.map["average"];
                            map.Add("report", "accept");
                        }
                    }
                }

                if (minerHeight != height && hashmining != "")
                {
                    MinerTask minerTask = NewNextTakID(height, address, number);
                    // new task
                    map.Add("height", height.ToString());
                    map.Add("hashmining", hashmining);
                    map.Add("taskid", minerTask.taskid);
                    map.Add("power" , power);
                    if(minerTask.number!= number) {
                        map.Add("number", minerTask.number);
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

        public virtual Dictionary<string, MinerTask> GetMiner(long height)
        {
            Miners.TryGetValue(height, out Dictionary<string, MinerTask> value);
            return value;
        }

        public virtual Dictionary<string, MinerTask> GetMinerReward(out long height)
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

        public virtual void DelMiner(long height)
        {
            Miners.Remove(height);
        }


        uint auto_takid = 1;
        public MinerTask NewNextTakID(long height, string address, string number)
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
            minerTask.taskid = (auto_takid++).ToString();

            while(true)
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

        public MinerTask GetMyTaskID(long height, string address, string number, string taskid)
        {
            Miners.TryGetValue(height, out Dictionary<string, MinerTask> list);
            if (list != null)
            {
                string address_number = $"{address}_{number}";
                list.TryGetValue(address_number, out MinerTask minerTask);
                if (minerTask!=null&&minerTask.address == address && minerTask.height == height && minerTask.number == number && minerTask.taskid == taskid)
                {
                    return minerTask;
                }
            }
            return null;
        }

    }


}