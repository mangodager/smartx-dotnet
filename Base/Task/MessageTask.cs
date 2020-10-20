using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;
using ETModel;

namespace ETHotfix
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

    public class MessageTask
    {
        public class QueryData
        {
            public int opcode;
            public long ActorId;
            public Action<IResponse> callback;

        }

        private readonly Dictionary<int, QueryData> requestCallback = new Dictionary<int, QueryData>();


        public ETTask<IResponse> Query(long ActorId, int opcode, float timeOut)
        {
            var tcs = new ETTaskCompletionSource<IResponse>();

            if (timeOut!=0)
                tcs.SetTimeOut(timeOut, null);

            QueryData data = new QueryData();
            data.ActorId = ActorId;
            data.opcode  = opcode;
            
            data.callback = (response) =>
            {
                try
                {
                    tcs.SetResult(response);
                }
                catch (Exception e)
                {
                    tcs.SetException(new Exception($"Message Error: {data.opcode}", e));
                }
            };

            this.requestCallback[data.opcode] = data;

            return tcs.Task;
        }

        public void Response(int opcode, object message)
        {
            IResponse response = message as IResponse;
            if (response == null)
                return;

            QueryData data;
            if (!this.requestCallback.TryGetValue(opcode, out data))
            {
                return;
            }

            if(data.ActorId!=0&&data.ActorId != response.ActorId)
                return ;

            this.requestCallback.Remove(opcode);
            data.callback(response);

        }

        public void Dispose()
        {
            requestCallback.Clear();
        }

    }

}