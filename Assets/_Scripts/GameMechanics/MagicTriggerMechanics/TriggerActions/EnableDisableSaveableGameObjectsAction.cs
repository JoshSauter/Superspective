using System;
using System.Linq;
using SerializableClasses;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class EnableDisableSaveableGameObjectsAction : TriggerAction {
        public SerializableReference[] objectsToEnable;
        public SerializableReference[] objectsToDisable;

        protected GameObject GameObjectFromReference(SerializableReference reference) {
            var maybeReference = reference.Reference;
            return maybeReference.isLeft
                ? maybeReference.LeftOrDefault().gameObject
                : throw new Exception($"Reference {maybeReference.RightOrDefault().ID} in scene {maybeReference.RightOrDefault().sceneName} is not loaded!");
        }

        protected void SetEnabled(bool enabled, SerializableReference[] objects, MagicTrigger triggerScript) {
            foreach (SerializableReference reference in objects) {
                GameObjectFromReference(reference).SetActive(enabled);
            }
        }
        
        public override void Execute(MagicTrigger triggerScript) {
            SetEnabled(true, objectsToEnable, triggerScript);
            SetEnabled(false, objectsToDisable, triggerScript);
        }

        [Serializable]
        class SaveData {
            public ActionTiming actionTiming;
            public bool[] objectsToEnableState;
            public bool[] objectsToDisableState;
        }
        
        public override object GetSaveData(MagicTrigger triggerScript) {
            return new SaveData {
                actionTiming = actionTiming,
                objectsToEnableState = objectsToEnable.Select(GameObjectFromReference).Select(go => go.activeSelf).ToArray(),
                objectsToDisableState = objectsToDisable.Select(GameObjectFromReference).Select(go => go.activeSelf).ToArray()
            };
        }
        
        public override void LoadSaveData(object saveData, MagicTrigger triggerScript) {
            SaveData data = (SaveData)saveData;
            actionTiming = data.actionTiming;
            for (int i = 0; i < objectsToEnable.Length; i++) {
                GameObjectFromReference(objectsToEnable[i]).SetActive(data.objectsToEnableState[i]);
            }
            for (int i = 0; i < objectsToDisable.Length; i++) {
                GameObjectFromReference(objectsToDisable[i]).SetActive(data.objectsToDisableState[i]);
            }
        }
    }
}
