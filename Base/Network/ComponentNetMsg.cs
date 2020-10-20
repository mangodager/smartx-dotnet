using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageAttribute : Attribute
    {
        public Type AttributeType { get; }

        public ushort Opcode { get; }
        public AppType appType { get; }

        public MessageAttribute(ushort opcode, AppType at)
        {
            this.AttributeType = this.GetType();
            this.Opcode  = opcode;
            this.appType = at;
        }
    }

    // 只对静态函数有效
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MessageMethodAttribute : Attribute
    {
        public Type AttributeType { get; }

        public ushort Opcode { get; }

        public MessageMethodAttribute(ushort opcode)
        {
            this.AttributeType = this.GetType();
            this.Opcode = opcode;
        }
    }

    // 回调函数封装
    public class NetworkMsgHandler
    {
        private int opcode = -1;
        private Action<Session, int, object> _fnCallback = null;
        public Action<Session, int, object> fnCallback { get { return _fnCallback; } }

        public NetworkMsgHandler(int op, Action<Session, int,object> fn)
        {
            opcode = op;
            _fnCallback = fn;
        }

        public void Call(Session session, int op, object msg)
        {
            if (_fnCallback != null)
                _fnCallback(session, op,msg);
        }
    }

    #region registerMsg
    public class ComponentNetMsg : Component
	{
        // 网络消息绑定数据结构
        private readonly DoubleMap<int, Type> opcodeTypes = new DoubleMap<int, Type>();

        // 网络消息内存池
        private readonly Dictionary<int, object> typeMessages = new Dictionary<int, object>();

        public override void Awake(JToken jd = null)
        {
            this.opcodeTypes.Clear();
            this.typeMessages.Clear();

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

                this.opcodeTypes.Add(messageAttribute.Opcode, type);
                this.typeMessages.Add(messageAttribute.Opcode, Activator.CreateInstance(type));
            }
            
            foreach (Type type in types)
            {
                object[] attrs = type.GetMethods();
                if (attrs.Length == 0)
                    continue;
                foreach (MethodInfo methodInfo in attrs)
                {
                    object[] attrs2 = methodInfo.GetCustomAttributes(typeof(MessageMethodAttribute), false);
                    if (attrs2.Length == 0)
                        continue;
                    MessageMethodAttribute attribute = attrs2[0] as MessageMethodAttribute;
                    if (attribute == null)
                        continue;

                    if(!methodInfo.IsStatic)
                    {
                        Log.Debug($"MessageMethod {type.Name}.{methodInfo.Name} 不是静态函数,将被忽略！");
                        continue;
                    }

                    Action<Session, int, object> dd = (Action<Session, int, object>)Delegate.CreateDelegate(typeof(Action<Session, int, object>), null, methodInfo);
                    this.registerMsg(attribute.Opcode, dd);
                }
            }

        }

        public int GetOpcode(Type type)
        {
            return this.opcodeTypes.GetKeyByValue(type);
        }

        public Type NetOpcode;
        public string GetOpcode(ushort opcode) { return Enum.ToObject(NetOpcode, opcode).ToString(); }

        public Type GetType(int opcode)
        {
            return this.opcodeTypes.GetValueByKey(opcode);
        }

        // 客户端为了0GC需要消息池，服务端消息需要跨协程不需要消息池
        public object GetInstance(int opcode)
        {
#if SERVER
            Type type = this.GetType(opcode);
            return Activator.CreateInstance(type);
#else
			return this.typeMessages[opcode];
#endif
        }


        protected Dictionary<int, List<NetworkMsgHandler>> m_MessageHandler = new Dictionary<int, List<NetworkMsgHandler>>();
        public void registerMsg(int opcode, Action<Session, int,object> fnCallback)
        {
            List<NetworkMsgHandler> list = null;
            m_MessageHandler.TryGetValue(opcode, out list);
            if (list == null)
            {
                list = new List<NetworkMsgHandler>();
                m_MessageHandler.Add(opcode, list);
            }
            list.Add(new NetworkMsgHandler(opcode, fnCallback));

        }


        public bool HandleMsg(Session session,int opcode, object msg)
        {
            bool b = false;

            List<NetworkMsgHandler> list = null;
            m_MessageHandler.TryGetValue(opcode, out list);
            if (list != null)
            {
                for(int i = 0; i < list.Count; i ++)
                {
                    list[i].Call(session, opcode, msg);
                    b = true;
                }
            }
            return b;
        }

        public SessionTask sessionTask = new SessionTask();
        public ETHotfix.MessageTask messageTask = new ETHotfix.MessageTask();


    }
    #endregion

}