using System;
using SuperspectiveUtils;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerOutsideOfColliderCondition : TriggerCondition {
        public Collider targetObject;
        
        protected override float Evaluate(Transform triggerTransform) {
            return targetObject.PlayerIsInCollider() ? 0 : 1;
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += $"Player in collider {targetObject.FullPath()}? {targetObject.PlayerIsInCollider()}\n";
            debugString += "--------\n";
            return debugString;
        }
    }
}
