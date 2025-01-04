using System;
using PortalMechanics;

namespace DeveloperConsole {
    public class TogglePortalRenderingCommand : ConsoleCommand {
        public TogglePortalRenderingCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            Portal.forceDebugRenderMode = !Portal.forceDebugRenderMode;
            return new SuccessResponse($"Portal rendering {(!Portal.forceDebugRenderMode ? "enabled" : "disabled")}");
        }
    }
}
