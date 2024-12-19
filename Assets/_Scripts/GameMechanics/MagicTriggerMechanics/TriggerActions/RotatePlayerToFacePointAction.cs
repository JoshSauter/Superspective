using System;
using UnityEngine;

namespace MagicTriggerMechanics.TriggerActions {
    [Serializable]
    public class RotatePlayerToFacePointAction : TriggerAction {
        public Vector3 targetPosition;
        public bool useLocalCoordinates;
        
        public override void Execute(MagicTrigger triggerScript) {
            RotateToFaceTarget(triggerScript.transform);
        }
        
        public void RotateToFaceTarget(Transform triggerTransform) {
            Transform playerTransform = Player.instance.transform;
            Vector3 effectiveTargetPosition = useLocalCoordinates ? triggerTransform.TransformPoint(targetPosition) : targetPosition;
            
            // Step 1: Calculate the direction to the target
            Vector3 targetDirection = effectiveTargetPosition - playerTransform.position;

            // Step 2: Project target direction onto the plane perpendicular to the local up
            Vector3 projectedDirection = Vector3.ProjectOnPlane(targetDirection, playerTransform.up);

            // Step 3: Ensure the projected direction is valid
            if (projectedDirection.sqrMagnitude < 0.0001f) {
                Debug.LogWarning("Target is directly above or below the Player. Rotation is not possible.");
                return;
            }

            // Step 4: Determine the desired rotation
            Quaternion targetRotation = Quaternion.LookRotation(projectedDirection, playerTransform.up);

            // Step 5: Apply the rotation (along local up)
            playerTransform.rotation = Quaternion.RotateTowards(playerTransform.rotation, targetRotation, 360f);
        }
    }
}
