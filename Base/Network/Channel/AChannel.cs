using System;
using System.IO;
using System.Net;

namespace ETModel
{
	public enum ChannelType
	{
		Connect,
		Accept,
	}

	public abstract class AChannel
	{
		public ChannelType ChannelType { get; }

		protected AService service;

		public abstract MemoryStream Stream { get; }
		
		public int Error { get; set; }

		public IPEndPoint RemoteAddress { get; protected set; }

        public Action<AChannel, int> errorCallback;

		public event Action<AChannel, int> ErrorCallback
		{
			add
			{
				this.errorCallback += value;
			}
			remove
			{
				this.errorCallback -= value;
			}
		}
        protected Action<bool> connnectCallback;

        public event Action<bool> ConnnectCallback
        {
            add
            {
                this.connnectCallback += value;
            }
            remove
            {
                this.connnectCallback -= value;
            }
        }

        public Action<MemoryStream> readCallback;

		public event Action<MemoryStream> ReadCallback
		{
			add
			{
				this.readCallback += value;
			}
			remove
			{
				this.readCallback -= value;
			}
		}
		
		protected void OnRead(MemoryStream memoryStream)
		{
			this.readCallback.Invoke(memoryStream);
		}

		public void OnError(int e)
		{
			this.Error = e;
			this.errorCallback?.Invoke(this, e);
		}

		protected AChannel(AService service, ChannelType channelType)
		{
			this.Id = IdGenerater.GenerateId();
			this.ChannelType = channelType;
			this.service = service;
		}

		public abstract void Start();
		
		public abstract void Send(MemoryStream stream);

        public bool IsDispose = false;
        public long Id;

        public virtual void Dispose()
		{
            bool IsDisposeLocal = this.IsDispose;
            this.IsDispose = true;
            if (IsDisposeLocal)
				return;

            this.service.Remove(this.Id);
        }

        // 清空委托列表
        public void ClearAction<T>(Action<T> action)
        {
            if (action != null)
            {
                Delegate[] delArray = action.GetInvocationList();
                for (int i = 0; i < delArray.Length; i++)
                {
                    action -= delArray[i] as Action<T>;
                }
            }
        }
        // 清空委托列表
        public void ClearAction<T,K>(Action<T,K> action)
        {
            if (action != null)
            {
                Delegate[] delArray = action.GetInvocationList();
                for (int i = 0; i < delArray.Length; i++)
                {
                    action -= delArray[i] as Action<T,K>;
                }
            }
        }

    }
}