using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerFacingDirectionCondition : ThresholdTriggerCondition {
        public bool useLocalCoordinates;
        public Vector3 targetDirection;
        
        protected override float Evaluate(Transform triggerTransform) {
            Transform cameraTransform = SuperspectiveScreen.instance.playerCamera.transform;
            Vector3 effectiveTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
            return Vector3.Dot(cameraTransform.forward, effectiveTargetDirection.normalized);
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            string worldOrLocal = useLocalCoordinates ? "local" : "world";
            debugString += $"Player facing direction ({worldOrLocal}): {(useLocalCoordinates ? transform.TransformDirection(targetDirection) : targetDirection):F3}";
            debugString += "--------\n";
            return debugString;
        }
    }
}
