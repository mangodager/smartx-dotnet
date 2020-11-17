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
        HttpPool httpPool = null;

        public override void Awake(JToken jd = null)
        {
            address = jd["address"]?.ToString();
            number  = jd["number"]?.ToString();
            poolUrl = jd["poolUrl"]?.ToString();
            thread  = 0;
            intervalTime = 100;

            changeCallback += OnMiningChange;
        }

        public override void Start()
        {
            httpPool = Entity.Root.GetComponentInChild<HttpPool>();

            Run();
        }

        public void OnMiningChange()
        {
            httpPool?.SetMinging(height, hashmining, poolPower);
        }
    }


}
