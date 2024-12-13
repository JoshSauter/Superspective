using System;
using PortalMechanics;

namespace DeveloperConsole {
    public class TogglePortalDebuggingCommand : ConsoleCommand {
        public TogglePortalDebuggingCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            bool debugEnabled = VirtualPortalCamera.instance.DEBUG = !VirtualPortalCamera.instance.DEBUG;
            return new SuccessResponse($"Portal debugging {(debugEnabled ? "enabled" : "disabled")}.");
        }
    }
}
