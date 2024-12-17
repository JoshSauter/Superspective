using System;
using LevelManagement;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class PlayCameraFlythroughAction : TriggerAction {
        public Levels flythroughCameraLevel;
        
        public override void Execute(MagicTrigger triggerScript) {
            CameraFlythrough.instance.PlayForLevel(flythroughCameraLevel);
        }
    }
}
