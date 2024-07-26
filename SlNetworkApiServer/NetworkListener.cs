using CommonLib.Networking.Interfaces;

using SlNetworkApiServer.Servers;

using System;
using System.Collections.Generic;

namespace SlNetworkApiServer
{
    public class NetworkListener
    {
        private ScpServer _server;

        private readonly Dictionary<Type, Func<INetworkMessage, INetworkMessage>> _returnListeners = new Dictionary<Type, Func<INetworkMessage, INetworkMessage>>();
        private readonly Dictionary<Type, Action<INetworkMessage>> _staticListeners = new Dictionary<Type, Action<INetworkMessage>>();

        public NetworkListener(ScpServer server)
        {
            if (server is null)
                throw new ArgumentNullException(nameof(server));

            _server = server;

            server.OnDisconnected += OnDisconnected;
            server.OnMessage += OnMessage;
        }

        public void Unregister<T>()
            => Unregister(typeof(T));

        public void Unregister(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _returnListeners.Remove(type);
            _staticListeners.Remove(type);
        }

        public void Register<T>(Func<T, INetworkMessage> listener) where T : INetworkMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public void Register<T>(Action<T> listener) where T : INetworkMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public void Register(Type type, Func<INetworkMessage, INetworkMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _returnListeners[type] = listener;
        }

        public void Register(Type type, Action<INetworkMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _staticListeners[type] = listener;
        }

        private void OnMessage(INetworkMessage obj)
        {
            var type = obj.GetType();

            if (_returnListeners.TryGetValue(type, out var listener))
            {
                var msg = listener?.Invoke(obj);

                if (msg != null)
                    _server.Send(msg);
            }

            if (_staticListeners.TryGetValue(type, out var staticListener))
                staticListener?.Invoke(obj);
        }

        private void OnDisconnected(CommonLib.Networking.Http.Transport.Enums.DisconnectReason obj)
        {
            _staticListeners.Clear();
            _returnListeners.Clear();

            _server.OnDisconnected -= OnDisconnected;
            _server.OnMessage -= OnMessage;
            _server = null;
        }
    }
}