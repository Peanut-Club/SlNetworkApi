using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Serialization;

namespace SlNetworkApi.Server
{
    public struct ServerVerificationRequest : IHttpMessage
    {
        public string Token;

        public ServerVerificationRequest(string token)
            => Token = token;

        public void Deserialize(Deserializer deserializer)
            => Token = deserializer.GetString();

        public void Serialize(Serializer serializer)
            => serializer.Put(Token);
    }
}
