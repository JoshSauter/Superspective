using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleCollidersAction : TriggerAction {
        [NonSerialized, ShowInInspector] // Fixes serialization in inspector issue
        public Collider[] collidersToEnable;
        [NonSerialized, ShowInInspector]
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
    }
}
