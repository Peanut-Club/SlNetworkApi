using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System.IO;

namespace SlNetworkApi.Modules
{
    public struct InvokeMethodResult : INetworkMessage
    {
        public int Id;

        public object Value;
        public string Exception;

        public InvokeMethodResult(int id, object value, string exception)
        {
            Id = id;
            Value = value;
            Exception = exception;
        }

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            Value = reader.ReadObject();
            Exception = reader.ReadString();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.WriteObject(Value);
            writer.Write(Exception);
        }
    }
}
