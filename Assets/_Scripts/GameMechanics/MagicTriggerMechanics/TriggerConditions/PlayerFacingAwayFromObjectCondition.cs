using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerConditions {
    [Serializable]
    public class PlayerFacingAwayFromObjectCondition : ThresholdTriggerCondition {
        public Collider targetObject;
        
        protected override float Evaluate(Transform triggerTransform) {
            Vector3 camPos = Cam.Player.CamPos();
            Vector3 camDirection = Cam.Player.CamDirection();
            
            Vector3 playerToObjectVector = (camPos - targetObject.ClosestPointOnBounds(camPos)).normalized;
            // TODO: Handle player being inside of object as a "always false" case
            bool insideTargetObject = false;
            return insideTargetObject ? -1 : Vector3.Dot(camDirection, playerToObjectVector);
        }
        
        public override string GetDebugInfo(Transform transform) {
            string debugString = base.GetDebugInfo(transform);
            debugString += "--------\n";
            return debugString;
        }
    }
}
