using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

// This is super cool! You can split up a class into multiple files!
public partial class PlayerMovement {
    [Serializable]
    public class StepFound {
        public readonly Vector3 stepOffset; // Stepping along the direction of player movement
        public readonly Vector3 stepDestination;
        
        public StepFound(Vector3 stepOffset) {
            this.stepOffset = stepOffset;
            this.stepDestination = instance.BottomOfPlayer + stepOffset;
        }
    }
    
    [Serializable]
    public class StaircaseMovement : PlayerMovementComponent {
        public StaircaseMovement(PlayerMovement movement) : base(movement) { }

        // Staircase handling characteristics
        private const float STEP_COOLDOWN = 0.025f;
        private const float RECENTLY_STEPPED_UP_TIME = 0.25f;
        private const float MIN_STEP_HEIGHT = 0.1f;
        private const float MAX_STEP_HEIGHT = 0.6f;
        private const float STEP_OVERBITE_MAGNITUDE = 0.1f;
        private const float CAPSULE_CHECK_OFFSET_FROM_BOTTOM = 0.1f;
        // [0-1] How much of the player's movement direction vs the wall face normal should be used when calculating the step offset
        // Values closer to 0 will use the player's movement direction more, values closer to 1 will use the wall face normal more
        private const float MOVEMENT_VS_WALL_LERP_VALUE = 0.25f;
        // Stepping while on sloped surfaces doesn't work properly, so we only step if the face normal is mostly aligned with the player's up vector
        private const float STEP_NORMAL_THRESHOLD = 0.9f;

        // Properties
        public bool RecentlySteppedUpOrDown => stepState.State == StepState.SteppingDiagonal || stepState.Time < RECENTLY_STEPPED_UP_TIME;

        // How far do we move into the step before raycasting down?
        private float StepOverbiteMagnitude => STEP_OVERBITE_MAGNITUDE * m.Scale;//m.EffectiveMovespeed * Time.fixedDeltaTime;
        float MaxStepHeight => MAX_STEP_HEIGHT * m.Scale;
        float MinStepHeight => MIN_STEP_HEIGHT * m.Scale;
        float CapsuleCheckOffsetFromBottom => CAPSULE_CHECK_OFFSET_FROM_BOTTOM * m.Scale;
        
        float MinRaycastForwardDistance(float desiredSpeed) => m.PlayerRadius + desiredSpeed * Time.fixedDeltaTime;
        // Arbitrary amount more than the normal raycast forward distance. Larger multiplier == wider angle to allow stepping
        float MaxRaycastForwardDistance(float desiredSpeed) => 3f * MinRaycastForwardDistance(desiredSpeed);
        
        // Debug settings
        private const float DEBUG_RAY_DURATION_HIT = 4f;
        private const float DEBUG_RAY_DURATION_NO_HIT = 0.25f;
        private float GizmoSphereRadius => 0.01f * m.Scale;

        public enum StepState {
            Idle,
            Cooldown,
            SteppingDiagonal
        }

        [ShowIf(nameof(DEBUG))]
        public bool onlyDrawLatestDebug = false; // If true, the debug scene view ray draws will only show the latest raycast, otherwise all recent rays are drawn
        
        public StateMachine<StepState> stepState;
        private StepFound currentStep;

        public override void Init() {
            InitStaircaseStateMachine();
        }
        
        void InitStaircaseStateMachine() {
            stepState = m.StateMachine(StepState.Idle, true);
            
            // Transition from moving diagonally to being ready to find a new step once we've moved far enough
            stepState.AddStateTransition(StepState.Cooldown, StepState.Idle, STEP_COOLDOWN);
        
            stepState.AddTrigger(StepState.SteppingDiagonal, () => {
                if (!m.DEBUG) return;
                Debug.DrawRay(m.BottomOfPlayer, currentStep.stepOffset, Color.yellow);
            });
        }

