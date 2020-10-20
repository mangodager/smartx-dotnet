using System.Collections.Generic;
using System.Reflection;
using System;
using System.Net;
using System.Threading;

namespace ETModel
{

    /// <summary>
    /// 消息分发组件,GatwWay才用到
    /// </summary>
    public class IMessageDispatcher
    {
        public class DataList
        {
            public AppType    appType;
            public IPEndPoint ipEndPoint;
            public Session    session;
        }

        public readonly Dictionary<int, AppType > Handlers = new Dictionary<int, AppType>();
        public ComponentNetworkInner networkInner = null;
        public ComponentNetworkOuter networkOuter = null;

        public virtual void Load(ComponentNetworkOuter outer, ComponentNetworkInner inner)
        {
            networkOuter = outer;
            networkInner = inner;

            Handlers.Clear();
            // 网络消息绑定数据结构
            List<Type> types = AssemblyHelper.GetTypes();
            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(MessageAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                MessageAttribute messageAttribute = attrs[0] as MessageAttribute;
                if (messageAttribute == null)
                {
                    continue;
                }

                Handlers.Add(messageAttribute.Opcode, messageAttribute.appType);
            }
        }



        // 分发给消息定义的服务器
        public virtual void Handle(Session session, int opcode, object msg)
        {

        }

    }
}