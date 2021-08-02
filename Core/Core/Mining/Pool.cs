using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace ETModel
{

    public class Pool : Component
    {
        HttpPool httpPool = null;
        public TransferProcess transferProcess = null;
        public LevelDBStore PoolDBStore = new LevelDBStore();
        public string style = "SOLO";
        public static float  serviceFee = 0; // 矿池手续费
        public long   OutTimeDBMiner   = 5760*3;
        public long   OutTimeDBCounted = 100;
        public long   RewardInterval   = 32;
        public string ownerAddress = "";
        public static bool registerPool = false;

        public override void Awake(JToken jd = null)
        {
            string db_path = jd["db_path"]?.ToString();
            var DatabasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), db_path);
            PoolDBStore.Init(DatabasePath);

            style = jd["style"]?.ToString();
            float.TryParse(jd["serviceFee"]?.ToString(), out serviceFee);
            serviceFee = MathF.Max(0, MathF.Min(0.3f, serviceFee));
            if (jd["registerPool"] != null)
                bool.TryParse(jd["registerPool"]?.ToString(), out registerPool);
            if (jd["OutTimeDBMiner"] != null)
                long.TryParse(jd["OutTimeDBMiner"]?.ToString(), out OutTimeDBMiner);
            if (jd["OutTimeDBCounted"] != null)
                long.TryParse(jd["OutTimeDBCounted"]?.ToString(), out OutTimeDBCounted);
            if (jd["RewardInterval"] != null)
                long.TryParse(jd["RewardInterval"]?.ToString(), out RewardInterval);

            if (registerPool)
            {
                Log.Info($"Pool.registerPool=true --> Pool.style=SOLO");
                style = "SOLO";
                RewardInterval = Math.Max(8, Math.Min(32, RewardInterval));
            }
            else
            if (style == "PPLNS")
            {
#if RELEASE
                RewardInterval = Math.Max(120, Math.Min(240*24, RewardInterval));
#else
                RewardInterval = Math.Max(32, RewardInterval);
#endif
            }
            Log.Info($"HttpPool.style           = {style}");
            Log.Info($"HttpPool.registerPool    = {registerPool}");
            Log.Info($"HttpPool.RewardInterval  = {RewardInterval}");

            httpPool = Entity.Root.GetComponentInChild<HttpPool>();
            transferProcess = Entity.Root.AddComponent<TransferProcess>();
        }

        public override void Start()
        {
            ownerAddress = Wallet.GetWallet().GetCurWallet().ToAddress();
            System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ThreadRun));
            thread.IsBackground = true;//设置为后台线程
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.Start(this);
        }

        int runSleep = 10; // idle 7500
        public void ThreadRun(object data)
        {
            System.Threading.Thread.Sleep(1000);
            while (true)
            {
                System.Threading.Thread.Sleep(runSleep);

                try
                {
                    MinerSave();
                    var minerTransfer = MinerReward();
                    ClearOutTimeDB();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void MinerSave()
        {
            if (httpPool != null)
            {
                Dictionary<string, MinerTask> miners = httpPool.GetMinerRewardMin(out long miningHeight);
                if (miners != null && miningHeight + 3 < httpPool.height)
                {
                    using (DbSnapshot snapshot = PoolDBStore.GetSnapshot(0, true))
                    {
                        string json = snapshot.Get("Pool_H_Miner");
                        long height_miner = -1;
                        if (!string.IsNullOrEmpty(json)) {
                            long.TryParse(json, out height_miner);
                        }

                        if (height_miner==-1 || height_miner < miningHeight)
                        {
                            snapshot.Add("Pool_H_Miner", miningHeight.ToString());
                            snapshot.Add("Pool_H2_" + miningHeight, JsonHelper.ToJson(miners));
                            snapshot.Commit();
                            httpPool.DelMiner(miningHeight);
                        }
                    }
                }
            }
        }

        public class MinerRewardDB
        {
            public long counted;
            public long minHeight;
            public long maxHeight;
            public string time;
            public string totalPower;
        }

        TimePass MinerReward_TimePass = new TimePass(15 * 8);

        public Dictionary<string, BlockSub> MinerReward(bool saveDB = true)
        {
            long counted = 0;
            long minHeight = -1;
            long maxHeight = -1;
            using (DbSnapshot snapshot = PoolDBStore.GetSnapshot(0,true))
            {
                string str_Counted = snapshot.Get("Pool_Counted");
                long.TryParse(str_Counted,out counted);
                string str_MR = snapshot.Get($"Pool_MR_{str_Counted}");
                MinerRewardDB minerRewardLast = null;
                if (!string.IsNullOrEmpty(str_MR))
                    minerRewardLast = JsonHelper.FromJson<MinerRewardDB>(str_MR);
                if (minerRewardLast==null)
                {
                    if (httpPool.height <= 2) // 还没有获取到最新高度
                        return null;
                    snapshot.Add("Pool_Counted", "0");
                    var minerRewardNew = new MinerRewardDB();
                    minerRewardNew.counted = 0;
                    minerRewardNew.minHeight = httpPool.height;
                    minerRewardNew.maxHeight = httpPool.height;
                    minerRewardNew.time = TimeHelper.time.ToString();
                    snapshot.Add($"Pool_MR_{0}", JsonHelper.ToJson(minerRewardNew));
                    snapshot.Commit();
                    return null;
                }
                minHeight = minerRewardLast.maxHeight;

                string json = snapshot.Get("Pool_H_Miner");
                if (!string.IsNullOrEmpty(json)){
                    long.TryParse(json, out maxHeight);
                }
            }

            // 超过一天的数据忽略
            if (maxHeight - minHeight > 5760)
            {
                Log.Warning($"MinerReward maxHeight - minHeight > 5760, {maxHeight} - {minHeight} = {maxHeight - minHeight}");
            }

            // 设置步长
            if (maxHeight - minHeight >= RewardInterval)
            {
                maxHeight = minHeight + RewardInterval;
                runSleep = saveDB ? 10 : runSleep; // 加快处理速度
            }

            // 缓存===========================================================================
            Dictionary<string, BlockSub> minerTransferCache = null;
            if (MinerReward_TimePass.IsPassSet())
            {
                minerTransferCache = MinerReward(false);
                if (minerTransferCache != null)
                {
                    using (DbSnapshot snapshot = PoolDBStore.GetSnapshot(0, true))
                    {
                        snapshot.Add($"Pool_Cache_MinerReward", JsonHelper.ToJson(minerTransferCache));
                        snapshot.Commit();
                    }
                }
            }
            // ===============================================================================

            if (maxHeight - minHeight < RewardInterval && saveDB)
            {
                runSleep = 7500;
                return null;
            }

            Dictionary<string, BlockSub> minerTransfer = null;
            if (style == "PPLNS")
            {
                minerTransfer = minerTransferCache ?? GetMinerReward_PPLNS(minHeight, maxHeight);
            }
            if (style == "SOLO")
            {
                minerTransfer = minerTransferCache ?? GetMinerReward_SOLO(minHeight, maxHeight);
            }

            if (saveDB)
            {
                foreach (var it in minerTransfer)
                {
                    transferProcess.AddTransferHandle(it.Value.addressIn, it.Value.addressOut, it.Value.amount, it.Value.data);
                }

                using (DbSnapshot snapshot = PoolDBStore.GetSnapshot(0, true))
                {
                    counted += 1;
                    snapshot.Add("Pool_Counted", counted.ToString());
                    var minerRewardNew = new MinerRewardDB();
                    minerRewardNew.counted = counted;
                    minerRewardNew.minHeight = minHeight;
                    minerRewardNew.maxHeight = maxHeight;
                    minerRewardNew.time = DateTime.Now.Ticks.ToString();
                    snapshot.Add($"Pool_MR_{counted}", JsonHelper.ToJson(minerRewardNew));

                    // Pool_MT
                    var depend = new DateTime(DateTime.Now.Ticks).ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var it in minerTransfer)
                    {
                        it.Value.depend = depend;
                        snapshot.Queue.Push($"Pool_MT_{it.Value.addressOut}", JsonHelper.ToJson(it.Value));
                    }
                    snapshot.Commit();
                }

            }

            return minerTransfer;
        }

        static public float GetServiceFee()
        {
            return Pool.serviceFee + HttpPoolRelay.rulerServiceFee;
        }

        // Miner reward, only after confirming that it cannot be rolled back
        public Dictionary<string, BlockSub> GetMinerReward_PPLNS(long minHeight,long maxHeight)
        {
            Dictionary<string, BlockSub> minerTransfer = new Dictionary<string, BlockSub>();
            if (httpPool != null)
            {
                string addressIn = Wallet.GetWallet().GetCurWallet().ToAddress();

                for (long rewardheight = minHeight; rewardheight < maxHeight; rewardheight++)
                {
                    Dictionary<string, MinerTask> miners = null;
                    using (DbSnapshot snapshot = PoolDBStore.GetSnapshot())
                    {
                        string json = snapshot.Get("Pool_H2_" + rewardheight);
                        if (!string.IsNullOrEmpty(json)) { 
                            miners = JsonHelper.FromJson<Dictionary<string, MinerTask>>(json);
                        }
                    }

                    if (miners!= null)
                    {
                        var mcblk = GetMcBlock(rewardheight);
                        if (mcblk == null)
                            throw new Exception($"GetMcBlock({rewardheight}) return null");

                        if (mcblk != null && mcblk.Address == ownerAddress)
                        {
                            double reward = Consensus.GetReward(rewardheight);
                            reward = reward * (1.0f- GetServiceFee());

                            var miner = miners.Values.FirstOrDefault(c => c.random.IndexOf(mcblk.random)!=-1);
                            if (miner == null)
                            {
                                continue;
                            }

                            // Total power
                            double powerSum = 0;
                            var Values = miners.Values.ToList();
                            for (var ii = 0; ii < Values.Count; ii++)
                            {
                                var dic = Values[ii];
                                if (string.IsNullOrEmpty(dic.address))
                                    continue;
                                double power = CalculatePower.Power(dic.diff);
                                if (power < HttpPool.ignorePower)
                                    continue;
                                powerSum += power;
                            }

                            // Reward for participation
                            BlockSub transfer = null;
                            for (var ii = 0; ii < Values.Count; ii++)
                            {
                                var dic = Values[ii];
                                if (string.IsNullOrEmpty(dic.address))
                                    continue;
                                double power = CalculatePower.Power(dic.diff);
                                double pay = Math.Round(power * reward / powerSum, 8) ;

                                if (minerTransfer.TryGetValue(dic.address, out transfer))
                                {
                                    if (power < HttpPool.ignorePower)
                                    {
                                        transfer.height += 1; // 这里表示无效提交数
                                        continue;
                                    }

                                    transfer.nonce += 1; // 这里表示有效提交数
                                    transfer.amount = BigHelper.Add(transfer.amount, pay.ToString("N8") );
                                }
                                else
                                if(pay>=0.002)
                                {
                                    transfer = new BlockSub();
                                    transfer.nonce = 1; // 这里表示有效提交数
                                    transfer.addressIn  = addressIn;
                                    transfer.addressOut = dic.address;
                                    transfer.amount     = BigHelper.Sub(pay.ToString("N8"), "0.002"); // 扣除交易手续费
                                    transfer.data = CryptoHelper.Sha256($"{mcblk.hash}_{maxHeight}_{ownerAddress}_{dic.address}_MinerReward");
                                    if (power < HttpPool.ignorePower)
                                    {
                                        transfer.height += 1; // 这里表示无效提交数
                                        continue;
                                    }

                                    minerTransfer.Add(transfer.addressOut, transfer);
                                }
                            }

                        }
                    }
                }

                // 有效提交次数越多收益越高
                var totalAmount1 = "0"; // 总账1
                var totalAmount2 = "0"; // 总账2
                foreach (var transfer in minerTransfer.Values)
                {
                    try
                    {
                        totalAmount1 = BigHelper.Add(totalAmount1, transfer.amount);
                        var totalSubmit = transfer.nonce + transfer.height;
                        var share = (float)transfer.nonce / (float)totalSubmit;
                        transfer.type = $"{Math.Round(share * 100, 2)}%"; // 有效提交百分比
                        share *= share;
                        transfer.remarks = BigHelper.Mul(share.ToString(), transfer.amount);
                        totalAmount2 = BigHelper.Add(totalAmount2, transfer.remarks);
                    }
                    catch (Exception)
                    {
                        transfer.type    = "0%";
                        transfer.remarks = "0";
                    }
                }

                var totalAmount3 = "0"; // 总账3
                foreach (var transfer in minerTransfer.Values)
                {
                    try
                    {
                        transfer.amount = BigHelper.Div(BigHelper.Mul(transfer.remarks, totalAmount1), totalAmount2);
                        totalAmount3 = BigHelper.Add(totalAmount3, transfer.amount);
                    }
                    catch (Exception)
                    {
                    }
                }

                //if (BigHelper.Greater(BigHelper.Abs(BigHelper.Sub(totalAmount1, totalAmount3)), "0.002", true))
                //{
                //    Log.Warning($"|totalAmount1 - totalAmount3| > 0.002 {BigHelper.Sub(totalAmount1, totalAmount3)}");
                //}

            }
            return minerTransfer;
        }

        public Dictionary<string, BlockSub> GetMinerReward_SOLO(long minHeight, long maxHeight)
        {
            Dictionary<string, BlockSub> minerTransfer = new Dictionary<string, BlockSub>();
            if (httpPool != null)
            {
                string addressIn = Wallet.GetWallet().GetCurWallet().ToAddress();

                for (long rewardheight = minHeight; rewardheight < maxHeight; rewardheight++)
                {
                    Dictionary<string, MinerTask> miners = null;
                    using (DbSnapshot snapshot = PoolDBStore.GetSnapshot())
                    {
                        string json = snapshot.Get("Pool_H2_" + rewardheight);
                        if (!string.IsNullOrEmpty(json))
                        {
                            miners = JsonHelper.FromJson<Dictionary<string, MinerTask>>(json);
                        }
                    }

                    if (miners != null)
                    {
                        var mcblk = GetMcBlock(rewardheight);
                        if (mcblk != null && mcblk.Address == ownerAddress)
                        {
                            var miner = miners.Values.FirstOrDefault(c => c.random.IndexOf(mcblk.random) != -1);
                            if (miner != null)
                            {
                                BigFloat reward = new BigFloat(Consensus.GetReward(rewardheight));
                                reward = reward * (1.0f - GetServiceFee());

                                var transfer = new BlockSub();
                                transfer.addressIn  = addressIn;
                                transfer.addressOut = miner.address;
                                string pay = BigHelper.Round8(reward.ToString());

                                if (minerTransfer.TryGetValue(miner.address, out transfer))
                                {
                                    transfer.amount = BigHelper.Add(transfer.amount, pay);
                                }
                                else
                                {
                                    transfer = new BlockSub();
                                    transfer.addressIn  = addressIn;
                                    transfer.addressOut = miner.address;
                                    transfer.amount = BigHelper.Sub(pay, "0.002"); // 扣除交易手续费
                                    transfer.type = "100%"; // 有效提交百分比
                                    transfer.data = CryptoHelper.Sha256($"{mcblk.hash}_{maxHeight}_{ownerAddress}_{miner.address}_Reward_SOLO");
                                    minerTransfer.Add(transfer.addressOut, transfer);
                                }
                            }
                        }
                    }
                }
            }
            return minerTransfer;
        }

        public class MinerView
        {
            public string address;
            public string amount_cur;
            public string share = "0%";
            public string totalPower;
            public long   totalMiners;
            public List<BlockSub>      transfers = new List<BlockSub>();
            public List<MinerViewData>    miners = new List<MinerViewData>();
        }

        public class MinerViewData
        {
            public string number;
            public string power_cur;
            public string power_average;
            public float  lasttime;

        }

        TimePass getMinerViewTimePass = new TimePass(15);
        Dictionary<string, MinerView> getMinerViewCache = new Dictionary<string, MinerView>();
        public MinerView GetMinerView(string address,long transferIndex,long transferColumn, long minerIndex,  long minerColumn)
        {
            if (getMinerViewTimePass.IsPassSet())
                getMinerViewCache.Clear();
            if (getMinerViewCache.TryGetValue($"{address}_{transferIndex}_{transferColumn}_{minerIndex}_{minerColumn}", out MinerView minerViewLast))
            {
                return minerViewLast;
            }

            transferColumn = Math.Min(transferColumn, 100);
            minerColumn    = Math.Min(minerColumn, 100);

            var minerView = new MinerView();
            minerView.address = address;

            Dictionary<string, BlockSub> minerTransferCache = null;
            using (DbSnapshot snapshot = PoolDBStore.GetSnapshot())
            {
                var str = snapshot.Get($"Pool_Cache_MinerReward");
                if (!string.IsNullOrEmpty(str))
                {
                    minerTransferCache = JsonHelper.FromJson<Dictionary<string, BlockSub>>(str);
                }
            }

            var transfers_cur  = minerTransferCache?.Values.FirstOrDefault(c => c.addressOut == address);
            if(transfers_cur!=null)
            {
                minerView.amount_cur = transfers_cur.amount;
                minerView.share      = transfers_cur.type;
            }

            // 交易确认
            using (DbSnapshot snapshot = PoolDBStore.GetSnapshot())
            {
                int TopIndex = snapshot.Queue.GetTopIndex($"Pool_MT_{address}");
                for (int ii = 1; ii <= (int)transferColumn; ii++)
                {
                    var value = snapshot.Queue.Get($"Pool_MT_{address}", TopIndex - (int)transferIndex - ii);
                    if (!string.IsNullOrEmpty(value))
                    {
                        var transfer = JsonHelper.FromJson<BlockSub>(value);
                        if (transfer != null)
                        {
                            minerView.transfers.Add(transfer);
                        }
                    }
                }

                foreach (var transfer in minerView.transfers)
                {
                    // 节点使用自己的地址挖矿
                    if (transfer.addressIn == transfer.addressOut)
                        transfer.hash = transfer.addressIn;
                    else
                        transfer.hash = TransferProcess.GetMinerTansfer(snapshot, transfer.data);
                }
            }

            var miners = httpPool.GetMinerRewardMin(out long miningHeight);
            var minerList = miners?.Values.Where((x) => x.address == address).ToList();
            double totalPower = 0L;

            if (minerList != null) {

                minerList.Sort((MinerTask a, MinerTask b) => {
                    return a.number.CompareTo(b.number);
                });

                // 当前页矿机算力
                for (var ii = 0; ii < minerColumn; ii++)
                {
                    if ((minerIndex + ii) >= minerList.Count)
                        break;
                    var miner = minerList[(int)minerIndex + ii];

                    if (string.IsNullOrEmpty(miner.power_cur))
                        miner.power_cur = CalculatePower.GetPowerCompany(CalculatePower.Power(miner.diff));

                    var minerdata = new MinerViewData();
                    minerdata.number = miner.number;
                    minerdata.lasttime = miner.time;
                    minerdata.power_cur = miner.power_cur;

                    double.TryParse(miner.power_average, out double power_average);
                    minerdata.power_average = CalculatePower.GetPowerCompany(power_average);
                    minerView.miners.Add(minerdata);
                }

                // 当前总算力
                for (var ii = 0; ii < minerList.Count; ii++)
                {
                    var miner = minerList[ii];
                    if (double.TryParse(miner.power_average, out double power_average))
                    {
                        totalPower += power_average;
                    }
                }
                minerView.totalMiners = minerList.Count;
            }

            minerView.totalPower  = CalculatePower.GetPowerCompany(totalPower);

            getMinerViewCache.Add($"{address}_{transferIndex}_{transferColumn}_{minerIndex}_{minerColumn}", minerView);
            return minerView;
        }

        TimePass minerViewAbstractTimePass = new TimePass(15);
        Dictionary<string, MinerView> minerViewAbstractCache = new Dictionary<string, MinerView>();
        public MinerView GetMinerViewAbstract(string address)
        {
            if (minerViewAbstractTimePass.IsPassSet())
                minerViewAbstractCache.Clear();
            if (minerViewAbstractCache.TryGetValue(address, out MinerView minerViewLast))
            {
                return minerViewLast;
            }

            var minerView = new MinerView();
            // pool name
            var httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (httpPoolRelay != null)
            {
                minerView.address = httpPoolRelay.number;
            }
            else{
                minerView.address = "Ruler";
            }

            Dictionary<string, BlockSub> minerTransferCache = null;
            using (DbSnapshot snapshot = PoolDBStore.GetSnapshot())
            {
                var str = snapshot.Get($"Pool_Cache_MinerReward");
                if (!string.IsNullOrEmpty(str))
                {
                    minerTransferCache = JsonHelper.FromJson<Dictionary<string, BlockSub>>(str);
                }
            }

            var transfers_cur = minerTransferCache?.Values.FirstOrDefault(c => c.addressOut == address);
            if (transfers_cur != null)
            {
                minerView.amount_cur = transfers_cur.amount;
                minerView.share = transfers_cur.type;
            }

            // 当前总算力
            var miners = httpPool.GetMinerRewardMin(out long miningHeight);
            var minerList = miners?.Values.Where((x) => x.address == address).ToList();
            double totalPower = 0L;
            if (minerList != null)
            {
                minerList.Sort((MinerTask a, MinerTask b) => {
                    return a.number.CompareTo(b.number);
                });

                for (var ii = 0; ii < minerList.Count; ii++)
                {
                    var miner = minerList[ii];
                    if (double.TryParse(miner.power_average, out double power_average))
                    {
                        totalPower += power_average;
                    }
                }
                minerView.totalMiners = minerList.Count;
            }
            minerView.totalPower = CalculatePower.GetPowerCompany(totalPower);

            minerViewAbstractCache.Add(address, minerView);
            return minerView;
        }

        public int GetTransfersCount()
        {
            return transferProcess.transfers.Count;
        }


        public void ClearOutTimeDB()
        {
            using (DbSnapshot snapshot = PoolDBStore.GetSnapshot(0, true))
            {
                string str_Counted = snapshot.Get("Pool_Counted");
                long counted = 0;
                long.TryParse(str_Counted, out counted);
                string str_MR = snapshot.Get($"Pool_MR_{counted-1}");
                MinerRewardDB minerRewardLast = null;
                if (!string.IsNullOrEmpty(str_MR)) {
                    minerRewardLast = JsonHelper.FromJson<MinerRewardDB>(str_MR);
                }
                if (minerRewardLast != null)
                {
                    bool bCommit = false;
                    int delCount = 5760 * 3;
                    for (long ii = counted - 2 ; ii > counted - delCount; ii--)
                    {
                        string key = $"Pool_MR_{ii}";
                        if (!string.IsNullOrEmpty(snapshot.Get(key)))
                        {
                            bCommit = true;
                            snapshot.Delete(key);
                        }
                        else
                            break;
                    }

                    // Miner
                    for (long ii = minerRewardLast.minHeight; ii > minerRewardLast.minHeight - delCount; ii--)
                    {
                        string key = $"Pool_H2_{ii}";
                        if (!string.IsNullOrEmpty(snapshot.Get(key)))
                        {
                            bCommit = true;
                            snapshot.Delete(key);
                        }
                        else
                            break;
                    }

                    if (bCommit)
                    {
                        snapshot.Commit();
                    }
                }
            }

        }

        public MinerView GetMinerTop(long minerIndex, long minerColumn)
        {
            minerColumn = Math.Min(minerColumn, 100);

            var minerView = new MinerView();

            if (httpPool != null)
            {
                Dictionary<string, MinerTask> miners = httpPool.GetMinerRewardMin(out long miningHeight);
                if (miners != null)
                {
                    var minerList = miners.Values.OrderByDescending( (x)=> { double.TryParse(x.power_average, out double power_average); return power_average; } ).ToList();
                    for (var ii = 0; ii < minerColumn; ii++)
                    {
                        if ((minerIndex + ii) >= minerList.Count)
                            break;
                        var miner = minerList[(int)minerIndex + ii];

                        if (string.IsNullOrEmpty(miner.power_cur))
                            miner.power_cur = CalculatePower.GetPowerCompany(CalculatePower.Power(miner.diff));

                        var minerdata = new MinerViewData();
                        minerdata.number = $"{miner.address}_{ miner.number}";
                        minerdata.lasttime = miner.time;
                        minerdata.power_cur = miner.power_cur;

                        double.TryParse(miner.power_average, out double power_average);
                        minerdata.power_average = CalculatePower.GetPowerCompany(power_average);
                        minerView.miners.Add(minerdata);
                    }
                }
            }

            return minerView;
        }

        public Block GetMcBlock(long rewardheight)
        {
            var httpPoolRelay = Entity.Root.GetComponent<HttpPoolRelay>();
            if (httpPoolRelay != null)
            {
                return httpPoolRelay.GetMcBlock(rewardheight);
            }
            return BlockChainHelper.GetMcBlock(rewardheight);
        }

    }


}






















