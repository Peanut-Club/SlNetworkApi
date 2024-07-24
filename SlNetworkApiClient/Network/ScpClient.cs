using CommonLib.Networking.Http.Transport;
using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using LabExtended.Core;
using LabExtended.API;

using System;
using System.Threading.Tasks;

using SlNetworkApi.Server;

namespace SlNetworkApiClient.Network
{
    public static class ScpClient
    {
        private static volatile bool _connecting = false;
        private static volatile bool _connected = false;

        private static string _url;
        private static TimeSpan? _delay;

        public static HttpTransportClient Client { get; set; }

        public static bool ReconnectClient => Plugin.Config.ReconnectClient;

        public static bool IsRunning => Client != null && Client.IsConnected;
        public static bool IsConnecting => _connecting;

        public static event Action OnServerConnected;
        public static event Action OnServerDisconnected;
        public static event Action<IHttpMessage> OnServerMessage;

        public static void Initialize(string url, TimeSpan? disconnectDelay = null)
        {
            if (_connecting)
                throw new InvalidOperationException($"The server is already connecting.");

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (disconnectDelay.HasValue && disconnectDelay.Value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(disconnectDelay));

            try
            {
                if (Client is null)
                {
                    Client = new HttpTransportClient();

                    Client.OnMessage += OnMessage;
                    Client.OnRejected += OnRejected;
                    Client.OnConnected += OnConnected;
                    Client.OnDisconnected += OnDisconnected;
                }

                _url = url;
                _delay = disconnectDelay;

                if (disconnectDelay.HasValue)
                    Client.DisconnectDelay = disconnectDelay.Value;

                Task.Run(async () =>
                {
                    _connecting = true;

                    while (!IsRunning)
                    {
                        try
                        {
                            Client.Connect(url);
                        }
                        catch (Exception ex)
                        {
                            ExLoader.Error("SCP HTTP", ex);
                        }

                        await Task.Delay(2000);
                    }

                    _connecting = false;
                    _connected = true;
                });
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        public static void Stop()
        {
            try
            {
                if (Client != null)
                {
                    Client.Disconnect();
                    Client = null;
                }
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        public static void Send(IHttpMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            ExLoader.Debug("SCP HTTP", $"Sending message: {message.GetType().FullName}");

            Client.Send(message);
        }

        private static ServerVerificationMessage HandleVerification(ServerVerificationRequest request)
        {
            ExLoader.Info("SCP HTTP", $"Received verification request, token &1{request.Token}&r");

            try
            {
                OnServerConnected?.Invoke();
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }

            return new ServerVerificationMessage(ExServer.Name, Plugin.Config.Id, ExServer.Port);
        }

        private static void OnRejected(RejectReason obj)
        {
            _connecting = false;
            ExLoader.Error("SCP HTTP", $"Connection rejected (&1{obj}&r)");
        }

        private static void OnMessage(IHttpMessage obj)
        {
            if (obj is null)
                return;

            ExLoader.Debug("SCP HTTP", $"Message received: {obj.GetType().FullName}");

            try
            {
                OnServerMessage?.Invoke(obj);
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        private static void OnDisconnected()
        {
            try
            {
                if (ReconnectClient && !_connecting)
                    Initialize(_url, _delay);
                else if (!ReconnectClient)
                    _connecting = false;

                if (!_connected)
                    ExLoader.Error("SCP HTTP", $"Client disconnected.");

                _connected = false;
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        private static void OnConnected()
        {
            try
            {
                ScpListener.OnConnected();
                ScpRequests.OnConnected();

                ScpRequests.Register<ServerVerificationRequest, ServerVerificationMessage>(HandleVerification);

                ExLoader.Info("SCP HTTP", $"Client connected to &3{_url}&r!");
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }
    }
}