using CommonLib;
using CommonLib.Logging;

using LabExtended.Core;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using SlNetworkApiClient.Network;

using System;

namespace SlNetworkApiClient
{
    public class Plugin
    {
        public static Plugin Singleton { get; private set; }
        public static Config Config { get; private set; }

        public PluginHandler Handler;

        [PluginConfig]
        public Config Instance;

        [PluginEntryPoint("SlNetworkApiClient", "1.0.0", "The client for the HTTP SCP SL API server.", "marchellcx")]
        public void Load()
        {
            Singleton = this;
            Config = Instance;
            Handler = PluginHandler.Get(this);

            ExLoader.Info("SCP HTTP", $"Initializing client ..");

            if (Config.ShowDebug)
                LogOutput.DefaultLoggers.Add(new ScpLogger());

            if (Config.ShowDebug)
                CommonLibrary.Initialize(new string[] { "-DebugLogs" });
            else
                CommonLibrary.Initialize(Array.Empty<string>());

            if (string.IsNullOrWhiteSpace(Config.Url))
            {
                ExLoader.Error("SCP HTTP", $"You need to set the server's URL.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Config.Id))
            {
                ExLoader.Error("SCP HTTP", $"You need to set the server's ID.");
                return;
            }

            ExLoader.Info("SCP HTTP", $"Connecting to &1{Config.Url}&r as &3{Config.Id}&r ..");

            ScpClient.Initialize(Config.Url);
        }
    }
}