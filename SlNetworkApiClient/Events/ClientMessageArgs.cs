using CommonLib.Networking.Interfaces;

using LabExtended.Core.Hooking.Interfaces;

namespace SlNetworkApiClient.Events
{
    public class ClientMessageArgs : IHookEvent
    {
        public INetworkMessage Message { get; }

        internal ClientMessageArgs(INetworkMessage message)
            => Message = message;
    }
}
