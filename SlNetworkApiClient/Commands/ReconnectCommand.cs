using LabExtended.API;

using LabExtended.Commands;
using LabExtended.Commands.Arguments;

using LabExtended.Core.Commands.Interfaces;

using SlNetworkApiClient.Network;

namespace SlNetworkApiClient.Commands
{
    public class ReconnectCommand : CustomCommand
    {
        public override string Command => "reconnect";
        public override string Description => "Attempts to reconnect to the central server.";

        public override void OnCommand(ExPlayer sender, ICommandContext ctx, ArgumentCollection args)
        {
            base.OnCommand(sender, ctx, args);

            ScpClient.Initialize(Plugin.Config.Url);

            ctx.RespondOk("Attempting to reconnect ..");
        }
    }
}