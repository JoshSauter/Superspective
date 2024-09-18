using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using StateUtils;

// This is super cool! You can split up a class into multiple files!
public partial class PlayerMovement {
    [Serializable]
    public class StepFound {
        public ContactPoint contact;
        public readonly Vector3 contactNormal;
        public readonly Vector3 stepOffset;

        private static Transform Player => PlayerMovement.instance.transform;
        public Vector3 StepOffsetVertical => Vector3.Dot(stepOffset, Player.up) * Player.up;
        public Vector3 StepOffsetHorizontal => Vector3.Dot(stepOffset, -contactNormal) * -contactNormal;

        public StepFound(ContactPoint contact, Vector3 contactNormal, Vector3 stepOffset) {
            this.contact = contact;
            this.contactNormal = contactNormal;
            this.stepOffset = stepOffset;
        }
    }
    
    [Serializable]
    public class StaircaseMovement : PlayerMovementComponent {
        public StaircaseMovement(PlayerMovement movement) : base(movement) { }

        // Staircase handling characteristics
        private const float RECENTLY_STEPPED_UP_TIME = 0.25f;
        private const float MAX_STEP_HEIGHT = 0.6f;
        private const float MIN_STEP_HEIGHT = 0.1f;
        private const float STEP_SPEED_MULTIPLIER = 10f;
        private const float CAPSULE_CHECK_OFFSET_FROM_BOTTOM = 0.5f;

        // Properties
        public bool RecentlySteppedUp => stepState.State == StepState.SteppingDiagonal || stepState.Time < RECENTLY_STEPPED_UP_TIME;
        [ShowNativeProperty]
        private Vector3 CurrentStepUp => currentStep?.StepOffsetVertical + (transform.up * 0.01f) ?? Vector3.zero;
        [ShowNativeProperty]
        // Move in the direction of player desired movement, with the distance == the radius of the player
        private Vector3 CurrentStepForward => m.ProjectHorizontalVelocity(m.groundMovement.lastGroundVelocity.normalized) * (m.thisCollider == null ? 0 : m.thisCollider.radius * m.Scale);
        [ShowNativeProperty]
        private Vector3 CurrentStepDiagonal => (CurrentStepUp + CurrentStepForward);
        
        private float StepSpeed => m.EffectiveMovespeed * STEP_SPEED_MULTIPLIER; // _stepSpeed * (1 + Mathf.InverseLerp(movespeed, walkSpeed, runSpeed));

        // How far do we move into the step before raycasting down?
        float StepOverbiteMagnitude => m.EffectiveMovespeed * Time.fixedDeltaTime;
        float MaxStepHeight => MAX_STEP_HEIGHT * m.Scale;
        float MinStepHeight => MIN_STEP_HEIGHT * m.Scale;

        // State
        [ShowNonSerializedField]
        private float distanceMovedForStaircaseOffset = 0;

        public enum StepState {
            Idle,
            SteppingDiagonal
        }

        public StateMachine<StepState> stepState;
        private StepFound currentStep;

        public override void Init() {
            InitStaircaseStateMachine();
        }
        
        void InitStaircaseStateMachine() {
            stepState = m.StateMachine(StepState.Idle, true);
            
            // Transition from moving diagonally to being ready to find a new step once we've moved far enough
            stepState.AddStateTransition(StepState.SteppingDiagonal, StepState.Idle, () =>
                distanceMovedForStaircaseOffset >= CurrentStepDiagonal.magnitude
            );
        
            stepState.AddTrigger(StepState.SteppingDiagonal, () => Debug.DrawRay(m.BottomOfPlayer, CurrentStepDiagonal, Color.yellow));
        
            // Reset distance moved whenever we change state
            stepState.OnStateChangeSimple += () => distanceMovedForStaircaseOffset = 0f;
        }

