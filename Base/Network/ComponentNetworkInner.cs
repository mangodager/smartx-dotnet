using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ETModel
{
    // 内网连接
    public class ComponentNetworkInner : ComponentNetwork
    {
        readonly Dictionary<IPEndPoint, Session> adressSessions = new Dictionary<IPEndPoint, Session>();
        readonly Dictionary<IPEndPoint, int> serverIPs = new Dictionary<IPEndPoint, int>();

        public override void Remove(long id)
        {
            Session session = this.Get(id);
            if (session == null|| session.RemoteAddress==null)
            {
                return;
            }
            this.adressSessions.Remove(session.RemoteAddress);

            base.Remove(id);
            session.Dispose();
        }

        /// <summary>
        /// 从地址缓存中取Session,如果没有则创建一个新的Session,并且保存到地址缓存中
        /// </summary>
        public ETTask<Session> Get(IPEndPoint ipEndPoint, float timeOut = 5)
        {
            ETTaskCompletionSource<Session> t = new ETTaskCompletionSource<Session>();
            if (timeOut != 0)
                t.SetTimeOut(timeOut, null);

            Session session;
            if (this.adressSessions.TryGetValue(ipEndPoint, out session))
            {
                t.SetResult(session);
                return t.Task;
            }

            session = this.Create(ipEndPoint, (b) =>
            {
                t.SetResult(session);
            });

            this.adressSessions.Add(ipEndPoint, session);
            return t.Task;
        }

        public ETTask<Session> Get(AppType appType)
        {
            foreach (KeyValuePair<IPEndPoint, int> kv in serverIPs)
            {
                if( AppTypeHelper.Is(kv.Value, appType) )
                {
                    return Get(kv.Key);
                }
            }
            return ETTask.FromResult<Session>(null);
        }

        public void Send(IPEndPoint ipEndPoint,IMessage msg)
        {
            if (ipEndPoint == null)
                return;
            Session session;
            if (this.adressSessions.TryGetValue(ipEndPoint, out session))
            {
                session.Send(msg);
            }
            else
            {
                session = this.Create(ipEndPoint,(b)=>{ session.Send(msg);});
                this.adressSessions.Remove(ipEndPoint);
                this.adressSessions.Add(ipEndPoint, session);
            }
        }

        public void Send(AppType appType, IMessage msg)
        {
            Send(GetIPEndPoint(appType), msg);
        }

        public void Broadcast(IMessage msg)
        {
            foreach (IPEndPoint ipEndPoint in serverIPs.Keys)
            {
                Send(ipEndPoint, msg);
            }
        }
        public void Broadcast(IMessage msg,AppType appType)
        {
            foreach (KeyValuePair<IPEndPoint, int> kv in serverIPs)
            {
                if (AppTypeHelper.Is(kv.Value, appType))
                    Send(ipEndPoint, msg);
            }
        }

        public IPEndPoint GetIPEndPoint(AppType appType)
        {
            foreach (KeyValuePair<IPEndPoint, int> kv in serverIPs)
            {
                if (AppTypeHelper.Is(kv.Value, appType))
                {
                    return kv.Key;
                }
            }
            return null;
        }

        public void Add(IPEndPoint ipEndPoint,AppType appType)
        {
            int newAppType = (int)appType;
            foreach (KeyValuePair<IPEndPoint, int> kv in serverIPs)
            {
                if (kv.Key.Equals(ipEndPoint))
                {
                    newAppType = newAppType | kv.Value;
                    serverIPs.Remove(kv.Key);
                    break;
                }
            }
            serverIPs.Add(ipEndPoint, newAppType);
        }

    }


}