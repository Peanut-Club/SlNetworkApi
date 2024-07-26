using CommonLib.Extensions;
using CommonLib.Networking.Interfaces;

using SlNetworkApi.Modules;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System;

using LabExtended.Core;

namespace SlNetworkApiClient.Network
{
    public abstract class ScpModule
    {
        private static int _invokeClock = 0;

        private volatile Dictionary<MethodInfo, ushort> _methods = new Dictionary<MethodInfo, ushort>();
        private volatile Dictionary<PropertyInfo, ushort> _properties = new Dictionary<PropertyInfo, ushort>();

        private volatile Dictionary<string, object> _values = new Dictionary<string, object>();
        private volatile Dictionary<int, Action<object>> _invoke = new Dictionary<int, Action<object>>();

        private volatile ushort _code;

        public abstract string Name { get; }

        public virtual void Start()
        {
            var type = GetType();

            _code = Name.GetShortCode();

            foreach (var method in type.GetAllMethods())
            {
                if (method.IsStatic)
                    continue;

                _methods.Add(method, method.Name.GetShortCode());
            }

            foreach (var property in type.GetAllProperties())
            {
                if (!property.Name.StartsWith("Network"))
                    continue;

                var getMethod = property.GetGetMethod(true);
                var setMethod = property.GetSetMethod(true);

                if (getMethod is null || setMethod is null)
                    continue;

                if (setMethod.IsStatic || getMethod.IsStatic)
                    continue;

                _properties.Add(property, property.Name.GetShortCode());
                _values[property.Name] = property.GetValue(this);
            }
        }

        public virtual void Stop()
        {
            _invoke.Clear();
            _values.Clear();
            _methods.Clear();
            _properties.Clear();
        }

        public virtual void OnMessage(INetworkMessage message)
        {
            if (message is InvokeMethodMessage methodMessage)
                OnInvokeMethod(methodMessage);
            else if (message is SetPropertyMessage setPropertyMessage)
                OnSetProperty(setPropertyMessage);
            else if (message is InvokeMethodResult methodResult)
                OnInvokeResult(methodResult);
        }

        public async Task<T> InvokeAsync<T>(string name, TimeSpan? timeout, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            var id = _invokeClock++;
            var done = false;
            var result = default(object);
            var time = DateTime.Now;

            _invoke[id] = value =>
            {
                result = value;
                done = true;
            };

            ScpClient.Send(new InvokeMethodMessage(_code, name.GetShortCode(), args ?? Array.Empty<object>(), id));

            while (!done)
            {
                await Task.Delay(100);

                if (timeout.HasValue && (DateTime.Now - time) >= timeout.Value)
                    throw new TimeoutException();
            }

            if (result is null)
                return default;

            return (T)result;
        }

        public void InvokeMethod<T>(string name, object[] args, Action<T> response, bool invokeNull = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            var id = _invokeClock++;

            _invoke[id] = value =>
            {
                if (value is null)
                {
                    if (invokeNull)
                        response?.Invoke(default);

                    return;
                }

                if (!value.Is<T>(out var cast))
                    return;

                response?.Invoke(cast);
            };

            ScpClient.Send(new InvokeMethodMessage(_code, name.GetShortCode(), args ?? Array.Empty<object>(), id));
        }

        public void InvokeMethod(string name, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            ScpClient.Send(new InvokeMethodMessage(_code, name.GetShortCode(), args, _invokeClock++));
        }

        public T GetProperty<T>(string name)
        {
            if (_values.TryGetValue(name, out var value))
            {
                if (value is null)
                    return default;

                if (!value.Is<T>(out var cast))
                    throw new InvalidCastException();

                return cast;
            }

            throw new Exception($"Unknown property: {name}");
        }

        public void SetProperty(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!_properties.TryGetFirst(m => m.Key.Name == name, out var property))
                throw new Exception($"Unknown property: {name}");

            _values[name] = value;

            ScpClient.Send(new SetPropertyMessage(_code, property.Value, value));
        }

        private void OnSetProperty(SetPropertyMessage msg)
        {
            if (msg.Type != _code)
                return;

            if (!_properties.TryGetFirst(m => m.Value == msg.Code, out var property))
                return;

            if (!_values.ContainsKey(property.Key.Name))
                return;

            _values[property.Key.Name] = msg.Value;
        }

        private void OnInvokeMethod(InvokeMethodMessage msg)
        {
            if (msg.TypeCode != _code)
                return;

            ExLoader.Debug($"SCP HTTP", $"Received a method invocation request: code={msg.Code} typeCode={msg.TypeCode} id={msg.Id} args={msg.Args.Length}");

            if (!_methods.TryGetFirst(m => m.Value == msg.Code, out var method))
            {
                ExLoader.Warn($"SCP HTTP", $"Unknown method code: {msg.Code}");
                return;
            }

            try
            {
                ExLoader.Debug($"SCP HTTP", $"Invoking method &1{method.Key.ToName()}&r");

                var result = method.Key.Invoke(this, msg.Args);

                ExLoader.Debug($"SCP HTTP", $"Method result: {result?.ToString() ?? "null"}");

                ScpClient.Send(new InvokeMethodResult(msg.Id, result, string.Empty));
            }
            catch (Exception ex)
            {
                ScpClient.Send(new InvokeMethodResult(msg.Id, null, ex.ToString()));
            }
        }

        private void OnInvokeResult(InvokeMethodResult msg)
        {
            if (!_invoke.TryGetValue(msg.Id, out var invoke))
                return;

            _invoke.Remove(msg.Id);

            if (!string.IsNullOrWhiteSpace(msg.Exception))
            {
                ExLoader.Error("SCP Modules", $"Remote-side method execution caught an error: {msg.Exception}");
                return;
            }

            invoke(msg.Value);
        }
    }
}