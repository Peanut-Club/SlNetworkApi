using PluginAPI.Enums;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

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
        [PluginPriority(LoadPriority.Low)]
        public void Load()
        {
            Singleton = this;
            Config = Instance;
            Handler = PluginHandler.Get(this);
        }
    }
}