using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    // 网络层Component
    public abstract class ComponentNetwork : Component
    {
		private AService Service;

		private readonly Dictionary<long, Session> sessions = new Dictionary<long, Session>();

		public IMessagePacker MessagePacker { get; set; }

        public int appType = 0;

        public IMessageDispatcher dispatcher = null;

        public IPEndPoint ipEndPoint;

        NetworkProtocol protocol;
        string address;

        public override void Awake(JToken jd = null)
		{
            if(jd==null)
                return;

            if(jd.Parent != null && jd.Parent.Parent != null && jd.Parent.Parent["appType"] != null)
            {
                string[] array = jd.Parent.Parent["appType"].ToString().Replace(" ","").Split('|');
                for(int i = 0 ;i < array.Length; i++)
                {
                    if(array[i]!="")
                    {
                        appType |= (int)Enum.Parse(typeof(AppType), array[i]);
                    }
                }
            }

            protocol = NetworkProtocol.TCP;

            if (jd["protocol"]!=null)
            {
                Enum.TryParse<NetworkProtocol>(jd["protocol"].ToString(), out protocol);
            }

            address = jd["address"]?.ToString();
            if (address != null && address != "" && protocol != NetworkProtocol.HttpSocket) 
            {
                ipEndPoint = NetworkHelper.ToIPEndPoint(address);
            }
            else 
            {
                ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            }

            if (jd["MessagePacker"]?.ToString() == "ProtobufPacker")
            {
                MessagePacker = new ProtobufPacker();
            }
            else
            {
                MessagePacker = new MongoPacker();
            }

            try
            {
                switch (protocol)
				{
					case NetworkProtocol.KCP:
						this.Service = new KService(ipEndPoint, this.OnAccept);
						break;
					case NetworkProtocol.TCP:
						this.Service = new TService(ipEndPoint, this.OnAccept);
						break;
                    case NetworkProtocol.WebSocket:
                        this.Service = new WService(address.Split(';'), this.OnAccept);
                        break;
					case NetworkProtocol.HttpSocket:
						string[] prefixs = address.Split(';');
						this.Service = new HttpService(prefixs[0], this.OnAccept);
						break;
                }
			}
			catch (Exception e)
			{
                Log.Debug($"创建KService失败!{e.Message}");
				//throw new Exception($"NetworkComponent Awake Error {address}", e);
			}

            ipEndPoint = this.Service.GetEndPoint();
            IdGenerater.AppId = ipEndPoint.Port;

            if (jd["CheckHearBeat"] != null && this.Service != null)
                this.Service.CheckHearBeat = jd["CheckHearBeat"].ToObject<bool>();

            if (jd["CheckKcpWaitsnd"] != null && this.Service != null)
                this.Service.CheckKcpWaitsnd = jd["CheckKcpWaitsnd"].ToObject<bool>();

        }

        public int Count
		{
			get { return this.sessions.Count; }
		}

		public void OnAccept(AChannel channel)
		{
			Session session = new Session(this, channel);
            this.sessions.Add(session.Id, session);
			session.Start();
		}

		public virtual void Remove(long id)
		{
			Session session;
			if (!this.sessions.TryGetValue(id, out session))
			{
				return;
			}
			this.sessions.Remove(id);
			session.Dispose();
		}

		public Session Get(long id)
		{
			Session session;
			this.sessions.TryGetValue(id, out session);
			return session;
		}

		/// <summary>
		/// 创建一个新Session
		/// </summary>
		public Session Create(IPEndPoint ipEndPoint, Action<bool> OnConnnect = null)
		{
            if(this.Service==null)
                return null;
			AChannel channel = this.Service.ConnectChannel(ipEndPoint);
			Session session = new Session(this, channel, OnConnnect);
            this.sessions.Add(session.Id, session);
			session.Start();
			return session;
		}
		
		/// <summary>
		/// 创建一个新Session
		/// </summary>
		public Session Create(string address)
		{
			AChannel channel = this.Service.ConnectChannel(address);
			Session session = new Session(this, channel);
			this.sessions.Add(session.Id, session);
			session.Start();
			return session;
		}

		public override void Update()
		{
			if (this.Service == null)
			{
				return;
			}
			this.Service.Update();
		}

        protected override void OnDestroy()
		{
			foreach (Session session in this.sessions.Values.ToArray())
			{
				session.Dispose();
			}

			this.Service?.Dispose();
            this.Service = null;

        }
	}
}