        public void UpdateStaircase(Vector3 desiredVelocity) {
            Vector3 horizontalVelocity = m.ProjectHorizontalVelocity(desiredVelocity);
            if (GravityRotateZone.playerIsInAnyGravityRotateZone) return;
            
            switch (stepState.State) {
                case StepState.Idle:
                    LookForStep(horizontalVelocity);
                    break;
                case StepState.SteppingDiagonal:
                    MoveAlongStep();
                    break;
                case StepState.Cooldown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void MoveAlongStep() {
            if (currentStep == null) return;

            //if (CheckCapsuleAtOffset(m.thisCollider, currentStep.stepOffset)) {
            //    m.debug.Log("Collided with something while trying to step. Cancelling movement.");
            //    currentStep = null;
            //    stepState.Set(StepState.Cooldown);
            //    return;
            //}
            transform.Translate(currentStep.stepOffset, Space.World);
            m.ResolveCollisions(out Vector3 _);

            Player.instance.cameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
            m.OnStaircaseStep?.Invoke();
                
            stepState.Set(StepState.Cooldown);
        }

        private bool LookForStep(Vector3 horizontalVelocity) {
            currentStep = DetectStep(horizontalVelocity);
            if (currentStep != null) {
                //stepState.Set(StepState.SteppingVertical);
                stepState.Set(StepState.SteppingDiagonal);
            }

            return currentStep != null;
        }

        StepFound DetectStep(Vector3 horizontalVelocity) {
            // If player is not grounded, don't look for a step
            if (!m.IsGrounded) return null;

            // If player is not moving, don't look for a step
            if (horizontalVelocity.magnitude < 0.1f * m.Scale) return null;

            Vector3 playerPos = m.BottomOfPlayer;

            // Step 1: Shoot a raycast out from (almost) the bottom of the player in the direction of the desired velocity
            // The reason we're slightly above the bottom of the player is to account for the case where the player is very slightly sunken into the ground
            // The reason we're casting twice the player radius is because we want to be able to take steps when the player is not directly facing the step. We need to raycast further than you'd think to find potential steps in this case
            Vector3 raycastForwardOrigin = playerPos + transform.up * MinStepHeight;
            float minRaycastForwardDistance = MinRaycastForwardDistance(horizontalVelocity.magnitude);
            float maxRaycastForwardDistance = MaxRaycastForwardDistance(horizontalVelocity.magnitude);
            RaycastHit[] allHitInFrontOfPlayer = Physics.RaycastAll(raycastForwardOrigin, horizontalVelocity.normalized, maxRaycastForwardDistance, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask);
            RaycastHit[] allValidHitInFrontOfPlayer = allHitInFrontOfPlayer
                .Where(hit => Vector3.Dot(transform.up, hit.normal) <= (1 - STEP_NORMAL_THRESHOLD))
                // If this wall was found far from the player, it better be because the wall's normal doesn't line up with the player's movement direction
                // After all, that's the only reason we're raycasting this far in the first place
                .Where(hit => {
                    float movementNormalAlignment = Vector3.Dot(horizontalVelocity.normalized, -hit.normal.normalized);
                    if (movementNormalAlignment < 0) return false;
                    
                    float hitDistance = hit.distance;
                    // Distance threshold should vary between PlayerRadius + desiredVelocity.magnitude * Time.fixedDeltaTime and twice that, based on the movement alignment
                    float distanceThreshold = Mathf.Lerp(maxRaycastForwardDistance, minRaycastForwardDistance, movementNormalAlignment);
                    
                    return hitDistance <= distanceThreshold;
                })
                .ToArray();
            
            if (allValidHitInFrontOfPlayer.Length == 0) {
                if (m.DEBUG) {
                    float distance = allHitInFrontOfPlayer.Length > 0 ? allHitInFrontOfPlayer.Select(hit => hit.distance).Min() : maxRaycastForwardDistance;
                    Debug.DrawRay(raycastForwardOrigin, horizontalVelocity.normalized * distance, Color.red, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_NO_HIT);
                }
                
                //m.debug.Log($"We found nothing in front of us at {raycastForwardOrigin}, direction: {horizontalVelocity}");
                // If we didn't find a wall in front of us, we might be stepping down a staircase
                return DetectDownStep(horizontalVelocity);
            }
            
            RaycastHit wallInFrontOfPlayer = allValidHitInFrontOfPlayer.OrderBy(hit => hit.distance).First();
            Vector3 wallPointHorizontal = m.DecomposeVectorHorizontal(wallInFrontOfPlayer.point);
            Vector3 wallHitPoint = wallPointHorizontal + m.DecomposeVectorVertical(playerPos);
            
            // Find the closest point on the wall face to the player
            Vector3 wallNormal = wallInFrontOfPlayer.normal.normalized;
            // Project the player's position onto the wall's plane
            Vector3 closestPointOnWall = playerPos - Vector3.Dot(playerPos - wallHitPoint, wallNormal) * wallNormal;
            // Using the closest point on the wall feels too much like it's ignoring the player's movement direction, so we'll use this point instead
            Vector3 stepUpWallPoint = Vector3.Lerp(wallHitPoint, closestPointOnWall, MOVEMENT_VS_WALL_LERP_VALUE);

            if (m.DEBUG) {
                DebugDraw.Sphere($"DetectStep_raycastForwardHitPoint", wallHitPoint, GizmoSphereRadius, Color.green, DEBUG_RAY_DURATION_HIT);
                DebugDraw.Sphere($"DetectStep_closestPointOnWall", closestPointOnWall, GizmoSphereRadius, Color.blue, DEBUG_RAY_DURATION_HIT);
                DebugDraw.Sphere($"DetectStep_stepUpWallPoint", stepUpWallPoint, GizmoSphereRadius, ColorExt.purple, DEBUG_RAY_DURATION_HIT);
                Debug.DrawLine(raycastForwardOrigin, wallInFrontOfPlayer.point, Color.green, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                Debug.DrawLine(wallInFrontOfPlayer.point, wallHitPoint, Color.green, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                Debug.DrawLine(playerPos, closestPointOnWall, Color.blue, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                Debug.DrawLine(playerPos, stepUpWallPoint, ColorExt.purple, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
            }

            bool ValidateStep(RaycastHit[] allHits, out RaycastHit validStepHit) {
                RaycastHit[] allValidHits = allHits
                    .Where(hit => Vector3.Dot(transform.up, hit.normal) > STEP_NORMAL_THRESHOLD)
                    .Where(hit => {
                        Vector3 thisOffset = hit.point - playerPos;
                        float originalRadius = m.thisCollider.radius;
                        m.thisCollider.radius *= .75f; // Hack: Temporarily shrink the collider because collision resolution will handle minor overlap cases
                        bool overlap = CheckCapsuleAtOffset(m.thisCollider, thisOffset);
                        m.thisCollider.radius = originalRadius;
                        return !overlap;
                    })
                    .ToArray();

                bool validHitExists = allValidHits.Length > 0;
                validStepHit = validHitExists ? allValidHits.OrderBy(hit => hit.distance).First() : default;
                return validHitExists;
            }

            // Step 2: We will do up to three different raycasts to find a potential location to step to
            // Primary: Raycast from the stepUpWallPoint with the stepOverbite offset
            // Secondary: Raycast from the stepUpWallPoint with a backup offset based on the wall normal
            // Tertiary: Raycast from the closest point on the wall with the backup offset
            StringBuilder sb = new StringBuilder();
            bool IsValidStep(Vector3 wallOriginPoint, Vector3 stepOverbite, out RaycastHit validStepFound, Color debugColor, string debugName) {
                // Only used for debug drawing
                Vector3 intermediatePos = wallOriginPoint + transform.up * MaxStepHeight;
                Vector3 raycastOrigin = intermediatePos + stepOverbite;
                
                float raycastDistance = MaxStepHeight - MinStepHeight;
                RaycastHit[] allHits = Physics.RaycastAll(raycastOrigin, -transform.up, raycastDistance, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask);
                bool wasValidStepFound = ValidateStep(allHits, out validStepFound);

                if (m.DEBUG) {
                    if (wasValidStepFound) {
                        sb.AppendLine($"{debugName} attempt at {raycastOrigin:F3}, direction: {-transform.up * raycastDistance:F3} found a valid step!\n------------------------------\nValid step location: {validStepFound.point:F3}, Offset: {validStepFound.point - playerPos:F2}");
                        Debug.DrawLine(wallOriginPoint, intermediatePos, debugColor, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                        Debug.DrawLine(intermediatePos, raycastOrigin, debugColor, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                        Debug.DrawRay(raycastOrigin, -transform.up * MaxStepHeight, debugColor, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                        DebugDraw.Sphere("DetectStep_raycastDownHitPoint", validStepFound.point, GizmoSphereRadius, debugColor, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_HIT);
                    }
                    else {
                        sb.AppendLine($"{debugName} attempt failed at {raycastOrigin:F3}, direction: {-transform.up * raycastDistance:F3}");
                        Debug.DrawLine(wallOriginPoint, intermediatePos, Color.red, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_NO_HIT);
                        Debug.DrawLine(intermediatePos, raycastOrigin, Color.red, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_NO_HIT);
                        Debug.DrawRay(raycastOrigin, -transform.up * raycastDistance, Color.red, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_NO_HIT);
                        if (allHits.Length > 0) {
                            DebugDraw.Sphere("DetectStep_raycastDownHitPoint", allHits.OrderBy(hit => hit.distance).First().point, GizmoSphereRadius, Color.red, onlyDrawLatestDebug ? 0 : DEBUG_RAY_DURATION_NO_HIT);
                        }
                    }
                }
                return wasValidStepFound;
            }

            // O-1 value representing how much the player's movement direction aligns with the wall normal
            float movementNormalAlignment = Vector3.Dot(horizontalVelocity.normalized, -wallNormal);
            // If the player's movement is not aligned with the wall normal, we need to scale the step overbite such that the perpindicular distance to the edge of the step remains constant
            float stepOverbiteScalar = StepOverbiteMagnitude / movementNormalAlignment;
            Vector3 stepOverbite = horizontalVelocity.normalized * stepOverbiteScalar;
            if (!IsValidStep(stepUpWallPoint, stepOverbite, out RaycastHit validStepHit, Color.green, "Primary")) {
                Vector3 adjustmentDirection = (closestPointOnWall - stepUpWallPoint).normalized;
                // Backup step overbite based on the wall normal, brought slightly away from a potential obstacle to give player space to land (thus the adjustmentDirection component)
                Vector3 backupStepOverbite = -wallNormal * StepOverbiteMagnitude + adjustmentDirection * (0.01f * m.Scale);
                if (!IsValidStep(stepUpWallPoint, backupStepOverbite, out validStepHit, Color.cyan, "Secondary")) {
                    if (!IsValidStep(closestPointOnWall, backupStepOverbite, out validStepHit, Color.blue, "Tertiary")) {
                        m.debug.Log(sb.ToString());
                        return null;
                    }
                }
            }
            
            m.debug.Log(sb.ToString());

            // Step 3: Calculate the step offset and return the step
            Vector3 desiredStepPosition = validStepHit.point;
            Vector3 stepOffset = desiredStepPosition - playerPos;

            if (m.DEBUG) {
                //Debug.DrawLine(playerPos, desiredStepPosition, ColorExt.purple, 1f);
            }
            StepFound stepFound = new StepFound(stepOffset);

            return stepFound;
        }

        private StepFound DetectDownStep(Vector3 horizontalVelocity) {
            // return null; // TODO: There are too many issues with this as is, need to revisit and rethink the approach.
            Vector3 playerPos = m.BottomOfPlayer;
            
            // Since horizontalVelocity is already scaled by the player's scale, we'll use the unscaled constant STEP_OVERBITE_MAGNITUDE here
            Vector3 horizontalOffset = horizontalVelocity.normalized * STEP_OVERBITE_MAGNITUDE;
            // Start the raycast up by the maximum step height to make sure we're not going through the ground to find a step down
            Vector3 verticalOffset = transform.up * MaxStepHeight;
            Vector3 raycastDownOrigin = playerPos + horizontalOffset + verticalOffset;

            RaycastHit firstHit;
            bool wasAnyHit;
            RaycastHit PerformDownwardsRaycast(Vector3 raycastOrigin) {
                //m.debug.Log($"Looking for a down-step at {raycastOrigin}, direction: {-transform.up * (verticalOffset.magnitude + MaxStepHeight):F3}");

                wasAnyHit = Physics.Raycast(raycastOrigin, -transform.up, out firstHit, verticalOffset.magnitude + MaxStepHeight * 2f, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask);
                if (wasAnyHit) {
                    // Must have a very ground-like normal
                    bool validNormal = Vector3.Dot(transform.up, firstHit.normal) > STEP_NORMAL_THRESHOLD;
                    // Must be far enough away from the raycast start position to be considered a step downwards
                    bool validDistance = firstHit.distance > (verticalOffset.magnitude + MinStepHeight);
                    return validNormal && validDistance ? firstHit : default;
                }
                
                //m.debug.Log($"No hits found for down-step at {raycastOrigin}, direction: {-transform.up * (verticalOffset.magnitude + MaxStepHeight):F3}");
                return default;
            }

            RaycastHit validStepHit = PerformDownwardsRaycast(raycastDownOrigin);
            bool wasValidStepFound = validStepHit.distance > 0;

            if (m.DEBUG) {
                RaycastHit invalidStepFound = wasAnyHit ? firstHit : default;
                float distance = wasValidStepFound ? validStepHit.distance : (wasAnyHit ? invalidStepFound.distance : verticalOffset.magnitude + MaxStepHeight);
                Color color = wasValidStepFound ? Color.green : Color.red;
                float duration = wasValidStepFound ? 10f : .25f;
                Debug.DrawRay(raycastDownOrigin, -transform.up * distance, color, onlyDrawLatestDebug ? 0 : duration);
            }
            
            if (!wasValidStepFound) {
                //m.debug.Log($"We didn't find a down-step in front of us at {raycastDownOrigin}, direction: {-transform.up}");
                return null;
            }
            
            // Great, we found a valid down-step! Now we need to determine where to place the player so that they don't clip into the wall
            Vector3 raycastBackToPlayerOrigin = validStepHit.point + transform.up * MinStepHeight;

            // This will be modified whether we hit a wall or not, but is the starting point for the final desired player position
            Vector3 desiredStepPosition = validStepHit.point;
            float playerRadiusOffsetMagnitude = m.PlayerRadius;
            
            bool wasWallHitBackToPlayer = Physics.Raycast(raycastBackToPlayerOrigin, -horizontalOffset.normalized, out RaycastHit backToPlayerWallHit, horizontalOffset.magnitude, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask);
            wasWallHitBackToPlayer = wasWallHitBackToPlayer && Vector3.Dot(transform.up, backToPlayerWallHit.normal) <= (1 - STEP_NORMAL_THRESHOLD);
            if (wasWallHitBackToPlayer) {
                Vector3 wallHitPosition = backToPlayerWallHit.point;
                Vector3 wallNormal = backToPlayerWallHit.normal;
                
                // Project the player's position onto the wall's plane
                Vector3 closestPointOnWall = playerPos - Vector3.Dot(playerPos - wallHitPosition, wallNormal) * wallNormal;
                
                // Decompose wallHitPosition into vertical and horizontal components
                Vector3 wallHitHorizontal = m.DecomposeVectorHorizontal(wallHitPosition);

                // Decompose closestPointOnWall into vertical and horizontal components
                Vector3 closestPointHorizontal = m.DecomposeVectorHorizontal(closestPointOnWall);
                
                // Perform the horizontal lerp
                Vector3 stepDownWallHorizontal = Vector3.Lerp(wallHitHorizontal, closestPointHorizontal, MOVEMENT_VS_WALL_LERP_VALUE);

                // Use the vertical component from validStepHit.point
                Vector3 stepDownWallVertical = m.DecomposeVectorVertical(validStepHit.point);
                
                // Using the closest point on the wall feels too much like it's ignoring the player's movement direction, so we'll use this point instead
                Vector3 stepDownWallPoint = stepDownWallHorizontal + stepDownWallVertical;
                Vector3 finalPlacementHorizontalOffset = playerRadiusOffsetMagnitude * Vector3.Lerp(horizontalVelocity.normalized, wallNormal, MOVEMENT_VS_WALL_LERP_VALUE).normalized;
                desiredStepPosition = stepDownWallPoint + finalPlacementHorizontalOffset;
            }
            else {
                desiredStepPosition += playerRadiusOffsetMagnitude * horizontalVelocity.normalized;
            }

            if (m.DEBUG) {
                Vector3 destination = wasWallHitBackToPlayer ? backToPlayerWallHit.point : raycastBackToPlayerOrigin - horizontalOffset;
                Color color = wasWallHitBackToPlayer ? Color.magenta : Color.red;
                float duration = wasWallHitBackToPlayer ? 10f : .25f;
                Debug.DrawLine(raycastBackToPlayerOrigin, destination, color, onlyDrawLatestDebug ? 0 : duration);
            }
            
            
            // This check is important to make sure that we don't overshoot small steps (when the step width is less or equal to player's radius)
            Vector3 isSmallStepVerticalOffset = transform.up * MinStepHeight;
            Vector3 isSmallStepHorizontalOffset = horizontalVelocity * Time.fixedDeltaTime;
            if (!Physics.Raycast(desiredStepPosition + isSmallStepHorizontalOffset + isSmallStepVerticalOffset, -transform.up, isSmallStepVerticalOffset.magnitude + MinStepHeight, SuperspectivePhysics.PlayerPhysicsCollisionLayerMask)) {
                m.debug.Log("Wow this is a small step! Let's stop the player's velocity so we don't overshoot it.");
                m.thisRigidbody.velocity = Vector3.zero;
                desiredStepPosition -= horizontalVelocity * Time.fixedDeltaTime;
            }
            Vector3 stepOffset = desiredStepPosition - m.BottomOfPlayer;
            
            m.debug.Log($"Found desired down-step at: {desiredStepPosition}\nOffset: {stepOffset:F3}");
            
            if (m.DEBUG) {
                Debug.DrawLine(playerPos, desiredStepPosition, ColorExt.purple, onlyDrawLatestDebug ? 0 : 1f);
            }
            
            // TODO: Backup offset for down-stepping?
            return new StepFound(stepOffset);
        }
        
        /// <summary>
        /// Checks if the capsule collider at the given global offset is colliding with anything in the player's interactable layer mask.
        /// </summary>
        /// <param name="capsuleCollider"></param>
        /// <param name="globalOffset"></param>
        /// <returns>True if the capsule collider is colliding with something, false otherwise.</returns>
        private bool CheckCapsuleAtOffset(CapsuleCollider capsuleCollider, Vector3 globalOffset) {
            // Get the relevant properties from the CapsuleCollider
            Vector3 capsuleCenter = transform.TransformPoint(capsuleCollider.center);
            float capsuleHeight = capsuleCollider.height * m.Scale;
            float capsuleRadius = capsuleCollider.radius * m.Scale;

            Vector3 capsuleAxis = transform.up;
            float halfHeight = (capsuleHeight * 0.5f) - capsuleRadius;
            Vector3 p1 = capsuleCenter - capsuleAxis * halfHeight + globalOffset;
            Vector3 p2 = capsuleCenter + capsuleAxis * halfHeight + globalOffset;

            Vector3 adjustedP1 = p1 + capsuleAxis * CapsuleCheckOffsetFromBottom;


            bool overlap = false;
            if (Player.instance.IsHoldingSomething) {
                int prevHeldObjLayer = Player.instance.heldObject.gameObject.layer;
                Player.instance.heldObject.gameObject.layer = SuperspectivePhysics.VisibleButNoPlayerCollisionLayer;
                overlap = Physics.CheckCapsule(adjustedP1, p2, capsuleRadius, Player.instance.interactsWithPlayerLayerMask, QueryTriggerInteraction.Ignore);
                Player.instance.heldObject.gameObject.layer = prevHeldObjLayer;
            }
            else {
                overlap = Physics.CheckCapsule(adjustedP1, p2, capsuleRadius, Player.instance.interactsWithPlayerLayerMask, QueryTriggerInteraction.Ignore);
            }
            
            if (m.DEBUG) {
                DebugDraw.Sphere("StaircaseMovement_P1", adjustedP1, capsuleRadius, overlap ? Color.red : Color.green, overlap ? 4f : 0.125f);
                DebugDraw.Sphere("StaircaseMovement_P2", p2, capsuleRadius, overlap ? Color.red : Color.green, overlap ? 4f : 0.125f);
            }

            return overlap;
        }
    }
}
