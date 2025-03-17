using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class UnityEventAction : TriggerAction {
        [SerializeReference]
        public UnityEvent unityEvent;

        public bool onlyTriggerForwards = true;
        [HideIf(nameof(onlyTriggerForwards)), SerializeReference]
        public UnityEvent unityEventBackwards;
        
        public override void Execute(MagicTrigger triggerScript) {
            unityEvent?.Invoke();
        }
        
        public override void NegativeExecute(MagicTrigger triggerScript) {
            if (onlyTriggerForwards) return;
            unityEventBackwards?.Invoke();
        }
    }
}
