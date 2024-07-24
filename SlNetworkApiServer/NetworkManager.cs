using CommonLib.Extensions;
using CommonLib.Logging;
using CommonLib.Networking.Http.Transport;
using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using SlNetworkApi.Server;
using SlNetworkApiServer.Servers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SlNetworkApiServer
{
    public static class NetworkManager
    {
        public static HttpTransportServer Listener { get; private set; }

        public static LogOutput Log { get; } = new LogOutput("Network Manager").Setup();
        public static Dictionary<string, ScpServer> Servers { get; } = new Dictionary<string, ScpServer>();

        public static bool IsRunning => Listener != null && Listener.IsListening;

        public static event Action<ScpServer> OnServerConnected;
        public static event Action<ScpServer, DisconnectReason> OnServerDisconnected;

        public static void Initialize(string ip, int port, TimeSpan? disconnectDelay = null)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip));

            if (port < 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            if (disconnectDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(disconnectDelay));

            Log.Info($"Initializing server at {ip}:{port} ..");

            if (Listener is null)
            {
                Listener = new HttpTransportServer();

                Listener.OnReady += OnReady;
                Listener.OnMessage += OnMessage;
                Listener.OnStopped += OnStopped;
                Listener.OnConnected += OnConnected;
                Listener.OnDisconnected += OnDisconnected;

                Log.Info("Listener instance created.");
            }
            else
            {
                Listener.Stop();
                Servers.Clear();
            }

            if (disconnectDelay.HasValue)
                Listener.DisconnectDelay = disconnectDelay.Value;

            Listener.Start(ip, port);
        }

        public static void Stop()
        {
            if (Listener != null)
            {
                Listener.Stop();

                Listener.OnReady -= OnReady;
                Listener.OnMessage -= OnMessage;
                Listener.OnStopped -= OnStopped;
                Listener.OnConnected -= OnConnected;
                Listener.OnDisconnected -= OnDisconnected;

                Listener = null;
            }

            Servers.Clear();
        }

        public static bool TryGetTokenServer(string token, out ScpServer server)
            => Servers.TryGetValue(token, out server);

        public static bool TryGetIdServer(string id, out ScpServer server)
            => (Servers.TryGetFirst(p => p.Value.Id != null && p.Value.Id == id, out var instance) ? server = instance.Value : server = null) != null;

        public static bool TryGetNameServer(string name, out ScpServer server)
            => (Servers.TryGetFirst(p => p.Value.Name != null && p.Value.Name == name, out var instance) ? server = instance.Value : server = null) != null;

        private static void OnConnected(HttpTransportPeer obj)
        {
            Log.Debug($"OnConnected {obj?.Token ?? "null"}");

            if (obj is null)
                return;

            var server = new ScpServer(obj.Token, obj);

            Servers[server.Token] = server;

            Task.Run(async () =>
            {
                await Task.Delay(250);

                Log.Debug($"Requesting ServerVerification");

                server.Requests.Get<ServerVerificationMessage>(new ServerVerificationRequest(obj.Token), msg =>
                {
                    server.Id = msg.Id;
                    server.Name = msg.Name;
                    server.Port = msg.Port;

                    OnServerConnected?.Invoke(server);

                    Log.Info($"Server connected: {server.Name} ({server.Id} - {server.Port})");
                });
            });

            Log.Info($"Peer connected: {obj.Token} ({obj.RemoteIp})");
        }

        private static void OnDisconnected(HttpTransportPeer arg1, DisconnectReason arg2)
        {
            Log.Debug($"OnDisconnected {arg1?.Token ?? "null"} {arg2}");

            if (arg1 is null)
                return;

            if (!Servers.TryGetValue(arg1.Token, out var server))
                return;

            server.InternalHandleDisconnect(arg2);

            OnServerDisconnected?.Invoke(server, arg2);

            Log.Warn($"Server {arg1.Token} ({arg1.RemoteIp}) has disconnected: {arg2}");
        }

        private static void OnMessage(HttpTransportPeer arg1, IHttpMessage arg2)
        {
            Log.Debug($"OnMessage {arg1?.Token ?? "null"} {arg2?.GetType().FullName ?? "null"}");

            if (arg2 is null)
                return;

            if (arg1 is null)
                return;

            if (!Servers.TryGetValue(arg1.Token, out var server))
                return;

            server.InternalHandleMessage(arg2);

            Log.Info($"Received message from server ({arg1.Token} - {arg1.RemoteIp}): {arg2.GetType().FullName}");
        }

        private static void OnReady()
            => Log.Info($"Listener initialized.");

        private static void OnStopped()
            => Log.Info("Listener stopped.");
    }
}