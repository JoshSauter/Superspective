using System;

namespace DeveloperConsole {
    public class HideCullEverythingLayerCommand : ConsoleCommand {
        public HideCullEverythingLayerCommand(string commandWord) : base(commandWord) { }

        private bool layerIsHidden = false;
        public override CommandResponse Execute(string[] args) {
            layerIsHidden = !layerIsHidden;

            if (layerIsHidden) {
                Player.instance.PlayerCam.cullingMask &= ~(1 << SuperspectivePhysics.CullEverythingLayer);
                return new SuccessResponse("CullEverythingLayer is hidden");
            }
            else {
                Player.instance.PlayerCam.cullingMask |= (1 << SuperspectivePhysics.CullEverythingLayer);
                return new SuccessResponse("CullEverythingLayer restored");
            }
        }
    }
}
