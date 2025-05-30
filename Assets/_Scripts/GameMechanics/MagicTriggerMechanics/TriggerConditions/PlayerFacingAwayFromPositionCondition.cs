using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerFacingAwayFromPositionCondition : ThresholdTriggerCondition {
        public Vector3 targetPosition;
        
        protected override float Evaluate(Transform triggerTransform) {
            Vector3 camPos = Cam.Player.CamPos();
            Vector3 camDirection = Cam.Player.CamDirection();
            
            Vector3 positionToPlayer = (camPos - targetPosition).normalized;
            return Vector3.Dot(camDirection, positionToPlayer);
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
