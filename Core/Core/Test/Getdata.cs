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

    public class Getdata : Component
    {
        public bool bRun = true;
        public JArray array = null;
        public string ErgodiList = null;

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
                Run1();
            }
        }
        public async void Run()
        {
            while (true)
            {
                string values = null;
                string filePath = @".\ErgodiList.csv";
                StreamReader reader = null;
                if (File.Exists(filePath))
                {
                    try
                    {
                        reader = new StreamReader(File.OpenRead(filePath));
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadToEnd();
                            values = line.Replace("\n", ",");
                            ErgodiList = values;
                        }
                    }
                    catch { 
                    
                    }
                }
                if (values==null)
                {
                    values = ErgodiList;
                }
                array = LevelDBExport.TraversalData(values);
                await Task.Delay(10000 * 8640);
                //await Task.Delay(1000 * 3600);
            }
        }
        public async void Run1()
        {
            while (true)
            {
                await Task.Delay(10000 * 8640);
                LevelDBExport.ErgodicPledgeContract();
            }
        }
    }

}