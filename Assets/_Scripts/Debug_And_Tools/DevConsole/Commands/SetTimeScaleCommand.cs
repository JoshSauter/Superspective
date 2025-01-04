using UnityEngine;

namespace DeveloperConsole {
    public class SetTimeScaleCommand : ConsoleCommand {
        private float originalTimeScale = -1;
        
        public SetTimeScaleCommand(string commandWord) : base(commandWord) {}
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            if (originalTimeScale < 0) {
                originalTimeScale = GameManager.timeScale;
            }

            if (args.Length < 1) {
                GameManager.timeScale = originalTimeScale;
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale; // Reset to default scaled value
                DevConsoleBehaviour.instance.pausedTimeScale = originalTimeScale;
                return new SuccessResponse($"Reset time scale to {originalTimeScale}");
            }
            
            if (float.TryParse(args[0], out float newTimeScale)) {
                GameManager.timeScale = newTimeScale;
                Time.timeScale = newTimeScale;
                Time.fixedDeltaTime = 0.02f * newTimeScale; // Scale fixed delta time with time scale
                DevConsoleBehaviour.instance.pausedTimeScale = newTimeScale;
                return new SuccessResponse($"Set time scale to {newTimeScale}");
            }
            else {
                return new FailureResponse($"Argument {args[0]} is not a valid float.");
            }
        }
    }
}
