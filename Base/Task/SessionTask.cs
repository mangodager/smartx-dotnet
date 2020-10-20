using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ETModel
{
    /*
     * 示例：
     * async function
     * IResponse response = await TaskMgr.Request("EVENT_NAME123")  // 等待下面的代码执行后继续执行
     * 
     * 
     * // 另一个业务函数中
     * async Response("EVENT_NAME123",response)  // 事件完成时执行
     * 
    */

    public class SessionTask
    {
        private readonly Dictionary<int, Action<IResponse>> requestCallback = new Dictionary<int, Action<IResponse>>();

        int m_rpcId = 1111;

        public int GetRpcID() 
        {
            return ((int)IdGenerater.AppId << 24) + (++m_rpcId);
        }

        public ETTask<IResponse> Query(Session session, IQuery request, float timeOut)
        {
            var tcs = new ETTaskCompletionSource<IResponse>();
            if(request.RpcId==0)
                request.RpcId = GetRpcID();
            int RpcId =  request.RpcId;

            if (timeOut!=0)
                tcs.SetTimeOut(timeOut, null);

            this.requestCallback[ RpcId ] = (response) =>
            {
                try
                {
                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {RpcId}", e));
                }
            };
            session.Send(0x1, request);
            return tcs.Task;
        }

        public ETTask<IResponse> Query(Session session, int RpcId, float timeOut)
        {
            var tcs = new ETTaskCompletionSource<IResponse>();

            if (timeOut!=0)
                tcs.SetTimeOut(timeOut, null);

            this.requestCallback[ RpcId ] = (response) =>
            {
                try
                {
                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {RpcId}", e));
                }
            };
            return tcs.Task;
        }

        public ETTask<IResponse> Query(Session session, IQuery request,CancellationToken cancellationToken)
        {
            var tcs = new ETTaskCompletionSource<IResponse>();
            if (request.RpcId == 0)
                request.RpcId = GetRpcID();
            int RpcId = request.RpcId;

            this.requestCallback[ RpcId ] = (response) =>
            {
                try
                {
                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Rpc Error: {RpcId}", e));
                }
            };
            cancellationToken.Register(() => { this.requestCallback.Remove(RpcId); });

            session.Send(0x1, request);
            return tcs.Task;
        }

        public void Response(object message)
        {
            IResponse response = message as IResponse;
            if(response==null)
                return ;

            Action<IResponse> action;
            if (!this.requestCallback.TryGetValue(response.RpcId, out action))
            {
                return;
            }
            this.requestCallback.Remove(response.RpcId);
            action(response);
            
        }

        public void Dispose()
        {
            requestCallback.Clear();
        }

    }

}