using LabExtended.Attributes;
using LabExtended.Core;

using SlNetworkApiClient.Events;
using SlNetworkApiClient.Network;

using System;

namespace SlNetworkApiClient
{
    public class TestModule : ScpModule
    {
        public override string Name => "Test Module";

        public override void Start()
        {
            base.Start();
            ExLoader.Info("Test Module", "Started");
        }

        public override void Stop()
        {
            base.Stop();
            ExLoader.Info("Test Module", "Stopped");
        }

        public string ClientInfo()
            => $"Hello: {DateTime.Now}";

        [HookDescriptor]
        private static void OnConnected(ClientConnectedArgs _)
            => ScpClient.AddModule<TestModule>();
    }
}