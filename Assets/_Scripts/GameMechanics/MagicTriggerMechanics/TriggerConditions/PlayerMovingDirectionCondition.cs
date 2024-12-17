using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerMovingDirectionCondition : ThresholdTriggerCondition {
        public bool useLocalCoordinates;
        public Vector3 targetDirection;
        
        protected override float Evaluate(Transform triggerTransform) {
            Vector3 effectiveTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
            return Vector3.Dot(Player.instance.movement.CurVelocity.normalized, effectiveTargetDirection.normalized);
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
