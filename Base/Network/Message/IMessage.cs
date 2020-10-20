using Google.Protobuf;

namespace ETModel
{
#if SERVER
    public interface IMessage
    {
    }
#else
    public interface IMessage : Google.Protobuf.IMessage
    {
    }
#endif
    public interface IQuery : IActorMessage
    {
        int RpcId { get; set; }
    }

    public interface IResponse : IActorMessage
    {
        int Error { get; set; }
        string Message { get; set; }
        int RpcId { get; set; }
    }

    public interface IActorMessage : IMessage
    {
        long ActorId { get; set; }
    }

    public interface IActorQuery : IQuery
    {
    }

    public interface IActorResponse : IResponse
    {
    }

    public class ResponseMessage : IResponse
    {
        public int Error { get; set; }
        public string Message { get; set; }
        public int RpcId { get; set; }
        public long ActorId { get; set; }

        public void MergeFrom(CodedInputStream input)
        {
            throw new System.NotImplementedException();
        }

        public void WriteTo(CodedOutputStream output)
        {
            throw new System.NotImplementedException();
        }

        public int CalculateSize()
        {
            throw new System.NotImplementedException();
        }
    }
}