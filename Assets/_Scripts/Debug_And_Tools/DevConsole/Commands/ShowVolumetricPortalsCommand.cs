using System;
using PortalMechanics;

namespace DeveloperConsole {
    public class ShowVolumetricPortalsCommand : ConsoleCommand {
        public ShowVolumetricPortalsCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            if (args.Length > 0) {
                string boolArg = args[0];
                if (!TryParseBool(boolArg, out Portal.forceVolumetricPortalsOn)) {
                    return new FailureResponse($"Invalid argument: {boolArg}. Expected 'true' or 'false'.");
                }
            }
            
            Portal.forceVolumetricPortalsOn = !Portal.forceVolumetricPortalsOn;
            return new SuccessResponse(Portal.forceVolumetricPortalsOn ? "Volumetric Portals are forced on." : "Volumetric Portals are back to normal.");
        }
    }
}
