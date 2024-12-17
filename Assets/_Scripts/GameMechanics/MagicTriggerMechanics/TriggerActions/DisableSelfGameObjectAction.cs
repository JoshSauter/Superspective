using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class DisableSelfGameObjectAction : TriggerAction {
        public override void Execute(MagicTrigger triggerScript) {
            triggerScript.gameObject.SetActive(false);
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            triggerScript.gameObject.SetActive(true);
        }
    }
}
