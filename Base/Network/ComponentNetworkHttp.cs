using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ETModel
{
    public class HttpClient
    {
        private static void init_Request(ref System.Net.HttpWebRequest request)
        {
            request.Accept = "text/json,*/*;q=0.5";
            request.Headers.Add("Accept-Charset", "utf-8;q=0.7,*;q=0.7");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, x-gzip, identity; q=0.9");
            request.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
            request.Timeout = 8000;
        }
        public static string Get(string url)
        {
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                if (request != null)
                {
                    string retval = null;
                    init_Request(ref request);
                    using (var Response = request.GetResponse())
                    {
                        using (var reader = new System.IO.StreamReader(Response.GetResponseStream(), System.Text.Encoding.UTF8))
                        {
                            retval = reader.ReadToEnd();
                        }
                    }
                    request.Abort();
                    return retval;
                }
            }
            catch
            {

            }
            return null;
        }
        public static string Post(string url, string data)
        {
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                if (request != null)
                {
                    string retval = null;
                    init_Request(ref request);
                    request.Method = "POST";
                    request.ServicePoint.Expect100Continue = false;
                    request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                    var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(data);
                    request.ContentLength = bytes.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    using (var response = request.GetResponse())
                    {
                        using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                        {
                            retval = reader.ReadToEnd();
                        }
                    }
                    request.Abort();
                    return retval;
                }
            }
            catch
            {

            }
            return null;
        }
    }


    // 外网连接
    public class ComponentNetworkHttp : ComponentNetwork
    {
        public override void Awake(JToken jd = null)
        {
            base.Awake(jd);
            //TestRun();
        }

        public static async Task<HttpMessage> Query(string url, HttpMessage request)
        {
            string jsonModel = JsonHelper.ToJson(request.map);
            using (WebClient wc = new WebClient())
            {
                //发送到服务端并获得返回值
                var returnInfo = await wc.UploadDataTaskAsync(url, System.Text.Encoding.UTF8.GetBytes(jsonModel)).ConfigureAwait(false);
                var str = System.Text.Encoding.UTF8.GetString(returnInfo); //把服务端返回的信息转成字符串
                //var str = HttpClient.Post(url, jsonModel);
                HttpMessage response = new HttpMessage();
                response.map = JsonHelper.FromJson<Dictionary<string, string>>(str);
                return response;
            }
        }

        public static async Task<string> QueryString(string url, HttpMessage request)
        {
            string jsonModel = JsonHelper.ToJson(request.map);
            using (WebClient wc = new WebClient())
            {
                //发送到服务端并获得返回值
                var returnInfo = await wc.UploadDataTaskAsync(url, System.Text.Encoding.UTF8.GetBytes(jsonModel)).ConfigureAwait(false);
                var str = System.Text.Encoding.UTF8.GetString(returnInfo); //把服务端返回的信息转成字符串
                return str;
            }
        }


        public static HttpMessage QuerySync(string url, HttpMessage request, float timeout = 1)
        {
            string jsonModel = JsonHelper.ToJson(request.map);
            var timepass = new TimePass(timeout);
            using (WebClient wc = new WebClient())
            {
                //发送到服务端并获得返回值
                var awaiter = wc.UploadDataTaskAsync(url, System.Text.Encoding.UTF8.GetBytes(jsonModel)).ConfigureAwait(false).GetAwaiter();
                while (!awaiter.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                    if (timepass.IsPassOnce())
                        return null;
                }
                var str = System.Text.Encoding.UTF8.GetString(awaiter.GetResult()); //把服务端返回的信息转成字符串
                //var str = HttpClient.Post(url, jsonModel);
                HttpMessage response = new HttpMessage();
                response.map = JsonHelper.FromJson<Dictionary<string, string>>(str);
                return response;
            }
        }

        public static string QueryStringSync(string url, HttpMessage request, float timeout = 1)
        {
            string jsonModel = JsonHelper.ToJson(request.map);
            var timepass = new TimePass(timeout);
            using (WebClient wc = new WebClient())
            {
                //发送到服务端并获得返回值
                var awaiter = wc.UploadDataTaskAsync(url, System.Text.Encoding.UTF8.GetBytes(jsonModel)).ConfigureAwait(false).GetAwaiter();
                while (!awaiter.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                    if (timepass.IsPassOnce())
                        return null;
                }
                var str = System.Text.Encoding.UTF8.GetString(awaiter.GetResult()); //把服务端返回的信息转成字符串
                return str;
            }
        }

        //public async void TestRun()
        //{
        //    await Task.Delay(3000);

        //    HttpMessage request = new HttpMessage() ;
        //    request.map = new Dictionary<string, string>();
        //    request.map.Add("cmd123", "cmd123");
        //    request.map.Add("test123", "http请求测试");

        //    HttpMessage httpMessage = await Query("http://127.0.0.1:8089/", request);
        //    Log.Info(JsonHelper.ToJson(httpMessage.map));
        //}

        //[MessageMethod(NetOpcodeBase.HttpMessage)]
        //public static void OnHttpMessage(Session session, int opcode, object msg)
        //{
        //    HttpMessage httpMessage = msg as HttpMessage;

        //    Dictionary<string, string> map = httpMessage.map;
        //    map.Remove("cmd");
        //    map.Remove("test456");
        //    map.Remove("result");
        //    map.Add("cmd", "HttpMessage");
        //    map.Add("test456", "http返回测试");
        //    map.Add("result", "Success");

        //    httpMessage.result = JsonHelper.ToJson(map);
        //}

    }


}