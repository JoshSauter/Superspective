using System;
using UnityEngine;

namespace DeveloperConsole {
    public class ShowTriggers : ConsoleCommand {
        private int TriggerZoneMask = 1 << SuperspectivePhysics.TriggerZoneLayer;
        private Camera PlayerCam = Player.instance.PlayerCam;
        
        public ShowTriggers(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            bool shouldShowTriggers = (PlayerCam.cullingMask & TriggerZoneMask) == 0;
            if (args.Length >= 1) {
                string trueFalseArg = args[0];
                if (trueFalseArg.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                    shouldShowTriggers = true;
                }
                else if (trueFalseArg.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                    shouldShowTriggers = false;
                }
                else {
                    return new FailureResponse($"Invalid argument: {trueFalseArg}. Expected 'true' or 'false'.");
                }
            }

            
            if (shouldShowTriggers) {
                PlayerCam.cullingMask |= TriggerZoneMask;
            }
            else {
                PlayerCam.cullingMask &= ~TriggerZoneMask;
            }
            return new SuccessResponse($"Triggers {(shouldShowTriggers ? "revealed" : "hidden")}.");
        }
    }
}
