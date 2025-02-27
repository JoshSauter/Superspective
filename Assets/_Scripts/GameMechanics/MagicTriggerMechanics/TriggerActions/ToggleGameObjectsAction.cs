using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleGameObjectsAction : EnableDisableGameObjectsAction {
        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var obj in objectsToEnable) {
                if (obj == null) {
                    triggerScript.debug.LogError($"Missing reference in {triggerScript.ID}!", true);
                    continue;
                }
                obj.SetActive(false);
            }
            foreach (var obj in objectsToDisable) {
                if (obj == null) {
                    triggerScript.debug.LogError($"Missing reference in {triggerScript.ID}!", true);
                    continue;
                }
                obj.SetActive(true);
            }
        }
    }
}
