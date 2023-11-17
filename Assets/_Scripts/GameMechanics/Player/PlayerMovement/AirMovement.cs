using UnityEngine;

partial class PlayerMovement {
    public class AirMovement : PlayerMovementComponent {
        public AirMovement(PlayerMovement movement) : base(movement) { }
        
        public override void Init() { }

        /// <summary>
        ///     Handles player movement when the player is in the air.
        ///     Movement is perpendicular to Vector3.up.
        /// </summary>
        /// <returns>Desired Velocity according to current input</returns>
        public Vector3 CalculateAirMovement() {
            Vector3 moveDirection = m.input.LeftStick.y * transform.forward + m.input.LeftStick.x * transform.right;
            if (m.autoRun) moveDirection = transform.forward;

            // DEBUG:
            Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

            // Handle mid-air collision with obstacles
            moveDirection = AirCollisionMovementAdjustment(moveDirection * m.effectiveMovespeed);

            // If no keys are pressed, decelerate to a horizontal stop
            if (!m.input.LeftStickHeld && !m.autoRun) {
                Vector3 horizontalVelocity = m.ProjectedHorizontalVelocity();
                Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                    horizontalVelocity,
                    Vector3.zero,
                    decelerationLerpSpeed * Time.fixedDeltaTime
                );
                return desiredHorizontalVelocity + (m.thisRigidbody.velocity - horizontalVelocity);
            }
            else {
                Vector3 horizontalVelocity = m.ProjectedHorizontalVelocity();
                Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                    horizontalVelocity,
                    moveDirection,
                    airspeedControlFactor * accelerationLerpSpeed * Time.fixedDeltaTime
                );
                return desiredHorizontalVelocity + (m.thisRigidbody.velocity - horizontalVelocity);
            }
        }

        /// <summary>
        ///     Checks the area in front of where the player wants to move for an obstacle.
        ///     If one is found, adjusts the player's movement to be parallel to the obstacle's face.
        /// </summary>
        /// <param name="movementVector"></param>
        /// <returns>True if there is something in the way of the player's desired movement vector, false otherwise.</returns>
        Vector3 AirCollisionMovementAdjustment(Vector3 movementVector) {
            float rayDistance = m.effectiveMovespeed * Time.fixedDeltaTime + m.thisCollider.radius * m.scale;
            RaycastHit obstacle = new RaycastHit();
            Physics.Raycast(transform.position, movementVector, out obstacle, rayDistance, Player.instance.interactsWithPlayerLayerMask);
            
            if (obstacle.collider == null || obstacle.collider.isTrigger ||
                (obstacle.collider.gameObject.GetComponent<PickupObject>()?.isHeld ?? false)) {
                return movementVector;
            }

            Vector3 newMovementVector = Vector3.ProjectOnPlane(movementVector, obstacle.normal);
            if (Vector3.Dot(m.ProjectedVerticalVelocity(), newMovementVector) > 0) {
                m.debug.LogWarning("movementVector:" + movementVector + "\nnewMovementVector:" + newMovementVector);
            }

            return newMovementVector;
        }
    }
}