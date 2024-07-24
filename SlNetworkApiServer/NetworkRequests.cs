using CommonLib.Caching;
using CommonLib.Logging;
using CommonLib.Extensions;
using CommonLib.Utilities.Generation;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Networking.Http.Transport.Enums;

using SlNetworkApiServer.Servers;
using SlNetworkApi.Requests;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SlNetworkApiServer
{
    public class NetworkRequests
    {
        private ScpServer _server;
        private UniqueStringGenerator _id;

        private readonly Dictionary<string, Action<ResponseMessage>> _responseHandlers = new Dictionary<string, Action<ResponseMessage>>();
        private readonly Dictionary<Type, Func<RequestMessage, ResponseMessage>> _requestHandlers = new Dictionary<Type, Func<RequestMessage, ResponseMessage>>();

        public LogOutput Log { get; } = new LogOutput("Network Requests").Setup();

        public NetworkRequests(ScpServer server)
        {
            _server = server;
            _id = new UniqueStringGenerator(new MemoryCache<string>(), 10, false);

            _server.OnDisconnected += OnDisconnected;
            _server.OnMessage += OnMessage;
        }

        public async Task<T> GetAsync<T>(object request, bool throwOnFailed = false, TimeSpan? timeout = null)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var status = false;
            var message = default(ResponseMessage);
            var time = DateTime.Now;

            Get(request, msg =>
            {
                message = msg;
                status = true;
            });

            while (!status)
            {
                if (timeout.HasValue && (DateTime.Now - time) >= timeout.Value)
                    throw new TimeoutException();

                await Task.Delay(50);
            }

            if (!message.IsSuccess)
                throw new Exception($"Request {message.Id} has failed");

            if (message.Response is null)
                return default;

            if (!message.Response.Is<T>(out var castValue))
                throw new InvalidCastException();

            return castValue;
        }

        public async Task<ResponseMessage> GetAsync(object request, TimeSpan? timeout = null)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var status = false;
            var message = default(ResponseMessage);
            var time = DateTime.Now;

            Get(request, msg =>
            {
                message = msg;
                status = true;
            });

            while (!status)
            {
                if (timeout.HasValue && (DateTime.Now - time) >= timeout.Value)
                    throw new TimeoutException();

                await Task.Delay(50);
            }

            return message;
        }

        public void Get<T>(object request, Action<T> response)
            => Get(request, msg =>
            {
                if (msg.IsSuccess)
                    response((T)msg.Response);
                else
                    throw new Exception($"Request {msg.Id} failed");
            });

        public void Get(object request, Action<ResponseMessage> response)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (response is null)
                throw new ArgumentNullException(nameof(response));

            var id = _id.Next();

            _responseHandlers[id] = response;
            _server.Send(new RequestMessage(id, request));
        }

        public void Unregister<T>()
            => _requestHandlers.Remove(typeof(T));

        public void Unregister(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _requestHandlers.Remove(type);
        }

        public void Register<TMessage, TResponse>(Func<TMessage, TResponse> listener)
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

        public void Register(Type type, Func<object, object> listener)
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

        public void Register<T>(Func<RequestMessage, ResponseMessage> listener)
            => Register(typeof(T), listener);

        public void Register(Type type, Func<RequestMessage, ResponseMessage> listener)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _requestHandlers[type] = listener;
        }

        private void InternalHandleRequest(RequestMessage requestMessage)
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

                _server.Send(response);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle request {requestMessage.Id}:\n{ex}");
            }
        }

        private void InternalHandleResponse(ResponseMessage responseMessage)
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
                Log.Error($"Failed to handle response {responseMessage.Id}:\n{ex}");
            }
        }

        private void OnMessage(IHttpMessage obj)
        {
            if (obj is RequestMessage requestMessage)
                InternalHandleRequest(requestMessage);
            else if (obj is ResponseMessage responseMessage)
                InternalHandleResponse(responseMessage);
        }

        private void OnDisconnected(DisconnectReason obj)
        {
            _responseHandlers.Clear();
            _requestHandlers.Clear();

            _id.FreeAll();
            _id = null;

            _server.OnDisconnected -= OnDisconnected;
            _server.OnMessage -= OnMessage;
            _server = null;
        }
    }
}