using CommonLib.Logging;

using LabExtended.Core;

using System;

namespace SlNetworkApiClient.Network
{
    public class ScpLogger : ILogger
    {
        public DateTime Started { get; } = DateTime.Now;

        public LogMessage Latest { get; } = default;

        public void Emit(LogMessage message)
        {
            if (message.Message is null)
                return;

            switch (message.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                case LogLevel.Verbose:
                    ExLoader.Debug(message.Source?.GetString() ?? "Common Library", message.Message.GetString());
                    break;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    ExLoader.Error(message.Source?.GetString() ?? "Common Library", message.Message.GetString());
                    break;

                case LogLevel.Information:
                    ExLoader.Info(message.Source?.GetString() ?? "Common Library", message.Message.GetString());
                    break;

                case LogLevel.Warning:
                    ExLoader.Warn(message.Source?.GetString() ?? "Common Library", message.Message.GetString());
                    break;
            }
        }
    }
}