        public void UpdateStaircase(Vector3 desiredVelocity) {
            // Staircase stepping while in staircase rotate zone caused gravity direction bugs. TODO: Make this just work
            if (StaircaseRotate.playerIsInAnyStaircaseRotateZone) return;
            
            bool CheckCapsule(CapsuleCollider capsuleCollider) {
                // Get the relevant properties from the CapsuleCollider
                Vector3 capsuleCenter = transform.TransformPoint(capsuleCollider.center);
                float capsuleHeight = capsuleCollider.height * m.Scale;
                float capsuleRadius = capsuleCollider.radius * m.Scale;

                Vector3 capsuleAxis = transform.up;
                Vector3 p1 = capsuleCenter - capsuleAxis * (capsuleHeight * (0.5f - capsuleRadius));
                Vector3 p2 = capsuleCenter + (capsuleAxis * (capsuleHeight * 0.5f - capsuleRadius));

                float distanceFromCapsuleCenterToStepPoint = Vector3.Dot(capsuleCenter - currentStep.contact.point, capsuleAxis);
                float distanceFromCapsuleCenterToP1 = Vector3.Dot(capsuleCenter - p1, capsuleAxis);

                float distanceFromCapsuleCenterToOffsetStartPoint = Mathf.Min(distanceFromCapsuleCenterToStepPoint, distanceFromCapsuleCenterToP1);
                p1 = capsuleCenter - capsuleAxis * (distanceFromCapsuleCenterToOffsetStartPoint - CAPSULE_CHECK_OFFSET_FROM_BOTTOM * m.Scale);
                
                var result = Physics.OverlapCapsule(p1, p2, capsuleRadius, Player.instance.interactsWithPlayerLayerMask, QueryTriggerInteraction.Ignore);

                bool FilterOutHeldObjects(Collider c) {
                    if (Player.instance.IsHoldingSomething) {
                        return c != Player.instance.heldObject.thisCollider;
                    }

                    return true;
                }
                return result.Where(FilterOutHeldObjects).ToList().Count > 0;
            }
            
            void MoveAlongStep(Vector3 moveDirection) {
                // How much distance do we actually have left to go
                float distanceRemaining = moveDirection.magnitude - distanceMovedForStaircaseOffset;
                // Either set the distance to move to full speed or whatever's left to move
                float distanceToMove = Mathf.Min(StepSpeed * Time.fixedDeltaTime, distanceRemaining);
                Vector3 diff = moveDirection.normalized * distanceToMove;
                
                m.debug.LogWarning($"Offset this frame: {diff:F3}");
                // Move the player up, record the distance moved
                transform.Translate(diff, Space.World);
                // If we move into colliding w/ something else undo it
                if (CheckCapsule(m.thisCollider)) {
                    transform.Translate(-diff, Space.World);
                }
                distanceMovedForStaircaseOffset += distanceToMove;

                if (distanceRemaining == distanceToMove) {
                    stepState.Set(StepState.Idle);
                    LookForStep(desiredVelocity);
                }
            }
            
            switch (stepState.State) {
                case StepState.Idle:
                    LookForStep(desiredVelocity);
                    break;
                case StepState.SteppingDiagonal:
                    MoveAlongStep(CurrentStepDiagonal);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool LookForStep(Vector3 desiredVelocity) {
            currentStep = DetectStep(desiredVelocity, m.Grounded.contact, m.IsGrounded);
            if (currentStep != null) {
                //stepState.Set(StepState.SteppingVertical);
                stepState.Set(StepState.SteppingDiagonal);
                Player.instance.cameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
                if (Vector3.Dot(transform.up, currentStep.stepOffset) > 0) m.OnStaircaseStepUp?.Invoke();
            }

            return currentStep != null;
        }

        
        StepFound DetectStep(Vector3 desiredVelocity, ContactPoint ground, bool isGrounded) {
            // If player is not moving, don't do any raycasts, just return
            if (desiredVelocity.magnitude < 0.1f) return null;

            foreach (ContactPoint contact in m.allContactThisFrame) {
                if (contact.otherCollider == null) continue;

                float stepHeight = Mathf.Abs(Vector3.Dot(contact.point, transform.up) - Vector3.Dot(ground.point, transform.up));
                bool isBelowMaxStepHeight = stepHeight < MAX_STEP_HEIGHT;
                // Basically all this nonsense is to get the contact surface's normal rather than the "contact normal" which is different
                RaycastHit hitInfo = default;
                bool rayHit = false;
                Vector3 contactNormal = contact.normal;
                if (isBelowMaxStepHeight) {
                    Vector3 rayLowStartPos = m.BottomOfPlayer + transform.up * 0.01f;
                    Vector3 bottomOfPlayerToContactPoint = contact.point - m.BottomOfPlayer;
                    Vector3 rayDirection = Vector3.ProjectOnPlane(bottomOfPlayerToContactPoint, transform.up).normalized;
                    if (rayDirection.magnitude > 0) {
                        Debug.DrawRay(rayLowStartPos, rayDirection * (m.thisCollider.radius * 2), Color.blue);
                        rayHit = contact.otherCollider.Raycast(
                            new Ray(rayLowStartPos, rayDirection),
                            out hitInfo,
                            m.thisCollider.radius * 2
                        );
                        contactNormal = hitInfo.normal;
                    }
                    else {
                        rayHit = false;
                        hitInfo = default;
                    }
                }

                bool isWallNormal = rayHit && Mathf.Abs(Vector3.Dot(contactNormal, transform.up)) < 0.1f;
                bool isInDirectionOfMovement = rayHit && Vector3.Dot(-contactNormal, desiredVelocity.normalized) > 0f;
                //if (ground.otherCollider == null || contact.otherCollider.gameObject != ground.otherCollider.gameObject) {
                //	float t = Vector3.Dot(-hitInfo.normal, desiredVelocity.normalized);
                //	if (Mathf.Abs(t) > 0.1f) {
                //		Debug.LogWarning(t);
                //	}
                //}

                StepFound step;
                if (isBelowMaxStepHeight && isWallNormal && isInDirectionOfMovement &&
                    GetStepInfo(out step, contact, hitInfo.normal, ground, isGrounded)) return step;
            }

            return null;
        }

        bool GetStepInfo(out StepFound step, ContactPoint contact, Vector3 contactNormal, ContactPoint ground, bool isGrounded) {
            step = null;
            RaycastHit stepTest;

            Vector3 stepOverbite = Vector3.ProjectOnPlane(-contact.normal.normalized, transform.up).normalized *
                                   StepOverbiteMagnitude;

            // Start the raycast position directly above the contact point with the step, at the vertical position of the bottom of the player
            // According to my shitty vector math, this is equivalent to Proj(contact point onto up) + up * Dot(up, bottomOfPlayer)
            Vector3 smarterRaycastStartPos = Vector3.ProjectOnPlane(contact.point, transform.up) + transform.up * Vector3.Dot(transform.up, m.BottomOfPlayer);
            Debug.DrawRay(smarterRaycastStartPos, transform.up * MaxStepHeight, Color.magenta, 10);
            
            // Old way of calculating raycastStartPos above contact point
            //Debug.DrawRay(contact.point, transform.up * maxStepHeight, Color.blue, 10);
            //Vector3 raycastStartPos = contact.point + transform.up * maxStepHeight;

            Vector3 raycastStartPos = smarterRaycastStartPos + transform.up * MaxStepHeight;
            // Move the raycast inwards towards the stair (we will be raycasting down at the stair)
            Debug.DrawRay(raycastStartPos, stepOverbite, Color.red);
            raycastStartPos += stepOverbite;
            Vector3 direction = -transform.up;

            Debug.DrawRay(raycastStartPos, direction * MaxStepHeight, Color.green);
            bool stepFound = contact.otherCollider.Raycast(
                new Ray(raycastStartPos, direction),
                out stepTest,
                MaxStepHeight
            );
            if (stepFound) {
                bool stepIsGround = Vector3.Dot(stepTest.normal, transform.up) > GroundMovement.IS_GROUND_THRESHOLD;
                if (!stepIsGround) return false;
                
                float stepHeight = Vector3.Dot(transform.up, stepTest.point - m.BottomOfPlayer);

                if (stepHeight < MinStepHeight) return false;

                Vector3 stepOffset = stepOverbite + transform.up * stepHeight;
                //Debug.DrawRay(smarterRaycastStartPos, stepOffset, Color.yellow, 10);
                step = new StepFound(contact, contactNormal, stepOffset);
                m.debug.Log($"Step: {contact.point}\n{stepOffset:F3}\nstepHeight:{stepHeight}");
            }

            return stepFound;
        }
    }
}
