using System;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class DisableSelfScriptAction  : TriggerAction {
        public override void Execute(MagicTrigger triggerScript) {
            triggerScript.enabled = false;
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            triggerScript.enabled = true;
        }
    }
}
