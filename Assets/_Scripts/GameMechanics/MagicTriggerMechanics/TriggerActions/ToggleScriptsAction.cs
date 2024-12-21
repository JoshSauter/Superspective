using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleScriptsAction : EnableDisableScriptsAction {
        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var script in scriptsToEnable) {
                script.enabled = false;
            }
            foreach (var script in scriptsToDisable) {
                script.enabled = true;
            }
        }
    }
}
