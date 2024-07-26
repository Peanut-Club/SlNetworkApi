using CommonLib.Networking.Interfaces;

using LabExtended.Attributes;

using SlNetworkApiClient.Events;

using System;
using System.Collections.Generic;

namespace SlNetworkApiClient.Network
{
    public static class ScpListener
    {
        private static readonly Dictionary<Type, Func<INetworkMessage, INetworkMessage>> _returnListeners = new Dictionary<Type, Func<INetworkMessage, INetworkMessage>>();
        private static readonly Dictionary<Type, Action<INetworkMessage>> _staticListeners = new Dictionary<Type, Action<INetworkMessage>>();

        public static void Unregister<T>()
            => Unregister(typeof(T));

        public static void Unregister(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _returnListeners.Remove(type);
            _staticListeners.Remove(type);
        }

        public static void Register<T>(Func<T, INetworkMessage> listener) where T : INetworkMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public static void Register<T>(Action<T> listener) where T : INetworkMessage
            => Register(typeof(T), msg => listener?.Invoke((T)msg));

        public static void Register(Type type, Func<INetworkMessage, INetworkMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _returnListeners[type] = listener;
        }

        public static void Register(Type type, Action<INetworkMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _staticListeners[type] = listener;
        }

        [HookDescriptor]
        private static void OnMessage(ClientMessageArgs args)
        {
            var obj = args.Message;
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

        [HookDescriptor]
        internal static void OnConnected(ClientConnectedArgs _)
        {
            _staticListeners.Clear();
            _returnListeners.Clear();
        }

        [HookDescriptor]
        internal static void OnDisconnected(ClientDisconnectedArgs _)
        {
            _staticListeners.Clear();
            _returnListeners.Clear();
        }
    }
}
