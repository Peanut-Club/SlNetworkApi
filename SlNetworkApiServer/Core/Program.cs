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

                        if (assembly != null)
                        {
                            Assemblies.Add(assembly);

                            foreach (var type in assembly.GetTypes())
                            {
                                foreach (var method in type.GetAllMethods())
                                {
                                    if (!method.IsStatic || method.Name != "InvokeServer" || method.Parameters().Length > 0)
                                        continue;

                                    try
                                    {
                                        method.Invoke(null, null);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"Failed to invoke invoke method '{method.ToName()}': {ex}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to load assembly '{Path.GetFileName(file)}': {ex}");
                    }
                }

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