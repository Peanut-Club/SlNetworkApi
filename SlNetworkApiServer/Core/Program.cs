using CommonLib;
using CommonLib.Configs;
using CommonLib.Extensions;
using CommonLib.Logging;
using CommonLib.Utilities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using SlNetworkApi.Modules;
using SlNetworkApi.Requests;
using SlNetworkApi.Server.Verification;

namespace SlNetworkApiServer.Core
{
    public static class Program
    {
        public static ConfigFile Config { get; private set; }
        public static LogOutput Log { get; private set; }

        public static List<Assembly> Assemblies { get; } = new List<Assembly>();

        [Config("Address", "Sets the server's IP address.")]
        public static string Address { get; set; } = "127.0.0.1";

        [Config("Port", "Sets the server's port.")]
        public static int Port { get; set; } = 8080;

        [Config("Delay", "Sets the server's disconnect delay.")]
        public static TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(500);

        public static async Task Main(string[] args)
        {
            try
            {
                CommonLibrary.Initialize(Environment.GetCommandLineArgs());

                Log = new LogOutput("Core");
                Log.Setup();

                if (ConsoleArgs.HasSwitch("DebugLogs"))
                    LogOutput.EnableForAll(LogLevel.Debug);

                Config = new ConfigFile($"{Directory.GetCurrentDirectory()}/config.ini");

                Config.Serializer = value => value.JsonSerialize(true);
                Config.Deserializer = (value, type) => value.JsonDeserialize(type, true);

                Config.Bind();

                var failed = Config.Load();

                foreach (var pair in failed)
                    Log.Error($"Failed to load config key '{pair.Key}': {pair.Value}");

                var depsDir = $"{Directory.GetCurrentDirectory()}/modules";

                if (!Directory.Exists(depsDir))
                    Directory.CreateDirectory(depsDir);

                foreach (var file in Directory.GetFiles(depsDir, "*.dll"))
                {
                    try
                    {
                        var assemblyRaw = File.ReadAllBytes(file);
                        var assembly = Assembly.Load(assemblyRaw);

                        assembly?.InvokeStaticMethods(m => m.Name == "InvokeOnServer");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to load assembly '{Path.GetFileName(file)}': {ex}");
                    }
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

                NetworkManager.OnServerConnected += scp => scp.AddModule<TestModule>();
                NetworkManager.Initialize(Address, Port, Delay);
            }
            catch (Exception ex)
            {
                LogOutput.Raw(ex, ConsoleColor.Red);
            }

            await Task.Delay(-1);
        }
    }
}