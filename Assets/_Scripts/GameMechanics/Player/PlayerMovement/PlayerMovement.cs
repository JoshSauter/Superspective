using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using SuperspectiveUtils;
using PortalMechanics;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using StateUtils;
using UnityEngine;
// Alias Settings.Gameplay.SprintBehaviorMode to a more readable name
using SprintBehaviorMode = Settings.Gameplay.SprintBehaviorMode;

[RequireComponent(typeof(Rigidbody))]
public partial class PlayerMovement : SingletonSuperspectiveObject<PlayerMovement, PlayerMovement.PlayerMovementSave>, AudioJobOnGameObject {
    private List<PlayerMovementComponent> components = new List<PlayerMovementComponent>();
    public StaircaseMovement staircaseMovement;
    public JumpMovement jumpMovement;
    public GroundMovement groundMovement;
    public AirMovement airMovement;
    public AFKDetectionComponent afkDetectionComponent;
    
    public bool PlayerIsAFK => afkDetectionComponent.state == AFKDetectionComponent.AFKState.AFK;
    
    // Inspector properties, ShowInInspector only works on Monobehaviour classes
    [ShowInInspector]
    public string GroundName => IsGrounded ? Grounded.Ground.name : "Not Grounded";
    [ShowInInspector]
    public Vector3 GroundNormal => IsGrounded ? Grounded.contact.normal : Vector3.zero;

    public bool snapToGroundEnabled = true;

    const float ACCELERATION_LERP_SPEED = 15f;
    const float AIRSPEED_CONTROL_FACTOR = 0.4f;
    const float DECELERATION_LERP_SPEED = 12f;
    const float BACKWARDS_SPEED = 1f;
    const float WALK_SPEED = 9f;
    const float RUN_SPEED = 14f;
    const float DESIRED_MOVEMENT_LERP_SPEED = 10;
    public const float WIND_RESISTANCE_MULTIPLIER = 0.4f;

    private bool SprintByDefault => Settings.Gameplay.SprintByDefault.value;
    public bool ToggleSprint => ((SprintBehaviorMode)Settings.Gameplay.SprintBehavior.dropdownSelection.selection.Datum) == SprintBehaviorMode.Toggle;
    public bool sprintIsToggled { get; private set; }

    public bool autoRun;
    public Rigidbody thisRigidbody;

    PlayerButtonInput input;

    private bool Stopped =>
        PlayerLook.instance.state != PlayerLook.ViewLockState.ViewUnlocked ||
        CameraFlythrough.instance.isPlayingFlythrough ||
        !GameManager.instance.gameHasLoaded;

    public CapsuleCollider thisCollider;
    MeshRenderer thisRenderer;

    private float Scale => Player.instance.Scale;

    [ShowInInspector]
    public Vector3 CurVelocity => (thisRigidbody == null) ? Vector3.zero : thisRigidbody.velocity;

    public float movespeedMultiplier = 1;

    private float movespeed;
    protected float EffectiveMovespeed => movespeed * Scale;
    public float WalkSpeed => WALK_SPEED * Scale * movespeedMultiplier;
    public float RunSpeed => RUN_SPEED * Scale * movespeedMultiplier;
    public Vector3 BottomOfPlayer => transform.position - (transform.up * 2.5f * Scale);
    public Vector3 TopOfPlayer => transform.position + (transform.up * 0.5f * Scale);
    
    // End-game mechanics
    public enum EndGameMovement : byte {
        NotStarted,
        Walking,
        HorizontalInputMovesPlayerForward,
        AllInputMovesPlayerForward,
        AllInputDisabled
    }
    public EndGameMovement endGameMovement;

    public struct TimeSlice {
        public float time;
        public Vector3 position;

        public TimeSlice(float time, Vector3 pos) {
            this.time = time;
            this.position = pos;
        }
    }

    private const int MAX_TIME_SLICES = 10;
    public List<TimeSlice> timeSlices = new List<TimeSlice>();

