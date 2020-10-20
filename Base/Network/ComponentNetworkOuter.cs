using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ETModel
{
    // 外网连接
    public class ComponentNetworkOuter : ComponentNetwork
    {
        public ETTask<Session> Create(IPEndPoint ipEndPoint)
        {
            ETTaskCompletionSource<Session> t = new ETTaskCompletionSource<Session>();
            Session session = null;
            session = this.Create(ipEndPoint, (b) =>
            {
                t.SetResult(session);
            });            
            return t.Task;
        }


    }


}