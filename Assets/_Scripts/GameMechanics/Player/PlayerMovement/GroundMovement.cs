using System;
using System.Linq;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

// We use a custom struct instead of ContactPoint because we may want to manually create it in the case where there is
// no contact point, but the player is close enough to the ground to be considered grounded
public struct GroundContact {
    public Vector3 point;
    public Vector3 normal;
    public Collider collider;

    public GroundContact(Vector3 point, Vector3 normal, Collider collider) {
        this.point = point;
        this.normal = normal;
        this.collider = collider;
    }

    public GroundContact(ContactPoint contact) {
        point = contact.point;
        normal = contact.normal;
        
        Collider other = contact.thisCollider == PlayerMovement.instance.thisCollider ? contact.otherCollider : contact.thisCollider;
        collider = other;
    }
}

partial class PlayerMovement {
    [Serializable]
    public class GroundMovement : PlayerMovementComponent {
        public GroundMovement(PlayerMovement movement) : base(movement) { }
        
        [Serializable]
        public class GroundedState {
            [ShowInInspector]
            public bool IsGrounded {
                get => Ground != null;
                // Only allow setting isGrounded to false (clear the state)
                set {
                    if (value) {
                        Debug.LogError("Only allowed to set to false to clear state");
                        return;
                    }
                
                    contact = default;
                    lastGroundedTime = -1;
                }
            }

            public Collider Ground => contact.collider;
            public bool StandingOnHeldObject => IsStandingOnHeldObject(contact);

            public GroundContact contact;
            public float lastGroundedTime = -1;
        }

        private const float TIME_TO_WAIT_AFTER_LEAVING_GROUND = 0.05f;
        // Dot(face normal, transform.up) must be greater than this value to be considered "ground"
        public const float IS_GROUND_THRESHOLD = 0.675f;
        // Dot(face normal, transform.up) must be greater than this value to be for snap-to-ground to occur
        public const float SNAP_TO_GROUND_DOT_NORMAL_THRESHOLD = 0.88f;

        // Given as a ratio of the player's radius, how far we should check for ground in front/behind/left/right of the player if it's not directly underneath
        private const float BACKUP_GROUND_RAYCAST_DISTANCE = 0.25f;

        public string lastGroundTouchedDebug = "";
        public GroundedState grounded;
        public Vector3 lastGroundVelocity;
        
        public override void Init() {
            grounded = new GroundedState();
        }

        private float VerticalDistanceFromPlayer(Vector3 p) {
            Vector3 diff = m.BottomOfPlayer - p;
            return Vector3.Dot(diff, transform.up);
        }

        /// <summary>
        /// Performs a short distance raycast from the bottom of the player to look for potential ground.
        /// Ground must be within SNAP_TO_GROUND_DISTANCE units of the player's bottom, and must have a normal
        /// that is mostly aligned with the player's up vector.
        /// If a valid ground is found, will update the grounded state with the new ground contact.
        /// </summary>
        public void UpdateGroundedState() {
            if (m.jumpMovement.UnderMinJumpTime) {
                grounded.IsGrounded = false;
                return;
            }
            
            GroundContact groundContactPoint = default;
            bool isGroundedNow = false;
            if (GroundRaycast(new Ray(m.BottomOfPlayer + transform.up * m.Scale, -transform.up), (1 + SNAP_TO_GROUND_DISTANCE) * m.Scale, out RaycastHit closestHit, IS_GROUND_THRESHOLD, BackupRaycastMode.All)) {
                groundContactPoint = new GroundContact(closestHit.point, closestHit.normal, closestHit.collider);
                isGroundedNow = true;
            }
            
            // Was a ground object found?
            if (isGroundedNow) {
                grounded.contact = groundContactPoint;
                grounded.lastGroundedTime = Time.time;
            }
            // If we were grounded before, and we're not now, wait a little bit before saying that the player is ungrounded
            else if (grounded.IsGrounded && Time.time - grounded.lastGroundedTime > TIME_TO_WAIT_AFTER_LEAVING_GROUND) {
                grounded.IsGrounded = false;
            }
        }

        public enum BackupRaycastMode {
            None,
            ForwardOnly,
            All
        }
        
