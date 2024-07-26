using CommonLib.Networking.Http.Transport.Enums;

using LabExtended.Core.Hooking.Interfaces;

namespace SlNetworkApiClient.Events
{
    public class ClientRejectedArgs : IHookEvent
    {
        public RejectReason Reason { get; }

        internal ClientRejectedArgs(RejectReason reason)
            => Reason = reason;
    }
}