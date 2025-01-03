using System;
using System.Linq;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class EnableDisableGameObjectsAction : TriggerAction {
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
        
        [Serializable]
        public class SaveData {
            public ActionTiming actionTiming;
            public bool[] objectsToEnableState;
            public bool[] objectsToDisableState;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData() {
                actionTiming = actionTiming,
                objectsToEnableState = objectsToEnable.Select(o => o.activeSelf).ToArray(),
                objectsToDisableState = objectsToDisable.Select(o => o.activeSelf).ToArray()
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
