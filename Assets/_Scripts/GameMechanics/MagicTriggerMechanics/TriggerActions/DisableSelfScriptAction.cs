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
        
        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool isEnabled;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                isEnabled = triggerScript.enabled
            };
        }

        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            triggerScript.enabled = data.isEnabled;
        }
    }
}
