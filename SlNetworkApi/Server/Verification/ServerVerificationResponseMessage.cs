using CommonLib.Networking.Interfaces;

using System.IO;

namespace SlNetworkApi.Server.Verification
{
    public struct ServerVerificationResponseMessage : INetworkMessage
    {
        public string Name;
        public string Id;

        public int Port;

        public ServerVerificationResponseMessage(string name, string id, int port)
            => (Name, Id, Port) = (name, id, port);

        public void Read(BinaryReader reader)
        {
            Name = reader.ReadString();
            Id = reader.ReadString();

            Port = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Id);
            writer.Write(Port);
        }
    }
}