    public Vector3 LastPlayerPosition => timeSlices.Count > 0 ? timeSlices[timeSlices.Count - 1].position : transform.position;
    public Vector3 AverageVelocityRecently {
        get {
            if (thisRigidbody.isKinematic) return Vector3.zero;
            if (timeSlices.Count == 0) return Vector3.zero;

            Vector3 avgDelta = Vector3.zero;
            for (int i = 1; i < timeSlices.Count; i++) {
                avgDelta += (timeSlices[i].position - timeSlices[i-1].position) / (timeSlices[i].time - timeSlices[i-1].time);
            }

            return avgDelta / timeSlices.Count;
        }
    }

    private GroundMovement.GroundedState Grounded => groundMovement.grounded;
    [ShowInInspector]
    public bool IsGrounded => Grounded?.IsGrounded ?? false;

    [ShowInInspector]
    bool StandingOnHeldObject => Grounded.StandingOnHeldObject;

    bool MoveDirectionHeld => Mathf.Abs(input.LeftStick.y) > 0  || Mathf.Abs(input.LeftStick.x) > 0;

    bool Jumping => jumpMovement.jumpState == JumpMovement.JumpState.Jumping;
    bool MoveDirectionHeldRecently => moveDirectionHeldRecentlyState == MoveDirectionInputState.HeldRecently;

    public enum MoveDirectionInputState : byte {
        HeldRecently,
        NotHeldRecently
    }
    
    private StateMachine<MoveDirectionInputState> moveDirectionHeldRecentlyState;
    private const float SNAP_TO_GROUND_DISTANCE = 0.5f;
    private const float RESET_MOVE_DIRECTION_HELD_STATE_TIME = .25f;
    public bool pauseSnapToGround = false; // Hack fix to turn off snapping to ground when player is in a StaircaseRotate
    
    private float PlayerRadius => thisCollider == null ? 0 : thisCollider.radius * Scale;

    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    public enum MovementEnabledState : byte {
        Enabled,
        Disabled
    }
    public MovementEnabledState movementEnabledState = MovementEnabledState.Enabled;

    protected override void Awake() {
        base.Awake();
        
        moveDirectionHeldRecentlyState = this.StateMachine(MoveDirectionInputState.NotHeldRecently, true);
        moveDirectionHeldRecentlyState.AddStateTransition(MoveDirectionInputState.HeldRecently, MoveDirectionInputState.NotHeldRecently, RESET_MOVE_DIRECTION_HELD_STATE_TIME);

        input = PlayerButtonInput.instance;
        thisRigidbody = GetComponent<Rigidbody>();
        thisCollider = GetComponent<CapsuleCollider>();
        thisRenderer = GetComponentInChildren<MeshRenderer>();

        components = new List<PlayerMovementComponent>();
        staircaseMovement = new StaircaseMovement(this);
        jumpMovement = new JumpMovement(this);
        groundMovement = new GroundMovement(this);
        airMovement = new AirMovement(this);
        afkDetectionComponent = new AFKDetectionComponent(this);
        components.Add(staircaseMovement);
        components.Add(jumpMovement);
        components.Add(groundMovement);
        components.Add(airMovement);
        components.Add(afkDetectionComponent);
        
        components.ForEach(c => c.Init());
    }

    // Use this for initialization
    new IEnumerator Start() {
        base.Start();
        movespeed = SprintByDefault ? RunSpeed : WalkSpeed;
        sprintIsToggled = SprintByDefault;

        thisRigidbody.isKinematic = true;
        yield return new WaitUntil(() => GameManager.instance.gameHasLoaded);
        thisRigidbody.isKinematic = false;

        TeleportEnter.BeforeAnyTeleport += (enter, exit, player) => {
            TransformTimeSlicesUponTeleport(enter.transform, exit.transform);
        };

        Portal.BeforeAnyPortalTeleport += (inPortal, teleported) => {
            if (!teleported.TaggedAsPlayer()) return;

            TransformTimeSlicesUponTeleport(inPortal.transform, inPortal.otherPortal.transform);
        };
        
        Settings.Gameplay.SprintByDefault.OnValueChanged += (sprintByDefault) => {
            if (sprintByDefault) {
                sprintIsToggled = true;
                movespeed = RunSpeed;
            } else {
                sprintIsToggled = false;
                movespeed = WalkSpeed;
            }
        };
    }

