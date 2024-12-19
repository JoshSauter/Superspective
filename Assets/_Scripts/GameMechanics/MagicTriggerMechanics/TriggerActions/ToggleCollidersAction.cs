using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleCollidersAction : TriggerAction {
        public Collider[] collidersToEnable;
        public Collider[] collidersToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var col in collidersToEnable) {
                col.enabled = true;
            }
            foreach (var col in collidersToDisable) {
                col.enabled = false;
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var col in collidersToEnable) {
                col.enabled = false;
            }
            foreach (var col in collidersToDisable) {
                col.enabled = true;
            }
        }
        
        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool[] collidersToEnableState;
            public bool[] collidersToDisableState;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                collidersToEnableState = Array.ConvertAll(collidersToEnable, c => c.enabled),
                collidersToDisableState = Array.ConvertAll(collidersToDisable, c => c.enabled)
            };
        }

        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            for (int i = 0; i < collidersToEnable.Length; i++) {
                collidersToEnable[i].enabled = data.collidersToEnableState[i];
            }
            for (int i = 0; i < collidersToDisable.Length; i++) {
                collidersToDisable[i].enabled = data.collidersToDisableState[i];
            }
        }
    }
}
