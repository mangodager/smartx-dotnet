using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace ETModel
{
    public static partial class NetOpcodeBase
    {
        public const ushort HttpMessage = ushort.MaxValue-1;
        public const ushort A2A_SessionDestroy = ushort.MaxValue;
    }

    // 负责AChannel的发送接收消息(运行消息回调)
    public sealed class Session
	{
		private static int RpcId { get; set; }
		private AChannel channel;
        
		private readonly List<byte[]> byteses = new List<byte[]>() { new byte[1], new byte[4] };

		public ComponentNetwork network;

		public int Error
		{
			get
			{
				return this.channel.Error;
			}
			set
			{
				this.channel.Error = value;
			}
		}

        public bool IsConnect() { return !IsDispose && Error == 0; }

        public long Id;
        ComponentNetMsg componentNetMsg = null;

        public Session(ComponentNetwork aNetwork, AChannel aChannel, Action<bool> OnConnnect = null)
		{
            this.network = aNetwork;
            this.channel = aChannel;
			long id = this.Id = IdGenerater.GenerateId();

            channel.ClearAction<AChannel,int>(channel.errorCallback);
			channel.ErrorCallback += (c, e) =>
			{
                if(OnConnnect!=null)
                {
                    OnConnnect(false);
                    OnConnnect = null;
                }
                this.network.Remove(id);
            };

            channel.ClearAction<AChannel, int>(channel.errorCallback);
            channel.ConnnectCallback += (c) =>
            {
                if (OnConnnect != null)
                {
                    OnConnnect(true);
                    OnConnnect = null;
                }
            };

            channel.ClearAction<MemoryStream>(channel.readCallback);
            channel.ReadCallback += this.OnRead;

            componentNetMsg = this.network.entity.GetComponent<ComponentNetMsg>();

        }

        public bool IsDispose;
        public void Dispose()
		{
            bool IsDisposeLocal = this.IsDispose;
            this.IsDispose = true;
            if (IsDisposeLocal)
                return;

            long id = this.Id;
			
			this.channel.Dispose();

            // 
            this.network.Remove(id);

            // 离线消息
            this.network.entity.GetComponent<ComponentNetMsg>()?.HandleMsg(this, NetOpcodeBase.A2A_SessionDestroy, null);

        }

        public void Start()
		{
			this.channel.Start();
		}

		public IPEndPoint RemoteAddress
		{
			get
			{
				return this.channel.RemoteAddress;
			}
		}

		public ChannelType ChannelType
		{
			get
			{
				return this.channel.ChannelType;
			}
		}

		public MemoryStream Stream
		{
			get
			{
				return this.channel.Stream;
			}
		}

		public void OnRead(MemoryStream memoryStream)
		{
			try
			{
				this.Run(memoryStream);
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

		private void Run(MemoryStream memoryStream)
		{
			memoryStream.Seek(Packet.MessageIndex, SeekOrigin.Begin);
			byte flag = memoryStream.GetBuffer()[Packet.FlagIndex];
			int opcode = BitConverter.ToInt32(memoryStream.GetBuffer(), Packet.OpcodeIndex);
			
			object message;
			try
			{
				object instance = componentNetMsg.GetInstance(opcode);
				message = this.network.MessagePacker.DeserializeFrom(instance, memoryStream);

                //Log.Debug("Recv: " + componentNetMsg. GetType((ushort)opcode).Name); // yuxj debug
                if ( !componentNetMsg.HandleMsg (this, opcode, message) )
                {
                    network.dispatcher?.Handle(this, opcode, message);
                }
                componentNetMsg.sessionTask.Response(message);
                componentNetMsg.messageTask.Response(opcode, message);
            }
            catch (Exception e)
			{
				// 出现任何消息解析异常都要断开Session，防止客户端伪造消息
				Log.Error($"opcode: {opcode} {this.network.Count} {e} ");
				this.Error = ErrorCode.ERR_PacketParserError;
				this.network.Remove(this.Id);
				return;
			}
		}

		public void Send(IMessage message)
		{
			this.Send(0x00, message);
		}

		public void Send(byte flag, IMessage message)
		{
            ComponentNetMsg componentNetMsg = this.network.entity.GetComponent<ComponentNetMsg>();
            int opcode = componentNetMsg.GetOpcode(message.GetType());
			
			Send(flag, opcode, message);
		}
		
		public void Send(byte flag, int opcode, object message)
		{
			if (this.IsDispose)
			{
                return ;
				//throw new Exception("session已经被Dispose了");
			}

            //Log.Debug("Send: " + message.GetType().Name); // yuxj debug

            MemoryStream stream = this.Stream;
			
			stream.Seek(Packet.MessageIndex, SeekOrigin.Begin);
			stream.SetLength(Packet.MessageIndex);
			this.network.MessagePacker.SerializeTo(message, stream);
			stream.Seek(0, SeekOrigin.Begin);

			if (stream.Length > 4194304) // 4K
			{
				Log.Warning($"message too large: {stream.Length}, opcode: {opcode}");
				//return;
			}
			
			this.byteses[0][0] = flag;
			this.byteses[1].WriteTo(0, opcode);
			int index = 0;
			foreach (var bytes in this.byteses)
			{
				Array.Copy(bytes, 0, stream.GetBuffer(), index, bytes.Length);
				index += bytes.Length;
			}
            
//#if SERVER
//            // 如果是allserver，内部消息不走网络，直接转给session,方便调试时看到整体堆栈
//            if (this.network.appType == AppType.AllServer)
//            {
//                Session session = this.network.entity.GetComponent<ComponentNetworkInner>().Get(this.RemoteAddress);
//                session.Run(stream);
//                return;
//            }
//#endif

            this.Send(stream);
		}

		public void Send(MemoryStream stream)
		{
			channel.Send(stream);
		}

        public ETTask<IResponse> Query(IQuery request,float timeOut=5)
        {
            return componentNetMsg.sessionTask.Query(this,request, timeOut);
        }
        public ETTask<IResponse> Query(int RpcId, float timeOut = 5)
        {
            return componentNetMsg.sessionTask.Query(this, RpcId, timeOut);
        }

        public ETTask<IResponse> Query(IQuery request, CancellationToken cancellationToken)
        {
            return componentNetMsg.sessionTask.Query(this, request, cancellationToken);
        }

        public ETTask<IResponse> Query(long ActorId, int opcode,  float timeOut = 5)
        {
            return componentNetMsg.messageTask.Query(ActorId,opcode,  timeOut);
        }

        public void Reply(IQuery request, IResponse response)
        {
            if(request.ActorId!=0)
                response.ActorId = request.ActorId;
            response.RpcId   = request.RpcId;
            Send(response);
        }

    }
}