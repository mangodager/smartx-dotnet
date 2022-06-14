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
using System.IO;

namespace ETModel
{

    public class GetProduct : Component
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
        static public Dictionary<string, string> productInfo = new Dictionary<string, string>();//用来代表模块和函数用这个启动

        public async void Run()
        {
            await Task.Delay(2000);
            productInfo.Add("initTotal", "7000000000");
            try
            {
                var httpRpc = Entity.Root.Find("HttpRpc")?.GetComponent<HttpRpc>();
                if (httpRpc == null)
                    return;
                var consensus = Entity.Root.GetComponent<Consensus>();
                var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

                HttpMessage httpMessage = new HttpMessage();
                string address = Wallet.GetWallet().GetCurWallet().ToAddress();
                var data = JsonHelper.FromJson<Dictionary<string, string>>(File.ReadAllText("./getProductInfo.json"));
                long height = long.Parse(data["height"]);
                string posProduct = data["posTotal"];
                string powProduct = data["powTotal"];
                Console.WriteLine("posProduct:" + posProduct + "\n" + "height:" + height);
                //string consAddress = "";

                while (true)
                {
                    await Task.Delay(1000 * 30);
                    long.TryParse(Entity.Root.GetComponent<LevelDBStore>().Get("UndoHeight"), out long UndoHeight);
                    if (UndoHeight > height)
                    {
                        long getheight;
                        if (UndoHeight - height > 2000)
                        {
                            getheight = height + 2000;
                        }
                        else
                        {
                            getheight = UndoHeight;
                        }

                        for (long i = (height + 1); i <= getheight; i++)
                        {
                            Block block = BlockChainHelper.GetMcBlock(i);
                            long posNodeCount = block.linksblk.Count;
                            posProduct = BigHelper.Add(posProduct, BigHelper.Mul(posNodeCount.ToString(),Consensus.GetRewardRule_2022_06_06(i)));
                            powProduct = BigHelper.Add(powProduct , Consensus.GetReward(i).ToString());
                            height++;
                            if (height % 500 == 0)
                                Console.WriteLine("已经计算到：" + height + "高度了");
                        }
                        productInfo.Clear();
                        productInfo.Add("posTotal", posProduct.ToString());
                        productInfo.Add("powTotal", powProduct.ToString());
                        productInfo.Add("initTotal", "7000000000");
                        productInfo.Add("height", height.ToString());
                        File.WriteAllText("./getProductInfo.json", JsonHelper.ToJson(productInfo));
                    }
                }
            }
            catch (Exception )
            {
                
            }
        }

    }

}