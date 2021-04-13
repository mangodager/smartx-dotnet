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

    public class AutoBeRuler : Component
    {
        public bool bRun = true;

        public override void Awake(JToken jd = null)
        {
            if (jd["Run"] != null)
                bool.TryParse(jd["Run"].ToString(), out bRun);


        }

        public override void Start()
        {
            if (bRun)
            {
                Run();
            }

        }

        public async void Run()
        {
            await Task.Delay(2000);
            var httpRpc = Entity.Root.Find("HttpRpc")?.GetComponent<HttpRpc>();
            if (httpRpc == null)
                return;
            var consensus  = Entity.Root.GetComponent<Consensus>();
            var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

            HttpMessage httpMessage = new HttpMessage();
            string address = Wallet.GetWallet().GetCurWallet().ToAddress();
            string consAddress = "";

            while (true)
            {
                await Task.Delay(60000*2);

                try
                {
                    if (consensus.IsRule(consensus.transferHeight, address))
                        continue;

                    if (string.IsNullOrEmpty(consAddress))
                    {
                        if (httpMessage.map == null)
                        {
                            httpMessage.map = new Dictionary<string, string>();
                            httpMessage.map.Add("1", consensus.PledgeFactory);
                            httpMessage.map.Add("2", $"getPair(\"{address}\")");
                        }
                        httpRpc.callFun(httpMessage);
                        if (!httpMessage.result.Contains("error"))
                        {
                            consAddress = JsonHelper.FromJson<string>(httpMessage.result);
                            using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
                            {
                                if (!luaVMEnv.IsERC(dbSnapshot, consAddress, null)) {
                                    consAddress = "";
                                }
                            }
                        }

                    }
                    if (string.IsNullOrEmpty(consAddress))
                        continue;

                    if (!Wallet.CheckAddress(consAddress))
                        continue;

                    Account account = null;
                    using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot(0))
                    {
                        account = dbSnapshot.Accounts.Get(consAddress);
                    }
                    if (account == null)
                        continue;

                    var rulers = consensus.GetRule(consensus.transferHeight);
                    if (rulers == null)
                        continue;

                    int rulerCount = 0;
                    string rulerAmountMin = "";
                    foreach (RuleInfo info in rulers.Values)
                    {
                        if (info.End == -1 || info.End > consensus.transferHeight)
                        {
                            rulerCount++;
                            rulerAmountMin = rulerAmountMin == "" ? info.Amount : BigHelper.Min(rulerAmountMin, info.Amount);
                        }
                    }

                    if ( (rulerCount < 25 && BigHelper.Greater(account.amount, "3000000", true))
                       || BigHelper.Greater(account.amount, rulerAmountMin, false))
                    {
                        httpRpc.OnBeRuler(httpMessage);
                    }

                }
                catch (Exception e)
                {
                }
            }
        }

    }

}





















