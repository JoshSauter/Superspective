using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using SuperspectiveUtils;
using LevelManagement;
using NaughtyAttributes;
using PortalMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using UnityEngine;
// Alias Settings.Gameplay.SprintBehaviorMode to a more readable name
using SprintBehaviorMode = Settings.Gameplay.SprintBehaviorMode;

[RequireComponent(typeof(Rigidbody))]
public partial class PlayerMovement : SingletonSaveableObject<PlayerMovement, PlayerMovement.PlayerMovementSave>, AudioJobOnGameObject {
    private List<PlayerMovementComponent> components = new List<PlayerMovementComponent>();
    public StairMovement stairMovement;
    public JumpMovement jumpMovement;
    public GroundMovement groundMovement;
    public AirMovement airMovement;
    
    // Inspector properties, ShowNativeProperty only works on Monobehaviour classes
    [ShowNativeProperty]
    public string GroundName => IsGrounded ? Grounded.Ground.name : "Not Grounded";
    [ShowNativeProperty]
    public Vector3 GroundNormal => IsGrounded ? Grounded.contact.normal : Vector3.zero;

    // For Debugging:
    void SetTimeScale() {
        Time.timeScale = timeScale;
    }
    [OnValueChanged("SetTimeScale")]
    public float timeScale = 1;

    public bool snapToGroundEnabled = true;

    const float accelerationLerpSpeed = 15f;
    const float airspeedControlFactor = 0.4f;
    const float decelerationLerpSpeed = 12f;
    const float backwardsSpeed = 1f;
    const float _walkSpeed = 9f;
    const float _runSpeed = 14f;
    const float desiredMovespeedLerpSpeed = 10;
    public const float windResistanceMultiplier = 0.4f;

    private bool SprintByDefault => Settings.Gameplay.SprintByDefault.value;
    public bool ToggleSprint => ((SprintBehaviorMode)Settings.Gameplay.SprintBehavior.dropdownSelection.selection.Datum) == SprintBehaviorMode.Toggle;
    public bool sprintIsToggled { get; private set; }

    public bool autoRun;
    public Rigidbody thisRigidbody;

    [ShowNativeProperty]
    public int numContactsThisFrame => allContactThisFrame.Count;
    readonly List<ContactPoint> allContactThisFrame = new List<ContactPoint>();
    PlayerButtonInput input;

    private bool stopped =>
        PlayerLook.instance.state != PlayerLook.ViewLockState.ViewUnlocked ||
        CameraFlythrough.instance.isPlayingFlythrough ||
        !GameManager.instance.gameHasLoaded;

    CapsuleCollider thisCollider;
    MeshRenderer thisRenderer;

    private float scale => Player.instance.scale;

    [ShowNativeProperty]
    public Vector3 curVelocity => (thisRigidbody == null) ? Vector3.zero : thisRigidbody.velocity;

    public float movespeedMultiplier = 1;

    private float movespeed;
    protected float effectiveMovespeed => movespeed * scale;
    public float walkSpeed => _walkSpeed * scale * movespeedMultiplier;
    public float runSpeed => _runSpeed * scale * movespeedMultiplier;
    public Vector3 bottomOfPlayer => transform.position - (transform.up * 2.5f * scale);


    public struct TimeSlice {
        public float time;
        public Vector3 position;

        public TimeSlice(float time, Vector3 pos) {
            this.time = time;
            this.position = pos;
        }
    }

    public List<TimeSlice> timeSlices = new List<TimeSlice>();

    public Vector3 averageVelocityRecently {
        get {
            if (timeSlices.Count == 0) return Vector3.zero;

            Vector3 avgDelta = Vector3.zero;
            for (int i = 1; i < timeSlices.Count; i++) {
                avgDelta += (timeSlices[i].position - timeSlices[i-1].position) / (timeSlices[i].time - timeSlices[i-1].time);
            }

            return avgDelta / timeSlices.Count;
        }
    }

    private GroundMovement.GroundedState Grounded => groundMovement.grounded;
    [ShowNativeProperty]
    public bool IsGrounded => Grounded?.IsGrounded ?? false;

    [ShowNativeProperty]
    bool StandingOnHeldObject => Grounded.StandingOnHeldObject;

    bool moveDirectionHeld => Mathf.Abs(input.LeftStick.y) > 0  || Mathf.Abs(input.LeftStick.x) > 0;

