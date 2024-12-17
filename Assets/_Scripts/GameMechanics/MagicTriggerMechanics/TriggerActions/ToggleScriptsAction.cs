using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class ToggleScriptsAction : TriggerAction {
        [NonSerialized, ShowInInspector] // Fixes serialization in inspector issue
        public MonoBehaviour[] scriptsToEnable;
        [NonSerialized, ShowInInspector]
        public MonoBehaviour[] scriptsToDisable;
        
        public override void Execute(MagicTrigger triggerScript) {
            foreach (var script in scriptsToEnable) {
                script.enabled = true;
            }
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }
        }

        public override void NegativeExecute(MagicTrigger triggerScript) {
            foreach (var script in scriptsToEnable) {
                script.enabled = false;
            }
            foreach (var script in scriptsToDisable) {
                script.enabled = true;
            }
        }
    }
}
