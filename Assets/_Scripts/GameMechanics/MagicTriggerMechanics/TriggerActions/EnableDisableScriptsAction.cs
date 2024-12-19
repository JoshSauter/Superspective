using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class EnableDisableScriptsAction : TriggerAction {
        public MonoBehaviour[] scriptsToEnable;
        public MonoBehaviour[] scriptsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var script in scriptsToEnable) {
                script.enabled = true;
            }
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }
        }
        
        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool[] scriptsToEnableState;
            public bool[] scriptsToDisableState;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                scriptsToEnableState = Array.ConvertAll(scriptsToEnable, s => s.enabled),
                scriptsToDisableState = Array.ConvertAll(scriptsToDisable, s => s.enabled)
            };
        }
        
        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            for (int i = 0; i < scriptsToEnable.Length; i++) {
                scriptsToEnable[i].enabled = data.scriptsToEnableState[i];
            }
            for (int i = 0; i < scriptsToDisable.Length; i++) {
                scriptsToDisable[i].enabled = data.scriptsToDisableState[i];
            }
        }
    }
}
