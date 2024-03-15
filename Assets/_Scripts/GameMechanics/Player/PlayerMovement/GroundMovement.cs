using System;
using System.Linq;
using NaughtyAttributes;
using SuperspectiveUtils;
using UnityEngine;

partial class PlayerMovement {
    [Serializable]
    public class GroundMovement : PlayerMovementComponent {
        public GroundMovement(PlayerMovement movement) : base(movement) { }
        
        [Serializable]
        public class GroundedState {
            [ShowNativeProperty]
            public bool IsGrounded {
                get => Ground != null;
                // Only allow setting isGrounded to false (clear the state)
                set {
                    if (!value) {
                        contact = default;
                        framesWaitedAfterLeavingGround = 0;
                    }
                }
            }

            public Collider Ground => contact.otherCollider;
            public bool StandingOnHeldObject => IsStandingOnHeldObject(contact);

            public ContactPoint contact;
            public float framesWaitedAfterLeavingGround;
        }

        const int FRAMES_TO_WAIT_AFTER_LEAVING_GROUND = 10;
        // Dot(face normal, transform.up) must be greater than this value to be considered "ground"
        public const float IS_GROUND_THRESHOLD = 0.675f;
        // Dot(face normal, transform.up) must be greater than this value to be for snap-to-ground to occur
        public const float SNAP_TO_GROUND_DOT_NORMAL_THRESHOLD = 0.9f;

        public string lastGroundTouchedDebug = "";
        public GroundedState grounded;
        public Vector3 lastGroundVelocity;
        
        public override void Init() {
            grounded = new GroundedState();
        }
        
        public void UpdateGroundedState() {
            ContactPoint groundContactPoint = default;
            float maxGroundTest = IS_GROUND_THRESHOLD; // Amount upwards-facing the most ground-like object is
            foreach (ContactPoint contact in m.allContactThisFrame) {
                float groundTest = Vector3.Dot(contact.normal, transform.up);
                if (groundTest > maxGroundTest) {
                    groundContactPoint = contact;
                    maxGroundTest = groundTest;
                }
            }

            // Was a ground object found?
            bool isGroundedNow = maxGroundTest > IS_GROUND_THRESHOLD && !m.jumpMovement.UnderMinJumpTime;
            if (isGroundedNow) {
                grounded.framesWaitedAfterLeavingGround = 0;
                grounded.contact = groundContactPoint;
                if (m.DEBUG) {
                    DebugExtension.DebugWireSphere(groundContactPoint.point, Color.green, 0.25f, 1f);
                }
                Vector3 contactNormal = groundContactPoint.normal;//GetGroundNormal(groundContactPoint, out bool rayHit, out RaycastHit hitInfo);
                lastGroundTouchedDebug = grounded.contact.otherCollider.FullPath();
                // if (rayHit) {
                //     grounded.contactNormal = contactNormal;
                // }
                // else {
                //     grounded.contactNormal = groundContactPoint.normal;
                // }
            }
            // If we were grounded last FixedUpdate and not grounded now
            else if (grounded.IsGrounded) {
                // Wait a few fixed updates before saying that the player is ungrounded
                if (grounded.framesWaitedAfterLeavingGround >= FRAMES_TO_WAIT_AFTER_LEAVING_GROUND) {
                    grounded.IsGrounded = false;
                }
                else {
                    grounded.framesWaitedAfterLeavingGround++;
                }
            }
            // Not grounded anytime recently
            else {
                grounded.contact = default;
            }
        }

        public void UpdateSnapToGround() {
            if (m.pauseSnapToGround) return;
            if (!grounded.IsGrounded) return;
            
            bool recentlySteppedUp = m.stairMovement.RecentlySteppedUp;

            if (m.Jumping) return;
            if (recentlySteppedUp) return;

            if (m.MoveDirectionHeldRecently) {
                if (m.allContactThisFrame.Count == 0) return;
                
                // For snap-to-ground, we use the contact point that is least ground-like to make sure we don't apply snap-to-ground when player is on a sloped surface
                float leastGroundNormalDotUp = m.allContactThisFrame.Select(c => Vector3.Dot(c.normal, transform.up)).Min();
                if (leastGroundNormalDotUp > SNAP_TO_GROUND_DOT_NORMAL_THRESHOLD && grounded.Ground.Raycast(new Ray(m.bottomOfPlayer, -transform.up), out RaycastHit hit, snapToGroundDistance * m.scale)) {
                    Vector3 beforePos = m.bottomOfPlayer;
                    // Snap the player to the ground
                    transform.Translate(-(m.bottomOfPlayer - hit.point), Space.World);
                    Vector3 afterPos = m.bottomOfPlayer;
                
                    m.debug.LogWarning($"NormalDotUp: {leastGroundNormalDotUp}");
                    Debug.DrawRay(hit.point, hit.normal, Color.green, 0.15f);
                }
            }
            // No input, freeze the player movement after a short delay
            else {
                m.thisRigidbody.isKinematic = true;
            }
        }

        public void UpdateLastGroundVelocity(Vector3 desiredVelocity) {
            if (grounded.IsGrounded && m.stairMovement.stepState == StairMovement.StepState.StepReady) {
                lastGroundVelocity = desiredVelocity;
            }
        }

        /// <summary>
        ///     Calculates player movement when the player is on (or close enough) to the ground.
        ///     Movement is perpendicular to the ground's normal vector.
        /// </summary>
        /// <param name="ground">RaycastHit info for the walkable object that passes the IsGrounded test</param>
        /// <returns>Desired Velocity according to current input</returns>
        public Vector3 CalculateGroundMovement(ContactPoint ground) {
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
                    decelerationLerpSpeed * Time.fixedDeltaTime
                );
                return desiredHorizontalVelocity + (m.thisRigidbody.velocity - horizontalVelocity);
            }

            float adjustedMovespeed = ground.otherCollider.CompareTag("Staircase") ? m.walkSpeed : m.effectiveMovespeed;
            return Vector3.Lerp(
                m.thisRigidbody.velocity,
                moveDirection * adjustedMovespeed,
                accelerationLerpSpeed * Time.fixedDeltaTime
            );
        }

        public bool WalkingOnGlass() {
            if (grounded.IsGrounded == false) {
                return false;
            }

            bool onGlass = grounded
                .Ground
                .GetMaybeComponent<Renderer>()
                .Exists(r => r.sharedMaterials.Where(m => m != null).ToArray()[0].name.ToLower().Contains("glass"));

            return onGlass;
        }

        static bool IsStandingOnHeldObject(ContactPoint contact) {
            PickupObject maybeCube1 = null, maybeCube2 = null;
            if (contact.thisCollider != null) maybeCube1 = contact.thisCollider.GetComponent<PickupObject>();
            if (contact.otherCollider != null) maybeCube2 = contact.otherCollider.GetComponent<PickupObject>();
            bool cube1IsHeld = maybeCube1 != null && (maybeCube1.isHeld || !maybeCube1.IsGrounded());
            bool cube2IsHeld = maybeCube2 != null && (maybeCube2.isHeld || !maybeCube2.IsGrounded());
            //debug.Log($"Grounded: {grounded.isGrounded}\nCube1IsHeld: {cube1IsHeld}\nCube2IsHeld: {cube2IsHeld}");
            return cube1IsHeld || cube2IsHeld;
        }
    }
}