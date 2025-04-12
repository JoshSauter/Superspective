using UnityEngine;

namespace DeveloperConsole {
    public class ShowInvisibleWallsCommand : ConsoleCommand {
        private readonly int InvisibleWallsMask = 1 << SuperspectivePhysics.InvisibleWallLayer;
        private readonly Camera PlayerCam = Player.instance.PlayerCam;
        
        public ShowInvisibleWallsCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            bool shouldShowTriggers = (PlayerCam.cullingMask & InvisibleWallsMask) == 0;
            if (args.Length >= 1) {
                string boolArg = args[0];
                if (!TryParseBool(boolArg, out shouldShowTriggers)) {
                    return new FailureResponse($"Invalid argument: {boolArg}. Expected 'true' or 'false'.");
                }
            }

            
            if (shouldShowTriggers) {
                PlayerCam.cullingMask |= InvisibleWallsMask;
            }
            else {
                PlayerCam.cullingMask &= ~InvisibleWallsMask;
            }
            return new SuccessResponse($"Invisible walls {(shouldShowTriggers ? "revealed" : "hidden")}.");
        }
    }
}