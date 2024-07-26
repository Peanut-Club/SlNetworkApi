using LabExtended.Core.Hooking.Interfaces;

namespace SlNetworkApiClient.Events
{
    public class ClientDisconnectedArgs : IHookEvent
    {
        public bool ShouldReconnect { get; set; }

        internal ClientDisconnectedArgs(bool reconnect)
            => ShouldReconnect = reconnect;
    }
}