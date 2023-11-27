namespace DeveloperConsole {
    public class ToggleDebugModeCommand : ConsoleCommand {
        public ToggleDebugModeCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            DebugInput.isDebugBuildOverride = !DebugInput.isDebugBuildOverride;
            return new SuccessResponse($"Debug mode turned {(DebugInput.isDebugBuildOverride ? "on" : "off")}");
        }
    }
}
