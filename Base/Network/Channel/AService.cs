using System;
using System.Net;

namespace ETModel
{
	public enum NetworkProtocol
	{
		KCP,
		TCP,
		WebSocket,
		HttpSocket,
	}

	public abstract class AService
	{
        public bool CheckHearBeat   = false;   // 检查心跳
        public bool CheckKcpWaitsnd = true;    // 内网不需要检查发送队列

        public bool IsDispose;
        public virtual void Dispose(){ IsDispose = true; }

        public abstract AChannel GetChannel(long id);

		private Action<AChannel> acceptCallback;

		public event Action<AChannel> AcceptCallback
		{
			add
			{
				this.acceptCallback += value;
			}
			remove
			{
				this.acceptCallback -= value;
			}
		}
		
		protected void OnAccept(AChannel channel)
		{
			this.acceptCallback.Invoke(channel);
		}

		public abstract AChannel ConnectChannel(IPEndPoint ipEndPoint);
		
		public abstract AChannel ConnectChannel(string address);

		public abstract void Remove(long channelId);

		public abstract void Update();

        public abstract IPEndPoint GetEndPoint();
    }
}