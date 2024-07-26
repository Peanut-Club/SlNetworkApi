using CommonLib;
using CommonLib.Logging;
using CommonLib.Networking.Interfaces;
using CommonLib.Networking.Http.Transport;
using CommonLib.Networking.Http.Transport.Enums;

using LabExtended.Core.Hooking;
using LabExtended.Core;

using LabExtended.API;
using LabExtended.Attributes;
using LabExtended.Extensions;

using System;
using System.Threading.Tasks;

using SlNetworkApi.Modules;
using SlNetworkApi.Requests;
using SlNetworkApi.Server.Verification;

using SlNetworkApiClient.Events;

using System.Collections.Generic;

namespace SlNetworkApiClient.Network
{
    public static class ScpClient
    {
        private static volatile bool _connecting = false;
        private static volatile string _url = null;

        private static volatile HttpTransportClient _client;
        private static volatile Dictionary<Type, ScpModule> _modules = new Dictionary<Type, ScpModule>();

        public static HttpTransportClient Client => _client;

        public static bool IsRunning => Client != null && Client.IsConnected;
        public static bool IsConnecting => _connecting;

        public static void Initialize(string url)
        {
            if (_connecting || (Client != null && Client.IsConnecting))
                return;

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                ExLoader.Info("SCP HTTP", $"Connecting to: &1{url}&r");

                _connecting = true;

                if (_client is null)
                {
                    _client = new HttpTransportClient();

                    _client.OnError += OnError;
                    _client.OnMessage += OnMessage;
                    _client.OnRejected += OnRejected;
                    _client.OnConnected += OnConnected;
                    _client.OnDisconnected += OnDisconnect;

                    ExLoader.Info("SCP HTTP", "Events registered");
                }

                _client.Disconnect();
                _url = url;

                Task.Run(async () =>
                {
                    while (!IsRunning)
                    {
                        try
                        {
                            _connecting = true;
                            _client.Connect(url);
                        }
                        catch (Exception ex)
                        {
                            ExLoader.Error("SCP HTTP", ex);
                        }

                        await Task.Delay(2000);
                    }

                    _connecting = false;
                });
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        public static void Disconnect()
        {
            Client?.Disconnect();
        }

        public static void Stop()
        {
            if (Client != null)
            {
                if (Client.IsConnected && !Client.IsConnecting)
                    OnDisconnected(false);

                Client.Stop();

                Client.OnError -= OnError;
                Client.OnMessage -= OnMessage;
                Client.OnRejected -= OnRejected;
                Client.OnConnected -= OnConnected;
                Client.OnDisconnected -= OnDisconnect;

                _connecting = false;
            }
        }

        public static void Send(INetworkMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            ExLoader.Debug("SCP HTTP", $"Sending message: {message.GetType().FullName}");

            Client.Send(message);
        }

        public static T GetModule<T>() where T : ScpModule
            => _modules.TryGetValue(typeof(T), out var module) ? (T)module : null;

        public static T AddModule<T>() where T : ScpModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
                return (T)module;

            module = typeof(T).Construct<ScpModule>();
            module.Start();

            _modules[typeof(T)] = module;
            return (T)module;
        }

        public static void AddModule(ScpModule module)
        {
            if (module is null)
                throw new ArgumentNullException(nameof(module));

            _modules[module.GetType()] = module;
        }

        public static void RemoveModule(ScpModule module)
        {
            if (module is null)
                throw new ArgumentNullException(nameof(module));

            _modules.Remove(module.GetType());
        }

        public static void RemoveModule<T>() where T : class
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                module.Stop();
            }

            _modules.Remove(typeof(T));
        }

        public static void RemoveModules()
        {
            foreach (var module in _modules.Values)
            {
                module.Stop();
            }

            _modules.Clear();
        }

        private static object HandleVerification(object msg)
        {
            if (msg is null || !msg.Is<ServerVerificationRequestMessage>(out var request))
                return null;

            ExLoader.Info("SCP HTTP", $"Received verification request, token &1{request.Token}&r");

            HookRunner.RunEvent(new ClientConnectedArgs());

            return new ServerVerificationResponseMessage(ExServer.Name.RemoveHtmlTags(), Plugin.Config.Id, ExServer.Port);
        }

        private static void OnRejected(RejectReason obj)
        {
            _connecting = false;

            HookRunner.RunEvent(new ClientRejectedArgs(obj));

            ExLoader.Error("SCP HTTP", $"Connection rejected (&1{obj}&r)");
        }

        private static void OnMessage(INetworkMessage obj)
        {
            if (obj is null)
                return;

            ExLoader.Debug("SCP HTTP", $"Message received: {obj.GetType().FullName}");

            if (obj is ServerVerificationRequestMessage msg)
            {
                ExLoader.Info("SCP HTTP", $"Received server verification request (token &1{msg.Token}&r)");

                Send(new ServerVerificationResponseMessage(ExServer.Name.RemoveHtmlTags(), Plugin.Config.Id, ExServer.Port));
                return;
            }

            try
            {
                foreach (var module in _modules.Values)
                    module.OnMessage(obj);

                HookRunner.RunEvent(new ClientMessageArgs(obj));
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        private static void OnDisconnect()
            => OnDisconnected(true);

        private static void OnDisconnected(bool reconnect)
        {
            try
            {
                RemoveModules();

                var disconnectedArgs = new ClientDisconnectedArgs(!_connecting && reconnect && (Client is null || !Client.IsConnecting));

                HookRunner.RunEvent(disconnectedArgs);

                if (disconnectedArgs.ShouldReconnect)
                    Initialize(_url);

                ExLoader.Error("SCP HTTP", $"Client disconnected.");
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
                HookRunner.RunEvent(new ClientConnectedArgs());
                ExLoader.Info("SCP HTTP", $"Client connected to &3{_url}&r!");
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP HTTP", ex);
            }
        }

        private static void OnError(Exception obj)
        {
            ExLoader.Error("SCP HTTP", obj.Message);
        }

        [HookCallback]
        private static void OnHooksRegistered()
        {
            ExLoader.Info("SCP HTTP", $"Initializing client ..");


            if (Plugin.Config.ShowDebug)
            {
                LogOutput.DefaultLoggers.Add(new ScpLogger());
                CommonLibrary.Initialize(new string[] { "-DebugLogs" });
            }
            else
                CommonLibrary.Initialize(Array.Empty<string>());

            if (string.IsNullOrWhiteSpace(Plugin.Config.Url))
            {
                ExLoader.Error("SCP HTTP", $"You need to set the server's URL.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Plugin.Config.Id))
            {
                ExLoader.Error("SCP HTTP", $"You need to set the server's ID.");
                return;
            }

            CommonLib.Serialization.Serialization.LoadSerializers();
            CommonLib.Serialization.Deserialization.LoadDeserializers();

            CommonLib.Serialization.Serialization.RegisterTypes(
                typeof(InvokeMethodMessage),
                typeof(InvokeMethodResult),

                typeof(SetPropertyMessage),

                typeof(RequestMessage),
                typeof(ResponseMessage),

                typeof(ServerVerificationRequestMessage),
                typeof(ServerVerificationResponseMessage));

            Initialize(Plugin.Config.Url);
        }
    }
}