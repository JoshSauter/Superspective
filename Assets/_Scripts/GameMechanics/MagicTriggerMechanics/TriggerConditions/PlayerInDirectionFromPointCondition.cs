using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerInDirectionFromPointCondition : ThresholdTriggerCondition {
        public bool useLocalCoordinates;
        public Vector3 targetDirection;
        public Vector3 targetPosition;
        
        protected override float Evaluate(Transform triggerTransform) {
            Vector3 effectiveTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
            Vector3 effectiveTargetPosition = (useLocalCoordinates) ? triggerTransform.TransformPoint(targetPosition) : targetPosition;
            Vector3 playerToPositionDirection = (Player.instance.transform.position - effectiveTargetPosition).normalized;
            return Vector3.Dot(effectiveTargetDirection.normalized, playerToPositionDirection);
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            Vector3 playerPos = (useLocalCoordinates ? transform.InverseTransformPoint(Player.instance.transform.position) : Player.instance.transform.position);
            Vector3 playerToTargetPosition = playerPos - targetPosition;
            string worldOrLocal = useLocalCoordinates ? "local" : "world";
            debugString += $"Player position ({worldOrLocal}): {playerPos}\nPlayer to target position: {playerToTargetPosition}\n";
            debugString += "--------\n";
            return debugString;
        }
    }
}
