using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System.IO;

namespace SlNetworkApi.Modules
{
    public struct InvokeMethodMessage : INetworkMessage
    {
        public ushort TypeCode;
        public ushort Code;

        public object[] Args;

        public int Id;

        public InvokeMethodMessage(ushort type, ushort code, object[] args, int id)
        {
            TypeCode = type;
            Code = code;
            Args = args;
            Id = id;
        }

        public void Read(BinaryReader reader)
        {
            TypeCode = reader.ReadUInt16();
            Code = reader.ReadUInt16();
            Id = reader.ReadInt32();
            Args = reader.ReadArray<object>();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(TypeCode);
            writer.Write(Code);
            writer.Write(Id);
            writer.WriteItems(Args);
        }
    }
}