        /// <summary>
        /// Performs the desired raycast, and returns the closest valid ground hit if one is found.
        /// </summary>
        /// <param name="ray">Raycast origin + direction</param>
        /// <param name="distance">Raycast distance</param>
        /// <param name="hitInfo">If true, will contain the closest valid ground hit by the raycast. If false, will be default.</param>
        /// <returns>True if a valid ground is found, false otherwise.</returns>
        public bool GroundRaycast(Ray ray, float distance, out RaycastHit hitInfo, float dotNormalThreshold, BackupRaycastMode useBackups = BackupRaycastMode.None) {
            Vector3 GetCardinalPoint(Vector3 center, float offsetMagnitude, int directionIndex) {
                Vector3 up = transform.up;
                Vector3 forward = transform.forward;
                // Calculate right vector based on the up and forward vectors
                Vector3 right = Vector3.Cross(up, forward).normalized;
    
                // Ensure forward is perpendicular to up
                forward = Vector3.Cross(right, up).normalized;

                // Cardinal direction offsets
                Vector3[] directions = new Vector3[] {
                    forward,   // North (Forward)
                    -forward,  // South (Backward)
                    right,     // East  (Right)
                    -right     // West  (Left)
                };

                // Compute the new position
                return center + directions[directionIndex] * offsetMagnitude;
            }
            
            // Sometimes the player is not directly in contact with the ground, but is instead floating a little bit above it
            // We should check for this case instead of relying on contacts, and consider the player grounded if they are close enough to the ground

            // Returns true if a valid ground hit is found, and updates hitInfo with the closest valid hit
            bool AttemptRaycast(Vector3 raycastOrigin, out RaycastHit closestValidHit, Color debugColor) {
                Ray ray = new Ray(raycastOrigin, -transform.up);
                RaycastHit[] hits = Physics.RaycastAll(ray, distance, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask);
                if (hits.Length > 0) {
                    RaycastHit closestHit = hits.OrderBy(hit => hit.distance).First();
                    // Two conditions: The normal must be "up" facing, and the hit point must be below the player's bottom
                    bool upNormal = Vector3.Dot(closestHit.normal, transform.up) > dotNormalThreshold;
                    bool belowPlayer = (VerticalDistanceFromPlayer(closestHit.point) + 0.01f * m.Scale) >= 0;
                    if (upNormal && belowPlayer) {
                        closestValidHit = closestHit;

                        if (m.DEBUG) {
                            Debug.DrawLine(raycastOrigin, closestValidHit.point, debugColor);
                        }
                        return true;
                    }
                }
                
                closestValidHit = default;
                if (m.DEBUG) {
                    Debug.DrawRay(raycastOrigin, -transform.up * distance, Color.red);
                }
                return false;
            }

            float offsetMagnitude = m.PlayerRadius * BACKUP_GROUND_RAYCAST_DISTANCE;
            RaycastHit closestValidHit = default;
            switch (useBackups) {
                case BackupRaycastMode.None:
                    if (AttemptRaycast(ray.origin, out closestValidHit, Color.green)) {
                        hitInfo = closestValidHit;
                        return true;
                    }
                    break;
                case BackupRaycastMode.ForwardOnly:
                    if (AttemptRaycast(ray.origin, out closestValidHit, Color.green) || AttemptRaycast(GetCardinalPoint(ray.origin, offsetMagnitude, 0), out closestValidHit, Color.cyan)) {
                        hitInfo = closestValidHit;
                        return true;
                    }
                    break;
                case BackupRaycastMode.All:
                    if (AttemptRaycast(ray.origin, out closestValidHit, Color.green) || 
                        AttemptRaycast(GetCardinalPoint(ray.origin, offsetMagnitude, 0), out closestValidHit, Color.cyan) ||
                        AttemptRaycast(GetCardinalPoint(ray.origin, offsetMagnitude, 1), out closestValidHit, Color.blue) ||
                        AttemptRaycast(GetCardinalPoint(ray.origin, offsetMagnitude, 2), out closestValidHit, ColorExt.purple) ||
                        AttemptRaycast(GetCardinalPoint(ray.origin, offsetMagnitude, 3), out closestValidHit, Color.magenta)) {

                        hitInfo = closestValidHit;
                        return true;
                    }
                    break;
            }

            hitInfo = default;
            return false;
        }

