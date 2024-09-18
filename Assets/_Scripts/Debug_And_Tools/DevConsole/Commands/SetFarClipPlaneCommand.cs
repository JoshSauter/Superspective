using System;

namespace DeveloperConsole {
    public class SetFarClipPlaneCommand : ConsoleCommand {
        private float originalFarClipPlane = -1;
        
        public SetFarClipPlaneCommand(string commandWord) : base(commandWord) { }
        
        public override CommandResponse Execute(string[] args) {
            // Parse the input and run the command here
            if (originalFarClipPlane < 0) {
                originalFarClipPlane = Player.instance.PlayerCam.farClipPlane;
            }

            if (args.Length < 1) {
                SuperspectiveScreen.instance.playerCamera.farClipPlane = originalFarClipPlane;
                SuperspectiveScreen.instance.portalMaskCamera.farClipPlane = originalFarClipPlane;
                SuperspectiveScreen.instance.dimensionCamera.farClipPlane = originalFarClipPlane;
                return new SuccessResponse($"Reset far clip plane to {originalFarClipPlane}");
            }
            
            if (float.TryParse(args[0], out float newFarClipPlane)) {
                SuperspectiveScreen.instance.playerCamera.farClipPlane = newFarClipPlane;
                SuperspectiveScreen.instance.portalMaskCamera.farClipPlane = newFarClipPlane;
                SuperspectiveScreen.instance.dimensionCamera.farClipPlane = newFarClipPlane;
                return new SuccessResponse($"Set far clip plane to {newFarClipPlane}");
            }
            else {
                return new FailureResponse($"Argument {args[0]} is not a valid float.");
            }
        }
    }
}
