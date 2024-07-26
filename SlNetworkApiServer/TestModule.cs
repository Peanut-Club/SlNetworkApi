using System.Threading.Tasks;

namespace SlNetworkApiServer
{
    public class TestModule : NetworkModule
    {
        public override string Name => "Test Module";

        public override void Start()
        {
            base.Start();
            Log.Info("Started");

            Task.Run(async () =>
            {
                await Task.Delay(5000);

                var str = await InvokeAsync<string>("ClientInfo", null);

                Log.Info("Result: " + str);
            });
        }

        public override void Stop()
        {
            base.Stop();
            Log.Info("Stopped");
        }
    }
}