    bool Jumping => jumpMovement.jumpState == JumpMovement.JumpState.Jumping;
    bool MoveDirectionHeldRecently => moveDirectionHeldRecentlyState == MoveDirectionInputState.HeldRecently;

    public enum MoveDirectionInputState {
        HeldRecently,
        NotHeldRecently
    }
    
    private StateMachine<MoveDirectionInputState> moveDirectionHeldRecentlyState;
    private const float snapToGroundDistance = 0.5f;
    private const float resetMoveDirectionHeldStateTime = .25f;
    public bool pauseSnapToGround = false; // Hack fix to turn off snapping to ground when player is in a StaircaseRotate

    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    protected override void Awake() {
        base.Awake();
        
        moveDirectionHeldRecentlyState = this.StateMachine(MoveDirectionInputState.NotHeldRecently, true);
        moveDirectionHeldRecentlyState.AddStateTransition(MoveDirectionInputState.HeldRecently, MoveDirectionInputState.NotHeldRecently, resetMoveDirectionHeldStateTime);

        input = PlayerButtonInput.instance;
        thisRigidbody = GetComponent<Rigidbody>();
        thisCollider = GetComponent<CapsuleCollider>();
        thisRenderer = GetComponentInChildren<MeshRenderer>();

        components = new List<PlayerMovementComponent>();
        stairMovement = new StairMovement(this);
        jumpMovement = new JumpMovement(this);
        groundMovement = new GroundMovement(this);
        airMovement = new AirMovement(this);
        components.Add(stairMovement);
        components.Add(jumpMovement);
        components.Add(groundMovement);
        components.Add(airMovement);
        
        components.ForEach(c => c.Init());
    }

