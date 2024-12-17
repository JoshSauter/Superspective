using System;
using LevelManagement;
using Sirenix.OdinInspector;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class PlayLevelChangeBannerAction : TriggerAction {
        public bool onlyTriggerForward;
        public Levels levelForward;
        [HideIf(nameof(onlyTriggerForward))]
        public Levels levelBackward;
        
        public override void Execute(MagicTrigger triggerScript) {
            // ManagerScene is a flag that we don't want to play level banner in this direction
            if (levelForward != Levels.ManagerScene) {
                LevelChangeBanner.instance.PlayBanner(levelForward);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            if (onlyTriggerForward) return;
            
            // ManagerScene is a flag that we don't want to play level banner in this direction
            if (levelBackward != Levels.ManagerScene) {
                LevelChangeBanner.instance.PlayBanner(levelBackward);
            }
        }
    }
}
