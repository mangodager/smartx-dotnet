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

    public class AutoGetAccount : Component
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
            var consensus = Entity.Root.GetComponent<Consensus>();
            var luaVMEnv = Entity.Root.GetComponent<LuaVMEnv>();

            HttpMessage httpMessage = new HttpMessage();
            string address = Wallet.GetWallet().GetCurWallet().ToAddress();
            //string consAddress = "";

            while (true)
            {
                await Task.Delay(60000);
               
                if (DateTime.Now.Hour >= 12   ) 
                {
                    var dayAccount = "account" + DateTime.Now.ToString("yyyyMMdd");
                    FileInfo fi = new FileInfo("./" + dayAccount + ".csv");
                    bool exists = fi.Exists;
                    if (exists == false)
                    {
                        var height = consensus.transferHeight;
                        LevelDBStore.test_ergodic2(height, dayAccount);
                        Console.WriteLine("导出完成");
                    }
                    
                }
                
            }
        }

    }

}





















