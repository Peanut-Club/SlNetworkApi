using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Serialization;

namespace SlNetworkApi.Requests
{
    public struct ResponseMessage : IHttpMessage
    {
        public bool IsSuccess;
        public object Response;
        public string Id;

        public ResponseMessage(string id, object msg, bool success)
            => (Id, Response, IsSuccess) = (id, msg, success);

        public void Deserialize(Deserializer deserializer)
        {
            Id = deserializer.GetString();
            IsSuccess = deserializer.GetBool();
            Response = deserializer.GetObject();
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Put(Id);
            serializer.Put(IsSuccess);
            serializer.PutObject(Response);
        }
    }
}