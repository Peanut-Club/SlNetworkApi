using CommonLib.Logging;
using CommonLib.Extensions;
using CommonLib.Networking.Interfaces;
using CommonLib.Networking.Http.Transport;
using CommonLib.Networking.Http.Transport.Enums;

using System;
using System.Collections.Generic;
using System.Net;

using SlNetworkApi.Server.Verification;

namespace SlNetworkApiServer.Servers
{
    public class ScpServer
    {
        private volatile HttpTransportPeer _peer;
        private volatile Dictionary<Type, NetworkModule> _modules = new Dictionary<Type, NetworkModule>();

        public string Token { get; }

        public int Port { get; internal set; }

        public string Id { get; internal set; }
        public string Name { get; internal set; }

        public bool IsVerified { get; private set; } = false;

        public TimeSpan Latency => _peer.Latency;
        public IPEndPoint Address => _peer.RemoteIp;

        public NetworkListener Listener { get; }
        public NetworkRequests Requests { get; }

        public event Action<INetworkMessage> OnMessage;
        public event Action<DisconnectReason> OnDisconnected;

        public ScpServer(string token, HttpTransportPeer peer)
            => (Token, Listener, Requests, _peer) = (token, new NetworkListener(this), new NetworkRequests(this), peer);

        public T GetModule<T>() where T : NetworkModule
            => _modules.TryGetValue(typeof(T), out var module) ? (T)module : null;

        public T AddModule<T>() where T : NetworkModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
                return (T)module;

            module = typeof(T).Construct<NetworkModule>();

            module.Server = this;
            module.Log = new LogOutput($"{module.GetType().Name} @ {Id}");

            module.Start();

            _modules[typeof(T)] = module;
            return (T)module;
        }

        public void AddModule(NetworkModule module)
        {
            if (module is null)
                throw new ArgumentNullException(nameof(module));

            module.Server = this;
            module.Log = new LogOutput($"{module.GetType().Name} @ {Id}");

            _modules[module.GetType()] = module;
        }

        public void RemoveModule(NetworkModule module)
        {
            if (module is null)
                throw new ArgumentNullException(nameof(module));

            _modules.Remove(module.GetType());
        }

        public void RemoveModule<T>() where T : class
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                module.Stop();
            }

            _modules.Remove(typeof(T));
        }

        public void RemoveModules()
        {
            foreach (var module in _modules.Values)
            {
                module.Stop();
            }

            _modules.Clear();
        }

        public void Send(INetworkMessage message)
            => _peer.Send(message);

        public void Disconnect(DisconnectReason reason)
            => _peer.Disconnect(reason);

        internal void InternalHandleDisconnect(DisconnectReason reason)
        {
            OnDisconnected?.Invoke(reason);
            RemoveModules();
        }

        internal void InternalHandleMessage(INetworkMessage httpMessage)
        {
            try
            {
                NetworkManager.Log.Debug($"Procesing message {httpMessage.GetType().FullName}");

                if (!IsVerified)
                {
                    if (httpMessage is ServerVerificationResponseMessage msg)
                    {
                        Id = msg.Id;
                        Name = msg.Name;
                        Port = msg.Port;

                        IsVerified = true;

                        NetworkManager.VerifyServer(this);
                    }
                    else
                    {
                        NetworkManager.Log.Warn($"Received a message on an unverified server!");
                    }

                    return;
                }

                OnMessage?.Invoke(httpMessage);

                NetworkManager.Log.Debug($"Event invoked");

                foreach (var module in _modules.Values)
                {
                    NetworkManager.Log.Debug($"Module: {module.GetType().FullName}");

                    module.OnMessage(httpMessage);

                    NetworkManager.Log.Debug($"Module invoked");
                }
            }
            catch (Exception ex)
            {
                NetworkManager.Log.Error(ex);
            }
        }
    }
}