using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleGameObjectsAction : TriggerAction {
        public GameObject[] objectsToEnable;
        public GameObject[] objectsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var obj in objectsToEnable) {
                obj.SetActive(true);
            }
            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var obj in objectsToEnable) {
                obj.SetActive(false);
            }
            foreach (var obj in objectsToDisable) {
                obj.SetActive(true);
            }
        }
        
        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool[] objectsToEnableState;
            public bool[] objectsToDisableState;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                objectsToEnableState = Array.ConvertAll(objectsToEnable, o => o.activeSelf),
                objectsToDisableState = Array.ConvertAll(objectsToDisable, o => o.activeSelf)
            };
        }

        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            for (int i = 0; i < objectsToEnable.Length; i++) {
                objectsToEnable[i].SetActive(data.objectsToEnableState[i]);
            }
            for (int i = 0; i < objectsToDisable.Length; i++) {
                objectsToDisable[i].SetActive(data.objectsToDisableState[i]);
            }
        }
    }
}
