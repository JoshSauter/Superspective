using System;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class UnityEventAction : TriggerAction {
        [NonSerialized, ShowInInspector] // Ensures UnityEvent uses Unity serialization
        public UnityEvent unityEvent;
        
        public override void Execute(MagicTrigger triggerScript) {
            unityEvent?.Invoke();
        }
    }
}