    // Use this for initialization
    new IEnumerator Start() {
        base.Start();
        movespeed = SprintByDefault ? runSpeed : walkSpeed;
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
                movespeed = runSpeed;
            } else {
                sprintIsToggled = false;
                movespeed = walkSpeed;
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
        if (Settings.Keybinds.AutoRun.Pressed) autoRun = !autoRun;


        bool ShouldUseSprintSpeed() {
            if (stairMovement.RecentlySteppedUp) return false;
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
            movespeed = Mathf.Lerp(movespeed, runSpeed / scale, desiredMovespeedLerpSpeed * Time.deltaTime);
        }
        else {
            movespeed = Mathf.Lerp(movespeed, walkSpeed / scale, desiredMovespeedLerpSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate() {
        if (!GameManager.instance.gameHasLoaded) return;
        
        if (moveDirectionHeld) {
            moveDirectionHeldRecentlyState.Set(MoveDirectionInputState.HeldRecently, true);
        }
        
        groundMovement.UpdateGroundedState();

        jumpMovement.UpdateJumping();

        if (snapToGroundEnabled) {
            groundMovement.UpdateSnapToGround();
        }
        
        bool stopPlayerWhenStanding = !pauseSnapToGround && !Jumping && Grounded.IsGrounded && !stairMovement.RecentlySteppedUp && !MoveDirectionHeldRecently;

        thisRigidbody.isKinematic = stopped || stairMovement.stepState != StairMovement.StepState.StepReady || stopPlayerWhenStanding;

        if (timeSlices.Count >= 10) {
            timeSlices.RemoveAt(0);
        }
        timeSlices.Add(new TimeSlice(Time.time, transform.position));

        if (stopped || Grounded.StandingOnHeldObject) {
            allContactThisFrame.Clear();
            return;
        }

        Vector3 desiredVelocity = Grounded.IsGrounded ? groundMovement.CalculateGroundMovement(Grounded.contact) : airMovement.CalculateAirMovement();

        groundMovement.UpdateLastGroundVelocity(desiredVelocity);
        
        // Prevent player from floating around on cubes they're holding...
        if (Grounded.StandingOnHeldObject) desiredVelocity += 5 * Physics.gravity * Time.fixedDeltaTime;

        stairMovement.UpdateStaircase(desiredVelocity);

        // TODO: Check if all this is bugged in warped gravity
        float movingBackward = Vector2.Dot(
            new Vector2(desiredVelocity.x, desiredVelocity.z),
            new Vector2(transform.forward.x, transform.forward.z)
        );
        if (movingBackward < -0.5f) {
            float slowdownAmount = Mathf.InverseLerp(-.5f, -1, movingBackward);
            desiredVelocity.x *= Mathf.Lerp(1, backwardsSpeed, slowdownAmount);
            desiredVelocity.z *= Mathf.Lerp(1, backwardsSpeed, slowdownAmount);
        }

        if (!input.LeftStickHeld && !input.JumpHeld && Grounded.Ground != null && Grounded.Ground.CompareTag("Staircase")) {
            thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        else {
            thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        thisRigidbody.useGravity = !IsGrounded;

        if (!thisRigidbody.isKinematic && stairMovement.stepState == StairMovement.StepState.StepReady) {
            debug.Log($"Player velocity: {thisRigidbody.velocity:F2}\nSetting it to: {desiredVelocity}");
            thisRigidbody.velocity = desiredVelocity;
        }

        // Apply wind resistance
        Vector3 projectedVertVelocity = ProjectedVerticalVelocity();
        if (!IsGrounded && Vector3.Dot(Physics.gravity.normalized, projectedVertVelocity.normalized) > 0)
            thisRigidbody.AddForce(
                transform.up * projectedVertVelocity.magnitude * thisRigidbody.mass * windResistanceMultiplier
            );

        // We don't collide with other objects while kinematic, so keep the contactThisFrame data while we remain kinematic
        if (!thisRigidbody.isKinematic) {
            allContactThisFrame.Clear();
        }
    }

    void OnCollisionStay(Collision collision) {
        allContactThisFrame.AddRange(collision.contacts);
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
        thisRigidbody.velocity = Vector3.zero;
    }

#region IsGrounded characteristics
#endregion

#region events
    public delegate void PlayerMovementAction();

    public PlayerMovementAction OnJump;
    public PlayerMovementAction OnJumpLanding;
    public PlayerMovementAction OnStaircaseStepUp;
#endregion

#region Saving
    // There's only one player so we don't need a UniqueId here
    public override string ID => "PlayerMovement";

    [Serializable]
    public class PlayerMovementSave : SerializableSaveObject<PlayerMovement> {
        bool autoRun;

        StateMachine<JumpMovement.JumpState>.StateMachineSave jumpStateSave;
        StateMachine<StairMovement.StepState>.StateMachineSave stepStateSave;
        float movespeed;
        float movespeedMultiplier;

        SerializableVector3 playerGravityDirection;

        bool thisRigidbodyKinematic;
        float thisRigidbodyMass;
        bool thisRigidbodyUseGravity;
        SerializableVector3 thisRigidbodyVelocity;

        public PlayerMovementSave(PlayerMovement playerMovement) : base(playerMovement) {
            autoRun = playerMovement.autoRun;
            jumpStateSave = playerMovement.jumpMovement.jumpState.ToSave();
            stepStateSave = playerMovement.stairMovement.stepState.ToSave();
            
            movespeed = playerMovement.movespeed;
            movespeedMultiplier = playerMovement.movespeedMultiplier;

            playerGravityDirection = Physics.gravity.normalized;
            thisRigidbodyVelocity = playerMovement.thisRigidbody.velocity;
            thisRigidbodyKinematic = playerMovement.thisRigidbody.isKinematic;
            thisRigidbodyUseGravity = playerMovement.thisRigidbody.useGravity;
            thisRigidbodyMass = playerMovement.thisRigidbody.mass;
        }

        public override void LoadSave(PlayerMovement playerMovement) {
            playerMovement.autoRun = autoRun;
            playerMovement.jumpMovement.jumpState.LoadFromSave(jumpStateSave);
            playerMovement.stairMovement.stepState.LoadFromSave(stepStateSave);
            
            playerMovement.movespeed = movespeed;
            playerMovement.movespeedMultiplier = movespeedMultiplier;

            // Don't know a better place to restore gravity direction
            Physics.gravity = Physics.gravity.magnitude * (Vector3) playerGravityDirection;
            playerMovement.thisRigidbody.isKinematic = thisRigidbodyKinematic;
            if (!playerMovement.thisRigidbody.isKinematic) {
                playerMovement.thisRigidbody.velocity = thisRigidbodyVelocity;
            }
            playerMovement.thisRigidbody.useGravity = thisRigidbodyUseGravity;
            playerMovement.thisRigidbody.mass = thisRigidbodyMass;
        }
    }
#endregion
}