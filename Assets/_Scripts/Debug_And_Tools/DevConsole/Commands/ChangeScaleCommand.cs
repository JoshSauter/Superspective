using System;

namespace DeveloperConsole {
    public class ChangeScaleCommand : ConsoleCommand {
        public ChangeScaleCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            if (args.Length < 1) {
                return ChangeScale(1f);
            }
            
            string scaleArg = args[0];
            if (float.TryParse(scaleArg, out float targetScale)) {
                return ChangeScale(targetScale);
            }
            else {
                return new FailureResponse($"Couldn't parse argument {scaleArg} as a float");
            }
        }

        CommandResponse ChangeScale(float targetScale) {
            if (targetScale <= 0) {
                return new FailureResponse($"Target scale {targetScale} is not greater than 0");
            }
            
            Player.instance.growShrink.SetScaleDirectly(targetScale);
            return new SuccessResponse($"Set player scale to {targetScale}");
        }
    }
}
