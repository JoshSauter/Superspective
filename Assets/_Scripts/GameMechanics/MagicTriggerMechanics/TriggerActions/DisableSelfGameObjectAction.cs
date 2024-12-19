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

        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool isActive;
        }

        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                isActive = triggerScript.gameObject.activeSelf
            };
        }
        
        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            var data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            triggerScript.gameObject.SetActive(data.isActive);
        }
    }
}
