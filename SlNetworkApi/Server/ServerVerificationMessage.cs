using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Serialization;

namespace SlNetworkApi.Server
{
    public struct ServerVerificationMessage : IHttpMessage
    {
        public string Name;
        public string Id;

        public int Port;

        public ServerVerificationMessage(string name, string id, int port)
            => (Name, Id, Port) = (name, id, port);

        public void Deserialize(Deserializer deserializer)
        {
            Name = deserializer.GetString();
            Id = deserializer.GetString();

            Port = deserializer.GetInt32();
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Put(Name);
            serializer.Put(Id);
            serializer.Put(Port);
        }
    }
}