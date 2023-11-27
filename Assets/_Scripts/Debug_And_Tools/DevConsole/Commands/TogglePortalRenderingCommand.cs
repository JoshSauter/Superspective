using System;
using PortalMechanics;

namespace DeveloperConsole {
    public class TogglePortalRenderingCommand : ConsoleCommand {
        public TogglePortalRenderingCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            Portal.allowPortalRendering = !Portal.allowPortalRendering;
            return new SuccessResponse($"Portal rendering {(Portal.allowPortalRendering ? "enabled" : "disabled")}");
        }
    }
}
