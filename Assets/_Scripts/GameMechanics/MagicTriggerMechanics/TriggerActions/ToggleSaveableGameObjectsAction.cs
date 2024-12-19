using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleSaveableGameObjectsAction : EnableDisableSaveableGameObjectsAction {
        public override void NegativeExecute(MagicTrigger triggerScript) {
            SetEnabled(false, objectsToEnable, triggerScript);
            SetEnabled(true, objectsToDisable, triggerScript);
        }
    }
}
