using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System.IO;

namespace SlNetworkApi.Requests
{
    public struct RequestMessage : INetworkMessage
    {
        public object Message;
        public string Id;

        public RequestMessage(string id, object msg)
            => (Id, Message) = (id, msg);

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadString();
            Message = reader.ReadObject();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.WriteObject(Message);
        }
    }
}