    void TransformTimeSlicesUponTeleport(Transform teleportIn, Transform teleportOut) {
        for (int i = 0; i < timeSlices.Count; i++) {
            TimeSlice timeSlice = timeSlices[i];
            Vector3 timeSliceWorldPos = timeSlice.position;
            Vector3 timeSliceLocalPos = teleportIn.InverseTransformPoint(timeSliceWorldPos);
            Vector3 timeSliceTransformedWorldPos = teleportOut.TransformPoint(timeSliceLocalPos);
            timeSlice.position = timeSliceTransformedWorldPos;
            timeSlices[i] = timeSlice;
        }
    }

    void Update() {
        if (!GameManager.instance.gameHasLoaded) return;
        // if (Settings.Keybinds.AutoRun.Pressed) autoRun = !autoRun;

        bool ShouldUseSprintSpeed() {
            if (endGameMovement != EndGameMovement.NotStarted) return false;
            if (staircaseMovement.RecentlySteppedUpOrDown) return false;
            if (autoRun) return true;

            if (ToggleSprint) {
                if (input.SprintPressed) sprintIsToggled = !sprintIsToggled;

                return sprintIsToggled;
            }
            else {
                return input.SprintHeld ? !SprintByDefault : SprintByDefault;
            }
        }
        if (ShouldUseSprintSpeed()) {
            movespeed = Mathf.Lerp(movespeed, RunSpeed / Scale, DESIRED_MOVEMENT_LERP_SPEED * Time.deltaTime);
        }
        else {
            movespeed = Mathf.Lerp(movespeed, WalkSpeed / Scale, DESIRED_MOVEMENT_LERP_SPEED * Time.deltaTime);
        }
    }

    void FixedUpdate() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        if (movementEnabledState == MovementEnabledState.Disabled) {
            if (!thisRigidbody.isKinematic) {
                thisRigidbody.velocity = Vector3.zero;
            }

            return;
        }
        
        if (MoveDirectionHeld) {
            moveDirectionHeldRecentlyState.Set(MoveDirectionInputState.HeldRecently, true);
        }
        
        groundMovement.UpdateGroundedState();

        jumpMovement.UpdateJumping();
        
        Vector3 desiredVelocity = Grounded.IsGrounded ? groundMovement.CalculateGroundMovement(Grounded.contact) : airMovement.CalculateAirMovement();
        if (snapToGroundEnabled) {
            desiredVelocity = groundMovement.UpdateSnapToGround(desiredVelocity);
        }
        
        bool closeToGround = groundMovement.GroundRaycast(new Ray(BottomOfPlayer + transform.up * Scale, -transform.up), (1 + SNAP_TO_GROUND_DISTANCE) * Scale, out RaycastHit _, GroundMovement.IS_GROUND_THRESHOLD);
        bool stopPlayerWhenStanding = !pauseSnapToGround && !Jumping && closeToGround && !staircaseMovement.RecentlySteppedUpOrDown && !MoveDirectionHeldRecently;

        thisRigidbody.isKinematic = (Stopped || staircaseMovement.stepState != StaircaseMovement.StepState.Idle || stopPlayerWhenStanding) && (endGameMovement == EndGameMovement.NotStarted);

        if (timeSlices.Count >= MAX_TIME_SLICES) {
            timeSlices.RemoveAt(0);
        }
        timeSlices.Add(new TimeSlice(Time.time, thisRigidbody.position));

        if (Stopped || Grounded.StandingOnHeldObject) {
            return;
        }

        groundMovement.UpdateLastGroundVelocity(desiredVelocity);
        
        // Prevent player from floating around on cubes they're holding...
        if (Grounded.StandingOnHeldObject) desiredVelocity += Physics.gravity * (5 * Time.fixedDeltaTime);

        staircaseMovement.UpdateStaircase(desiredVelocity);

