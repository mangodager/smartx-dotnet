using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ETModel
{
    /*
        "ComponentNetworkOuter": {
          "protocol": "KCP",
          "RemoteAddress": "122.10.161.138:9101",
          "address": "0:0",
          "CheckHearBeat": "true",
        } 
     */

    // 外网连接
    public class ComponentNetworkOuter : ComponentNetwork
    {
        readonly Dictionary<IPEndPoint, Session> adressSessions = new Dictionary<IPEndPoint, Session>();
        string RemoteAddressDns;
        IPEndPoint RemoteAddress;

        public override void Awake(JToken jd = null)
        {
            RemoteAddressDns = jd["RemoteAddress"]?.ToString();
            if (string.IsNullOrEmpty(RemoteAddressDns))
            {
                Log.Debug($"address not can be Empty");
            }
            RemoteAddress = NetworkHelper.ToIPEndPoint( NetworkHelper.DnsToIPEndPoint(RemoteAddressDns) );
            jd["address"] = "0:0";

            base.Awake(jd);
        }

        TimePass timePass = new TimePass(60*10);
        public override void Update()
        {
            if (timePass.IsPassSet())
            {
                RemoteAddress = NetworkHelper.ToIPEndPoint(NetworkHelper.DnsToIPEndPoint(RemoteAddressDns));
            }
            base.Update();
        }

        public override void Remove(long id)
        {
            Session session = this.Get(id);
            if (session == null || session.RemoteAddress == null)
            {
                return;
            }
            this.adressSessions.Remove(session.RemoteAddress);

            base.Remove(id);
            session.Dispose();
        }

        public void SetRemoteAddress(IPEndPoint _ipEndPoint)
        {
            RemoteAddress = _ipEndPoint;
        }

        /// <summary>
        /// 从地址缓存中取Session,如果没有则创建一个新的Session,并且保存到地址缓存中
        /// </summary>
        public ETTask<Session> GetSession(float timeOut = 5)
        {
            ETTaskCompletionSource<Session> t = new ETTaskCompletionSource<Session>();
            if (timeOut != 0)
                t.SetTimeOut(timeOut, null);

            Session session;
            if (this.adressSessions.TryGetValue(RemoteAddress, out session))
            {
                t.SetResult(session);
                return t.Task;
            }

            session = this.Create(RemoteAddress, (b) =>
            {
                t.SetResult(session);
            });

            this.adressSessions.Add(RemoteAddress, session);
            return t.Task;
        }

        public void Send(IMessage msg)
        {
            if (RemoteAddress == null)
                return;
            Session session;
            if (this.adressSessions.TryGetValue(RemoteAddress, out session))
            {
                session.Send(msg);
            }
            else
            {
                session = this.Create(RemoteAddress, (b) => { session.Send(msg); });
                this.adressSessions.Remove(RemoteAddress);
                this.adressSessions.Add(RemoteAddress, session);
            }
        }

        public async ETTask<IResponse> Query(IQuery request, float timeOut = 5)
        {
            var session = await GetSession();
            return await session.Query(request, timeOut);
        }


    }


}