        public Vector3 UpdateSnapToGround(Vector3 desiredVelocity) {
            if (m.pauseSnapToGround || !grounded.IsGrounded || !m.JumpReady || m.staircaseMovement.RecentlySteppedUpOrDown) return desiredVelocity;
            
            Vector3 horizontalVelocity = m.DecomposeVectorHorizontal(desiredVelocity);
            Vector3 rayOrigin = m.BottomOfPlayer + transform.up * m.Scale + horizontalVelocity * Time.fixedDeltaTime;
            
            if (GroundRaycast(new Ray(rayOrigin, -transform.up), (1 + SNAP_TO_GROUND_DISTANCE) * m.Scale, out RaycastHit hit, SNAP_TO_GROUND_DOT_NORMAL_THRESHOLD, BackupRaycastMode.ForwardOnly)) {
                // If we're snapping to the ground, then we are grounded on it. Refresh the grounded state framesWaited counter
                grounded.lastGroundedTime = Time.time;
                
                Vector3 beforePos = m.BottomOfPlayer;
                // Snap the player to the ground
                Vector3 offset = Vector3.Project(-(m.BottomOfPlayer - hit.point), transform.up);
                transform.Translate(offset, Space.World);
                Vector3 afterPos = m.BottomOfPlayer;

                if (offset.magnitude < 0.001f) return horizontalVelocity;
                
                m.debug.Log($"Before position: {beforePos}\nAfter position: {afterPos}\nOffset: {offset}");
            
                Debug.DrawRay(hit.point, hit.normal, Color.cyan, 0.15f);
                
                Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();

                return horizontalVelocity;
            }
            else {
                m.debug.Log($"No ground found to snap to. Current position: {m.BottomOfPlayer}");
                return desiredVelocity;
            }
        }

        public void UpdateLastGroundVelocity(Vector3 desiredVelocity) {
            if (grounded.IsGrounded && m.staircaseMovement.stepState == StaircaseMovement.StepState.Idle) {
                lastGroundVelocity = desiredVelocity;
            }
        }

        /// <summary>
        ///     Calculates player movement when the player is on (or close enough) to the ground.
        ///     Movement is perpendicular to the ground's normal vector.
        /// </summary>
        /// <param name="ground">RaycastHit info for the walkable object that passes the IsGrounded test</param>
        /// <returns>Desired Velocity according to current input</returns>
        public Vector3 CalculateGroundMovement(GroundContact ground) {
            Vector3 up = ground.normal;
            Vector3 right = Vector3.Cross(Vector3.Cross(up, transform.right), up);
            Vector3 forward = Vector3.Cross(Vector3.Cross(up, transform.forward), up);

            Vector3 moveDirection = forward * m.input.LeftStick.y + right * m.input.LeftStick.x;
            if (m.autoRun) moveDirection = forward + right * m.input.LeftStick.x;
            switch (m.endGameMovement) {
                case EndGameMovement.NotStarted:
                case EndGameMovement.Walking:
                    break;
                case EndGameMovement.HorizontalInputMovesPlayerForward:
                    moveDirection.x = Mathf.Abs(moveDirection.x);
                    break;
                case EndGameMovement.AllInputMovesPlayerForward:
                    moveDirection = Vector3.right * moveDirection.magnitude;
                    break;
                case EndGameMovement.AllInputDisabled:
                    moveDirection = Vector3.right;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // DEBUG:
            if (m.DEBUG) {
                //Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
                Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.blue, 0.1f);
            }

            // If no keys are pressed, decelerate to a stop
            if (!m.input.LeftStickHeld && !m.autoRun && m.endGameMovement != EndGameMovement.AllInputDisabled) {
                Vector3 horizontalVelocity = m.ProjectedHorizontalVelocity();
                Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                    horizontalVelocity,
                    Vector3.zero,
                    DECELERATION_LERP_SPEED * Time.fixedDeltaTime
                );
                return desiredHorizontalVelocity + (m.thisRigidbody.velocity - horizontalVelocity);
            }

            float adjustedMovespeed = ground.collider.CompareTag("Staircase") ? m.WalkSpeed : m.EffectiveMovespeed;
            return Vector3.Lerp(
                m.thisRigidbody.velocity,
                moveDirection * adjustedMovespeed,
                ACCELERATION_LERP_SPEED * Time.fixedDeltaTime
            );
        }

        public bool WalkingOnGlass() {
            if (grounded.IsGrounded == false) {
                return false;
            }

            // TODO: This is really inefficient and should be improved:
            bool onGlass = grounded
                .Ground
                .GetMaybeComponent<Renderer>()
                .Exists(r => r.sharedMaterials.Where(m => m != null).ToArray()[0].name.ToLower().Contains("glass"));

            return onGlass;
        }

        static bool IsStandingOnHeldObject(GroundContact contact) {
            PickupObject maybeCube = null;
            if (contact.collider != null) maybeCube = contact.collider.GetComponent<PickupObject>();
            bool cubeIsHeld = maybeCube != null && (maybeCube.isHeld || !maybeCube.IsGrounded());
            return cubeIsHeld;
        }
    }
}