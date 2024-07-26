using CommandSystem;

using LabExtended.Commands;

namespace SlNetworkApiClient.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class NetworkCommands : VanillaParentCommandBase
    {
        public NetworkCommands() : base("network", "A set of commands to control the HTTP network API.") { }

        public override void OnInitialized()
        {
            base.OnInitialized();

            RegisterCommand(new ReconnectCommand());
        }
    }
}