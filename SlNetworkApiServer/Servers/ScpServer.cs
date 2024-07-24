using CommonLib.Networking.Http.Transport;
using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using System;
using System.Net;

namespace SlNetworkApiServer.Servers
{
    public class ScpServer
    {
        private readonly HttpTransportPeer _peer;

        public string Token { get; }

        public int Port { get; internal set; }

        public string Id { get; internal set; }
        public string Name { get; internal set; }

        public TimeSpan Latency => _peer.Latency;
        public IPEndPoint Address => _peer.RemoteIp;

        public NetworkListener Listener { get; }
        public NetworkRequests Requests { get; }

        public event Action<IHttpMessage> OnMessage;
        public event Action<DisconnectReason> OnDisconnected;

        public ScpServer(string token, HttpTransportPeer peer)
            => (Token, Listener, Requests, _peer) = (token, new NetworkListener(this), new NetworkRequests(this), peer);

        public void Send(IHttpMessage message)
            => _peer.Send(message);

        public void Disconnect(DisconnectReason reason)
            => _peer.Disconnect(reason);

        internal void InternalHandleDisconnect(DisconnectReason reason)
        {
            OnDisconnected?.Invoke(reason);
        }

        internal void InternalHandleMessage(IHttpMessage httpMessage)
        {
            OnMessage?.Invoke(httpMessage);
        }
    }
}