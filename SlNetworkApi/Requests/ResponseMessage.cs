using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System.IO;

namespace SlNetworkApi.Requests
{
    public struct ResponseMessage : INetworkMessage
    {
        public bool IsSuccess;
        public object Response;
        public string Id;

        public ResponseMessage(string id, object msg, bool success)
            => (Id, Response, IsSuccess) = (id, msg, success);

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadString();
            IsSuccess = reader.ReadBoolean();
            Response = reader.ReadObject();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(IsSuccess);
            writer.WriteObject(Response);
        }
    }
}