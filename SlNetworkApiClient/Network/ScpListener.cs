using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using System;
using System.Collections.Generic;

namespace SlNetworkApiClient.Network
{
    public static class ScpListener
    {
        private static readonly Dictionary<Type, Func<IHttpMessage, IHttpMessage>> _returnListeners = new Dictionary<Type, Func<IHttpMessage, IHttpMessage>>();
        private static readonly Dictionary<Type, Action<IHttpMessage>> _staticListeners = new Dictionary<Type, Action<IHttpMessage>>();

        static ScpListener()
            => ScpClient.OnServerConnected += OnConnected;

        public static void Unregister<T>()
            => Unregister(typeof(T));

        public static void Unregister(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _returnListeners.Remove(type);
            _staticListeners.Remove(type);
        }

        public static void Register<T>(Func<T, IHttpMessage> listener) where T : IHttpMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public static void Register<T>(Action<T> listener) where T : IHttpMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public static void Register(Type type, Func<IHttpMessage, IHttpMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _returnListeners[type] = listener;
        }

        public static void Register(Type type, Action<IHttpMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _staticListeners[type] = listener;
        }

        private static void OnMessage(IHttpMessage obj)
        {
            var type = obj.GetType();

            if (_returnListeners.TryGetValue(type, out var listener))
            {
                var msg = listener?.Invoke(obj);

                if (msg != null)
                    ScpClient.Send(msg);
            }

            if (_staticListeners.TryGetValue(type, out var staticListener))
                staticListener?.Invoke(obj);
        }

        internal static void OnConnected()
        {
            _staticListeners.Clear();
            _returnListeners.Clear();

            ScpClient.OnServerDisconnected += OnDisconnected;
            ScpClient.OnServerMessage += OnMessage;
        }

        private static void OnDisconnected()
        {
            _staticListeners.Clear();
            _returnListeners.Clear();

            ScpClient.OnServerDisconnected -= OnDisconnected;
            ScpClient.OnServerMessage -= OnMessage;
        }
    }
}