        // TODO: Check if all this is bugged in warped gravity
        //float movingBackward = Vector2.Dot(
        //    new Vector2(desiredVelocity.x, desiredVelocity.z),
        //    new Vector2(transform.forward.x, transform.forward.z)
        //);
        //if (movingBackward < -0.5f) {
        //    float slowdownAmount = Mathf.InverseLerp(-.5f, -1, movingBackward);
        //    desiredVelocity.x *= Mathf.Lerp(1, BACKWARDS_SPEED, slowdownAmount);
        //    desiredVelocity.z *= Mathf.Lerp(1, BACKWARDS_SPEED, slowdownAmount);
        //}

        if (!input.LeftStickHeld && !input.JumpHeld && Grounded.Ground != null && Grounded.Ground.CompareTag("Staircase")) {
            thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        else {
            thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        thisRigidbody.useGravity = !IsGrounded;

        if (!thisRigidbody.isKinematic && staircaseMovement.stepState == StaircaseMovement.StepState.Idle) {
            // debug.Log($"Player velocity: {thisRigidbody.velocity:F2}\nSetting it to: {desiredVelocity}");
            thisRigidbody.velocity = desiredVelocity;

            // Test for collisions next frame by simulating one frame of movement and checking the capsule for collisions
            // If we find a collision, change the velocity so we end up next to the wall instead of inside it
            Vector3 positionBeforeCollisionResolution = transform.position;
            Vector3 thisFrameOffset = thisRigidbody.velocity * Time.fixedDeltaTime;
            transform.position += thisFrameOffset;
            if (ResolveCollisions(out Vector3 _)) {
                thisRigidbody.velocity = (transform.position - positionBeforeCollisionResolution) / Time.fixedDeltaTime;
            }
            transform.position = positionBeforeCollisionResolution;
        }

        // Apply wind resistance
        Vector3 projectedVertVelocity = ProjectedVerticalVelocity();
        if (!IsGrounded && Vector3.Dot(Physics.gravity.normalized, projectedVertVelocity.normalized) > 0) {
            thisRigidbody.AddForce(
                transform.up * projectedVertVelocity.magnitude * thisRigidbody.mass * WIND_RESISTANCE_MULTIPLIER
            );
        }
    }
        
    private void DecomposeVector(Vector3 vector, out Vector3 vertical, out Vector3 horizontal) {
        vertical = DecomposeVectorVertical(vector);
        horizontal = DecomposeVectorHorizontal(vector);
    }
        
    private Vector3 DecomposeVectorHorizontal(Vector3 vector) {
        return vector - DecomposeVectorVertical(vector);
    }
        
    private Vector3 DecomposeVectorVertical(Vector3 vector) {
        return Vector3.Project(vector, transform.up);
    }

    public Vector3 ProjectHorizontalVelocity(Vector3 unprojectedVelocity) {
        return Vector3.ProjectOnPlane(unprojectedVelocity, transform.up);
    }

    public Vector3 ProjectedHorizontalVelocity() {
        return ProjectHorizontalVelocity(thisRigidbody.velocity);
    }

    public Vector3 ProjectedVerticalVelocity() {
        return thisRigidbody.velocity - ProjectedHorizontalVelocity();
    }

    public void StopMovement() {
        if (thisRigidbody.isKinematic) return;
        thisRigidbody.velocity = Vector3.zero;
    }
    
    private Collider[] CheckCapsule() {
        // Get the relevant properties from the CapsuleCollider
        Vector3 capsuleCenter = transform.TransformPoint(thisCollider.center);
        float capsuleHeight = thisCollider.height * Scale;
        float capsuleRadius = thisCollider.radius * Scale;

        Vector3 capsuleAxis = transform.up;
        Vector3 p1 = capsuleCenter - capsuleAxis * (capsuleHeight * (0.5f - capsuleRadius));
        Vector3 p2 = capsuleCenter + (capsuleAxis * (capsuleHeight * 0.5f - capsuleRadius));
                
        var result = Physics.OverlapCapsule(p1, p2, capsuleRadius, Player.instance.interactsWithPlayerLayerMask, QueryTriggerInteraction.Ignore);

        bool FilterOutHeldObjects(Collider c) {
            if (Player.instance.IsHoldingSomething) {
                return c != Player.instance.heldObject.thisCollider;
            }

            return true;
        }
        return result.Where(FilterOutHeldObjects).ToArray();
    }

    // Returns true if a collision was resolved
    private bool ResolveCollisions(out Vector3 offsetApplied) {
        Collider[] neighbors = CheckCapsule();

        offsetApplied = Vector3.zero;
        bool collisionResolved = false;
        for (int i = 0; i < neighbors.Length; i++) {
            Collider neighbor = neighbors[i];
            if (neighbor == thisCollider ||
                Physics.GetIgnoreLayerCollision(SuperspectivePhysics.PlayerLayer, neighbor.gameObject.layer) ||
                SuperspectivePhysics.CollisionsAreIgnored(thisCollider, neighbor)) continue;

            bool collisionExists = CollisionResolutionOffset(neighbor, out Vector3 offset);
            if (collisionExists) {
                transform.position += offset;
                offsetApplied += offset;
            }
            collisionResolved |= collisionExists;
        }
        
        return collisionResolved;
    }
    
    // Returns true if the colliders overlap, and sets the offset to the direction and distance to resolve the collision
    private bool CollisionResolutionOffset(Collider neighbor, out Vector3 offset) {
        bool collidersOverlap = Physics.ComputePenetration(
            thisCollider, transform.position, transform.rotation,
            neighbor, neighbor.transform.position, neighbor.transform.rotation,
            out Vector3 offsetDirection, out float offsetMagnitude);
        offset = collidersOverlap ? offsetDirection * offsetMagnitude : Vector3.zero;
        return collidersOverlap;
    }

#region events
    public delegate void PlayerMovementAction();

    public PlayerMovementAction OnJump;
    public PlayerMovementAction OnJumpLanding;
    public PlayerMovementAction OnStaircaseStep;
#endregion

#region Saving

    public override void LoadSave(PlayerMovementSave save) {
        autoRun = save.autoRun;
        jumpMovement.jumpState.LoadFromSave(save.jumpStateSave);
        staircaseMovement.stepState.LoadFromSave(save.stepStateSave);
        movementEnabledState = save.movementEnabledState;
            
        movespeed = save.movespeed;
        movespeedMultiplier = save.movespeedMultiplier;

        thisRigidbody.isKinematic = save.thisRigidbodyKinematic;
        if (!thisRigidbody.isKinematic) {
            thisRigidbody.velocity = save.thisRigidbodyVelocity;
        }
        thisRigidbody.useGravity = save.thisRigidbodyUseGravity;
        thisRigidbody.mass = save.thisRigidbodyMass;
    }
    
    // There's only one player so we don't need a UniqueId here
    public override string ID => "PlayerMovement";

    [Serializable]
    public class PlayerMovementSave : SaveObject<PlayerMovement> {
        public StateMachine<JumpMovement.JumpState>.StateMachineSave jumpStateSave;
        public StateMachine<StaircaseMovement.StepState>.StateMachineSave stepStateSave;
        public SerializableVector3 thisRigidbodyVelocity;
        public float movespeed;
        public float movespeedMultiplier;
        public float thisRigidbodyMass;
        public MovementEnabledState movementEnabledState;
        public bool autoRun;
        public bool thisRigidbodyKinematic;
        public bool thisRigidbodyUseGravity;

        public PlayerMovementSave(PlayerMovement playerMovement) : base(playerMovement) {
            autoRun = playerMovement.autoRun;
            jumpStateSave = playerMovement.jumpMovement.jumpState.ToSave();
            stepStateSave = playerMovement.staircaseMovement.stepState.ToSave();
            movementEnabledState = playerMovement.movementEnabledState;
            
            movespeed = playerMovement.movespeed;
            movespeedMultiplier = playerMovement.movespeedMultiplier;

            thisRigidbodyVelocity = playerMovement.thisRigidbody.velocity;
            thisRigidbodyKinematic = playerMovement.thisRigidbody.isKinematic;
            thisRigidbodyUseGravity = playerMovement.thisRigidbody.useGravity;
            thisRigidbodyMass = playerMovement.thisRigidbody.mass;
        }
    }
#endregion
}