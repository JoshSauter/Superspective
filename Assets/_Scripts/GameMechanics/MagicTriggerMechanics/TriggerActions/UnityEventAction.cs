using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class UnityEventAction : TriggerAction {
        [SerializeReference]
        public UnityEvent unityEvent;
        
        public override void Execute(MagicTrigger triggerScript) {
            unityEvent?.Invoke();
        }
    }
}
