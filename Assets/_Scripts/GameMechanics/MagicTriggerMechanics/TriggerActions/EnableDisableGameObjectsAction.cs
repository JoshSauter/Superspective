using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class EnableDisableGameObjectsAction : TriggerAction {
        [NonSerialized, ShowInInspector] // Fixes serialization in inspector issue
        public GameObject[] objectsToEnable;
        [NonSerialized, ShowInInspector]
        public GameObject[] objectsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var obj in objectsToEnable) {
                obj.SetActive(true);
            }
            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }
        }
    }
}
