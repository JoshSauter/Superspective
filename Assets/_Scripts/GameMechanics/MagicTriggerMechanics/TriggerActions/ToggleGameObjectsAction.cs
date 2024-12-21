using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleGameObjectsAction : EnableDisableGameObjectsAction {
        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var obj in objectsToEnable) {
                obj.SetActive(false);
            }
            foreach (var obj in objectsToDisable) {
                obj.SetActive(true);
            }
        }
    }
}
