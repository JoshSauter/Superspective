using System;
using UnityEngine;

namespace DeveloperConsole {
    public class SetGravityCommand : ConsoleCommand {
        public SetGravityCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            if (args.Length < 1) {
                Physics.gravity = SuperspectivePhysics.originalGravity;
                return new SuccessResponse($"Reset gravity to {Physics.gravity:F2}");
            }
            
            if (float.TryParse(args[0], out float newGravityMagnitude)) {
                Vector3 newGravity = SuperspectivePhysics.originalGravity.normalized * newGravityMagnitude;
                Physics.gravity = newGravity;
                return new SuccessResponse($"Set gravity to {newGravity}");
            }
            else {
                return new FailureResponse($"Argument {args[0]} is not a valid float.");
            }
        }
    }
}
