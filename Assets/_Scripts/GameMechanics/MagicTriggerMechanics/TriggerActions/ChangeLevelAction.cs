using System;
using LevelManagement;
using Sirenix.OdinInspector;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ChangeLevelAction : TriggerAction {
        public bool onlyTriggerForward;
        public Levels levelForward;
        [HideIf(nameof(onlyTriggerForward))]
        public Levels levelBackward;
        
        public override void Execute(MagicTrigger triggerScript) {
            // ManagerScene is a flag that we don't want to change level in this direction
            if (levelForward != Levels.ManagerScene && LevelManager.instance.ActiveScene != levelForward) {
                LevelManager.instance.SwitchActiveScene(levelForward);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            if (onlyTriggerForward) return;
            
            // ManagerScene is a flag that we don't want to change level in this direction
            if (levelBackward != Levels.ManagerScene && LevelManager.instance.ActiveScene != levelBackward) {
                LevelManager.instance.SwitchActiveScene(levelBackward);
            }
        }
    }
}
