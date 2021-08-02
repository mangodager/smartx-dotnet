using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace ETModel
{
    public class HttpPoolRelay : Miner
    {
        public class PoolBlock
        {
            public string hash;       // 块哈希
            public string Address;    // 出块地址
            public string random;     // 随机数
        }

        public static List<string> poolInfos     = new List<string>();
        public static List<string> poolIpAddress = new List<string>();

        Pool pool     = null;
        HttpRpc  httpRpc  = null;
        HttpPool httpPool = null;
        public string rulerRpc = null;
        public string rulerWeb = null;

        static public float rulerServiceFee = 0; // Ruler的手续费

        public override void Awake(JToken jd = null)
        {
            address = Wallet.GetWallet().GetCurWallet().ToAddress();
            number  = jd["number"]?.ToString();
            poolUrl = jd["poolUrl"]?.ToString();
            rulerRpc = jd["rulerRpc"]?.ToString();
            rulerWeb = jd["rulerWeb"]?.ToString();
            thread = 0;
            intervalTime = 100;

            poolUrl  = poolUrl.Replace("http://", "");
            rulerRpc = rulerRpc.Replace("http://", "");
            rulerWeb = rulerWeb.Replace("http://", "");

            Log.Info($" poolUrl:{poolUrl}");
            Log.Info($"rulerRpc:{rulerRpc}");
            Log.Info($"rulerWeb:{rulerWeb}");

            changeCallback += OnMiningChange;
        }

        public async override void Start()
        {
            timePass1 = new TimePass(0.1f);
            timePass2 = new TimePass(0.1f);

            pool = Entity.Root.GetComponentInChild<Pool>();
            httpPool = Entity.Root.GetComponentInChild<HttpPool>();
            httpRpc  = Entity.Root.GetComponentInChild<HttpRpc>();
            pool.transferProcess.rulerRpc = rulerRpc;

            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(RunRegisterPool));
            thread.IsBackground = true;//设置为后台线程
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.Start(this);

            await Task.Delay(3000);

            Run();
        }

        public void OnMiningChange()
        {
            httpPool?.SetMinging(height, hashmining, poolPower);
        }

        protected override void SetTitle(string title)
        {
        }

        TimePass timepassRegister = new TimePass(15*6);
        TimePass timepassGetBlock = new TimePass(7.5f);
        public void RunRegisterPool(object This)
        {
            System.Threading.Thread.Sleep(1000);
            try
            {
                RegisterPool();
            }
            catch (Exception) {
            }

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                try
                {
                    if(timepassRegister.IsPassSet())
                        RegisterPool();
                    if(timepassGetBlock.IsPassSet())
                        GetMcBlock(height-3);
                }
                catch (Exception)
                {
                }
            }
        }

        public override void Get_Random_diff_max()
        {
            Dictionary<string, MinerTask> miners = httpPool.GetMiner(height);
            if (miners != null)
            {
                double diff = miners.Values.Max(t => t.diff);
                var miner = miners.Values.FirstOrDefault(c => c.diff == diff);
                if (miner != null && miner.random != null&& miner.random.Count>0)
                {
                    diff_max = diff;
                    random   = miner.random[miner.random.Count - 1];
                }
            }
        }

        // Pool Call
        public void RegisterPool()
        {
            HttpMessage quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();
            quest.map.Clear();
            quest.map.Add("cmd", "registerPool");
            quest.map.Add("version", version);
            quest.map.Add("password", HttpPool.poolPassword);
            quest.map.Add("poolInfo", $"{number}->0.0.0.0:{httpPool.GetHttpAdderrs().Port}##http://0.0.0.0:{httpRpc.GetHttpAdderrs().Port}");
            HttpMessage result = null;
            result = ComponentNetworkHttp.QuerySync($"http://{poolUrl}/mining", quest,5);
            if (result != null && result.map != null)
            {
                if (result.map.TryGetValue("report", out string report) && report!= "accept")
                {
                    Log.Warning($"\n {result.map["tips"]}");
                    return;
                }
                if (result.map.TryGetValue("ownerAddress",out string ownerAddress))
                {
                    if (pool.ownerAddress != ownerAddress)
                    {
                        pool.ownerAddress = ownerAddress;
                        Log.Info($"RegisterPool addr: {ownerAddress}");
                    }
                }
                if (result.map.TryGetValue("serviceFee", out string serviceFee))
                {
                    float.TryParse(serviceFee, out float fee);
                    if (rulerServiceFee != fee)
                    {
                        rulerServiceFee = fee;
                        Log.Info($"RegisterPool  fee: {rulerServiceFee}");
                    }
                }
            }
        }

        // Ruler Call 
        public static void OnRegisterPool(HttpMessage httpMessage)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            string version = httpMessage.map["version"];
            if (version != Miner.version)
            {
                map.Remove("report");
                map.Add("report", "error");
                map.Add("tips", $"Miner.version: {Miner.version}");
                httpMessage.result = JsonHelper.ToJson(map);
                return;
            }

            if (!Pool.registerPool)
            {
                map.Add("report", "refuse");
                map.Add("tips", $"Ruler no support registerPool!");
                httpMessage.result = JsonHelper.ToJson(map);
                return;
            }

            httpMessage.map.TryGetValue("password", out string password);
            if ( !string.IsNullOrEmpty(HttpPool.poolPassword) && password != HttpPool.poolPassword)
            {
                map.Add("report", "refuse");
                map.Add("tips", $"poolPassword error");
                httpMessage.result = JsonHelper.ToJson(map);
                return;
            }

            string IPAddress = httpMessage.request.RemoteEndPoint.Address.ToString();
            string poolInfo = httpMessage.map["poolInfo"].Replace("0.0.0.0", IPAddress);

            if (poolInfos.IndexOf(poolInfo) == -1) {
                Log.Info($"RegisterPool: {IPAddress} {poolInfo}");
            }

            poolIpAddress.Remove(IPAddress);
            poolIpAddress.Add(IPAddress);
            poolInfos.Remove(poolInfo);
            poolInfos.Add(poolInfo);
            map.Add("report", "accept");
            map.Add("ownerAddress", Wallet.GetWallet().GetCurWallet().ToAddress());
            map.Add("serviceFee", Pool.serviceFee.ToString());
            httpMessage.result = JsonHelper.ToJson(map);
        }

        public Block GetMcBlock(long ___height)
        {
            if (___height <= 1)
                return null;

            try
            {
                PoolBlock poolBlock = null;
                using (DbSnapshot snapshot = pool.PoolDBStore.GetSnapshot())
                {
                    var str = snapshot.Get($"PoolBlock_{___height}");
                    if(!string.IsNullOrEmpty(str)) {
                        poolBlock = JsonHelper.FromJson<PoolBlock>(str);
                        if(poolBlock!=null&&Wallet.CheckAddress(poolBlock.Address)) {
                            return new Block() { hash = poolBlock.hash , Address = poolBlock.Address ,random = poolBlock.random };
                        }
                    }
                }

                HttpMessage quest = new HttpMessage();
                quest.map = new Dictionary<string, string>();
                quest.map.Add("cmd", "GetMcBlock");
                quest.map.Add("version", version);
                quest.map.Add("height", ___height.ToString());
                var result = ComponentNetworkHttp.QueryStringSync($"http://{poolUrl}", quest, 5);
                if (string.IsNullOrEmpty(result))
                    return null;
                poolBlock = JsonHelper.FromJson<PoolBlock>(result);
                if (poolBlock!=null&&Wallet.CheckAddress(poolBlock.Address))
                {
                    using (DbSnapshot snapshot = pool.PoolDBStore.GetSnapshot())
                    {
                        snapshot.Add($"PoolBlock_{___height}",JsonHelper.ToJson(poolBlock));
                        snapshot.Commit();
                    }
                    return new Block() { hash = poolBlock.hash, Address = poolBlock.Address, random = poolBlock.random };
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static void OnGetMcBlock(HttpMessage httpMessage)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            // 版本检查
            string version = httpMessage.map["version"];
            if (version != Miner.version)
            {
                map.Remove("report");
                map.Add("report", "error");
                map.Add("tips", $"Miner.version: {Miner.version}");
                return;
            }

            // 矿池登记检查
            if (Pool.registerPool &&poolIpAddress.IndexOf(httpMessage.request.RemoteEndPoint.Address.ToString())==-1)
            {
                map.Add("tips", "need register Pool");
                return;
            }

            long ___height = long.Parse(httpMessage.map["height"]);

            var mcblk = BlockChainHelper.GetMcBlock(___height);
            if (mcblk != null)
            {
                var poolBlock = new PoolBlock();
                poolBlock.hash    = mcblk.hash;
                poolBlock.Address = mcblk.Address ;
                poolBlock.random  = mcblk.random  ;
                httpMessage.result = JsonHelper.ToJson(poolBlock);
            }
        }

    }


}
