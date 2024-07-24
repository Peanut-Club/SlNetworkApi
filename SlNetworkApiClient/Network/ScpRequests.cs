using CommonLib.Caching;
using CommonLib.Utilities.Generation;

using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using SlNetworkApi.Requests;

using System;
using System.Collections.Generic;

using LabExtended.Core;

namespace SlNetworkApiClient.Network
{
    public static class ScpRequests
    {
        private static volatile UniqueStringGenerator _id = new UniqueStringGenerator(new MemoryCache<string>(), 10, false);

        private static volatile Dictionary<string, Action<ResponseMessage>> _responseHandlers = new Dictionary<string, Action<ResponseMessage>>();
        private static volatile Dictionary<Type, Func<RequestMessage, ResponseMessage>> _requestHandlers = new Dictionary<Type, Func<RequestMessage, ResponseMessage>>();

        public static void Get<T>(object request, Action<T> response)
            => Get(request, msg =>
            {
                if (msg.IsSuccess)
                    response((T)msg.Response);
                else
                    throw new Exception($"Request {msg.Id} failed");
            });

        public static void Get(object request, Action<ResponseMessage> response)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (response is null)
                throw new ArgumentNullException(nameof(response));

            var id = _id.Next();

            _responseHandlers[id] = response;

            ScpClient.Send(new RequestMessage(id, request));
        }

        public static void Unregister<T>()
            => _requestHandlers.Remove(typeof(T));

        public static void Unregister(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _requestHandlers.Remove(type);
        }

        public static void Register<TMessage, TResponse>(Func<TMessage, TResponse> listener)
            => Register(typeof(TMessage), new Func<RequestMessage, ResponseMessage>(msg =>
            {
                try
                {
                    return new ResponseMessage(msg.Id, listener((TMessage)msg.Message), true);
                }
                catch
                {
                    return new ResponseMessage(msg.Id, null, false);
                }
            }));

        public static void Register(Type type, Func<object, object> listener)
            => Register(type, new Func<RequestMessage, ResponseMessage>(msg =>
            {
                try
                {
                    return new ResponseMessage(msg.Id, listener(msg.Message), true);
                }
                catch
                {
                    return new ResponseMessage(msg.Id, null, false);
                }
            }));

        public static void Register<T>(Func<RequestMessage, ResponseMessage> listener)
            => Register(typeof(T), listener);

        public static void Register(Type type, Func<RequestMessage, ResponseMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _requestHandlers[type] = listener;
        }

        private static void InternalHandleRequest(RequestMessage requestMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestMessage.Id) || requestMessage.Message is null)
                    return;

                var type = requestMessage.Message.GetType();

                if (!_requestHandlers.TryGetValue(type, out var handler))
                    return;

                var response = handler.Invoke(requestMessage);

                if (string.IsNullOrWhiteSpace(response.Id) || response.Response is null)
                    return;

                ScpClient.Send(response);
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP Requests", $"Failed to handle request {requestMessage.Id}:\n{ex}");
            }
        }

        private static void InternalHandleResponse(ResponseMessage responseMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseMessage.Id) || responseMessage.Response is null)
                    return;

                if (!_responseHandlers.TryGetValue(responseMessage.Id, out var handler))
                    return;

                _responseHandlers.Remove(responseMessage.Id);

                handler?.Invoke(responseMessage);
            }
            catch (Exception ex)
            {
                ExLoader.Error("SCP Requests", $"Failed to handle response {responseMessage.Id}:\n{ex}");
            }
        }

        internal static void OnConnected()
        {
            _responseHandlers.Clear();
            _requestHandlers.Clear();

            _id.FreeAll();

            ScpClient.OnServerDisconnected += OnDisconnected;
            ScpClient.OnServerMessage += OnMessage;
        }

        private static void OnMessage(IHttpMessage obj)
        {
            ExLoader.Info("SCP Requests", $"Received message: {obj.GetType().FullName}");

            if (obj is RequestMessage requestMessage)
                InternalHandleRequest(requestMessage);
            else if (obj is ResponseMessage responseMessage)
                InternalHandleResponse(responseMessage);
        }

        private static void OnDisconnected()
        {
            ExLoader.Info("SCP Requests", $"Server disconnected!");

            _responseHandlers.Clear();
            _requestHandlers.Clear();

            _id.FreeAll();

            ScpClient.OnServerDisconnected -= OnDisconnected;
            ScpClient.OnServerMessage -= OnMessage;
        }
    }
}