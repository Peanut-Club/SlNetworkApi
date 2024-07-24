using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Serialization;

namespace SlNetworkApi.Requests
{
    public struct RequestMessage : IHttpMessage
    {
        public object Message;
        public string Id;

        public RequestMessage(string id, object msg)
            => (Id, Message) = (id, msg);

        public void Deserialize(Deserializer deserializer)
        {
            Id = deserializer.GetString();
            Message = deserializer.GetObject();
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Put(Id);
            serializer.PutObject(Message);
        }
    }
}