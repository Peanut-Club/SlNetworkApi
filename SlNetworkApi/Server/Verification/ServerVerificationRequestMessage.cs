using CommonLib.Networking.Interfaces;

using System.IO;

namespace SlNetworkApi.Server.Verification
{
    public struct ServerVerificationRequestMessage : INetworkMessage
    {
        public string Token;

        public ServerVerificationRequestMessage(string token)
            => Token = token;

        public void Read(BinaryReader reader)
            => Token = reader.ReadString();

        public void Write(BinaryWriter writer)
            => writer.Write(Token);
    }
}
