using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System.IO;

namespace SlNetworkApi.Modules
{
    public struct SetPropertyMessage : INetworkMessage
    {
        public ushort Type;
        public ushort Code;
        public object Value;

        public SetPropertyMessage(ushort type, ushort code, object value)
        {
            Type = type;
            Code = code;
            Value = value;
        }

        public void Read(BinaryReader reader)
        {
            Type = reader.ReadUInt16();
            Code = reader.ReadUInt16();
            Value = reader.ReadObject();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Code);
            writer.WriteObject(Value);
        }
    }
}