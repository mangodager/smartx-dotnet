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

    public class TokenPocket : Component
    {
        ComponentNetworkHttp networkHttp;
        string explorelUrl = "https://explorel2.smartx.one/api";

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            componentNetMsg.registerMsg(NetOpcodeBase.HttpMessage, OnHttpMessage);

            explorelUrl = jd["explorelUrl"].ToString();

        }

        public override void Start()
        {
            networkHttp = this.entity.GetComponent<ComponentNetworkHttp>();
            Log.Info($"TokenPocket http://{networkHttp.ipEndPoint}/");
        }

        public void OnHttpMessage(Session session, int opcode, object msg)
        {
            HttpMessage httpMessage = msg as HttpMessage;
            if (httpMessage == null || httpMessage.request == null || networkHttp == null 
             || httpMessage.request.LocalEndPoint.ToString() != networkHttp.ipEndPoint.ToString())
                return;

            string cmd = httpMessage.map["cmd"].ToLower();
            switch (cmd)
            {
                case "v1/transaction_action/universal_list":
                    universal_list(httpMessage);
                    break;
                case "v1/transaction_action/universal":
                    universal(httpMessage);
                    break;
                default:
                    break;
            }
        }


        public void universal_list(HttpMessage httpMessage)
        {
            HttpRpc.GetParam(httpMessage, "ns", "", out string ns);
            HttpRpc.GetParam(httpMessage, "chain_id", "", out string chain_id);
            HttpRpc.GetParam(httpMessage, "blockchain_id", "", out string blockchain_id);
            HttpRpc.GetParam(httpMessage, "search", "",  out string search);
            HttpRpc.GetParam(httpMessage, "address", "", out string address);
            HttpRpc.GetParam(httpMessage, "contract_address", "", out string contract_address);
            HttpRpc.GetParam(httpMessage, "token_id", "", out string token_id);
            HttpRpc.GetParam(httpMessage, "page", "", out string page);
            HttpRpc.GetParam(httpMessage, "count", "", out string count);
            HttpRpc.GetParam(httpMessage, "sort", "", out string sort);
            HttpRpc.GetParam(httpMessage, "fork", "", out string fork);
            HttpRpc.GetParam(httpMessage, "type", "", out string type);

            HttpMessage quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();
            quest.map.Add("module", "transaction");
            quest.map.Add("action", "gettxinfo");
            quest.map.Add("address ", address);
            quest.map.Add("sort", sort);
            quest.map.Add("page", page);
            quest.map.Add("offset", count);
            var awaiter = ComponentNetworkHttp.QueryString(explorelUrl, quest).GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            var result = JsonHelper.FromJson<List<Dictionary<string, object>>>(awaiter.GetResult());


            List<Dictionary<string, object>> myRes = new List<Dictionary<string, object>>();

            for (int ii = 0; ii < result.Count; ii++)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                var jd = result[ii];

                data.Add("decimal", 18);
                data.Add("LogIndex", -1);
                data.Add("BlockNumber", long.Parse(jd["blockNumber"].ToString()));
                data.Add("Hash", jd["hash"].ToString());
                data.Add("Nonce", jd["nonce"]?.ToString());
                data.Add("From", jd["from"].ToString());
                data.Add("To", jd["to"].ToString());
                data.Add("Value", jd["value"].ToString());
                data.Add("Type", 0); // 1 表示是代币 0 表示Eth(原生币)
                data.Add("Timestamp", long.Parse(jd["timeStamp"].ToString()));
                data.Add("Input", jd["input"].ToString());
                data.Add("InputStatus", jd["input"].ToString() == "0x" ? 0 : 1);
                data.Add("Status", int.Parse(jd["status"].ToString()));
                data.Add("Gas", jd["gasLimit"].ToString());
                data.Add("GasPrice", jd["gasPrice"].ToString());
                data.Add("UsedGas", jd["gasUsed"].ToString());

                myRes.Add(data);
            }

            httpMessage.result = JsonHelper.ToJson(myRes);
        }

        public void universal(HttpMessage httpMessage)
        {
            HttpRpc.GetParam(httpMessage, "ns", "", out string ns);
            HttpRpc.GetParam(httpMessage, "chain_id", "", out string chain_id);
            HttpRpc.GetParam(httpMessage, "blockchain_id", "", out string blockchain_id);
            HttpRpc.GetParam(httpMessage, "block_hash", "", out string block_hash);
            HttpRpc.GetParam(httpMessage, "hash", "", out string hash);
            HttpRpc.GetParam(httpMessage, "log_index", "", out string log_index);
            HttpRpc.GetParam(httpMessage, "internal_index", "", out string internal_index);


            HttpMessage quest = new HttpMessage();
            quest.map = new Dictionary<string, string>();
            quest.map.Add("module", "transaction");
            quest.map.Add("action", "gettxinfo");
            quest.map.Add("txhash", hash);
            var awaiter = ComponentNetworkHttp.QueryString(explorelUrl, quest).GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            JToken jd = JToken.Parse(awaiter.GetResult());

            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add("decimal", 18);
            data.Add("LogIndex", -1);
            data.Add("BlockNumber", long.Parse(jd["blockNumber"].ToString()));
            data.Add("Hash", jd["hash"].ToString());
            data.Add("Nonce", jd["nonce"]?.ToString());
            data.Add("From", jd["from"].ToString());
            data.Add("To", jd["to"].ToString());
            data.Add("Value", jd["value"].ToString());
            data.Add("Type", 0); // 1 表示是代币 0 表示Eth(原生币)
            data.Add("Timestamp", long.Parse(jd["timeStamp"].ToString()));
            data.Add("Input", jd["input"].ToString());
            data.Add("InputStatus", jd["input"].ToString()=="0x"?0:1 );
            data.Add("Status", int.Parse(jd["status"].ToString()));
            data.Add("Gas", jd["gasLimit"].ToString());
            data.Add("GasPrice", jd["gasPrice"].ToString());
            data.Add("UsedGas", jd["gasUsed"].ToString());


            Dictionary<string, object> res  = new Dictionary<string, object>();
            res.Add("message", "success");
            res.Add("result", "0");
            res.Add("data", data );

            httpMessage.result = JsonHelper.ToJson(res);
        }



    }


}



















