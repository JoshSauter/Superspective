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
public class PlayerMovement : SingletonSaveableObject<PlayerMovement, PlayerMovement.PlayerMovementSave>, AudioJobOnGameObject {
#region JumpConfig
    // Jump Settings
    public enum JumpState {
        JumpReady,
        Jumping,
        JumpOnCooldown
    }
    const float _jumpForce = 936;
    const float jumpCooldown = 0.2f; // Time after landing before jumping is available again
    const float minJumpTime = 0.5f; // as long as underMinJumpTime
#endregion

    // For Debugging:
    void SetTimeScale() {
        Time.timeScale = timeScale;
    }
    [OnValueChanged("SetTimeScale")]
    public float timeScale = 1;

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
    
#region StaircaseConfig
    public class StepFound {
        public ContactPoint contact;
        public readonly Vector3 contactNormal;
        public readonly Vector3 stepOffset;

        private Transform player => PlayerMovement.instance.transform;
        public Vector3 stepOffsetVertical => Vector3.Dot(stepOffset, player.up) * player.up;
        public Vector3 stepOffsetHorizontal => Vector3.Dot(stepOffset, -contactNormal) * -contactNormal;

        public StepFound(ContactPoint contact, Vector3 contactNormal, Vector3 stepOffset) {
            this.contact = contact;
            this.contactNormal = contactNormal;
            this.stepOffset = stepOffset;
        }
    }
    
    // Staircase handling characteristics
    private const float _maxStepHeight = 0.6f;
    private const float _minStepHeight = 0.1f;
    private const int framesAfterStepToKeepVelocityZero = 5;
    private const float stepSpeedMultiplier = 2.5f;
    private float stepSpeed => effectiveMovespeed * stepSpeedMultiplier;// _stepSpeed * (1 + Mathf.InverseLerp(movespeed, walkSpeed, runSpeed));
    [ShowNonSerializedField]
    private float distanceMovedForStaircaseOffset = 0;

    public enum StepState {
        StepReady,
        SteppingDiagonal
    }
    public StateMachine<StepState> stepState = new StateMachine<StepState>(StepState.StepReady, true);
    public StepFound currentStep;

    [ShowNativeProperty]
    public Vector3 currentStepDiagonal => (currentStepUp + currentStepForward);
    
    [ShowNativeProperty]
    public Vector3 currentStepUp => currentStep?.stepOffsetVertical + (transform.up * 0.01f) ?? Vector3.zero;
    [ShowNativeProperty]
    // Move in the direction of player desired movement, with the distance == the radius of the player
    public Vector3 currentStepForward => ProjectHorizontalVelocity(lastGroundVelocity.normalized) * (thisCollider == null ? 0 : thisCollider.radius);

    public Vector3 lastGroundVelocity;

    // How far do we move into the step before raycasting down?
    float stepOverbiteMagnitude => effectiveMovespeed * Time.fixedDeltaTime * scale;
    float maxStepHeight => _maxStepHeight * scale;
    float minStepHeight => _minStepHeight * scale;
#endregion

    public bool autoRun;
    public Rigidbody thisRigidbody;

    readonly List<ContactPoint> allContactThisFrame = new List<ContactPoint>();
    PlayerButtonInput input;

    float jumpCooldownRemaining; // Prevents player from jumping again while > 0
    JumpState jumpState = JumpState.JumpReady;


    private bool stopped =>
        PlayerLook.instance.state != PlayerLook.ViewLockState.ViewUnlocked ||
        CameraFlythrough.instance.isPlayingFlythrough ||
        !GameManager.instance.gameHasLoaded;

    CapsuleCollider thisCollider;
    MeshRenderer thisRenderer;
    float timeSpentJumping;
    bool underMinJumpTime; // Used to delay otherwise immediate checks for isGrounded right after jumping

    private float scale => Player.instance.scale;

    [ShowNativeProperty]
    public Vector3 curVelocity => (thisRigidbody == null) ? Vector3.zero : thisRigidbody.velocity;

    public float movespeedMultiplier = 1;

    private float movespeed;
    private float effectiveMovespeed => movespeed * scale;
    public float walkSpeed => _walkSpeed * scale * movespeedMultiplier;
    public float runSpeed => _runSpeed * scale * movespeedMultiplier;
    public float jumpForce => CalculatedJumpForce(desiredJumpHeight * scale, thisRigidbody.mass, Physics.gravity.magnitude);
    public const float desiredJumpHeight = 2.672f;
    public Vector3 bottomOfPlayer => transform.position - (transform.up * 2.5f * scale);
    
    public float CalculatedJumpForce(float wantedHeight, float mass, float g){
        return mass * Mathf.Sqrt( 2 * wantedHeight * g);
    }


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

    // Inspector-only:
    [ShowNativeProperty]
    bool isGrounded => grounded.isGrounded;

    [ShowNativeProperty]
    bool standingOnHeldObject => grounded.standingOnHeldObject;

    [ShowNativeProperty]
    string ground => grounded.ground?.gameObject.name ?? "";

    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    protected override void Awake() {
        base.Awake();
        input = PlayerButtonInput.instance;
        thisRigidbody = GetComponent<Rigidbody>();
        thisCollider = GetComponent<CapsuleCollider>();
        thisRenderer = GetComponentInChildren<MeshRenderer>();
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

        InitStaircaseStateMachine();
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
        if (Settings.Keybinds.AutoRun.Pressed) autoRun = !autoRun;

        bool recentlySteppedUp = stepState.prevState == StepState.SteppingDiagonal && stepState.timeSinceStateChanged < 0.25f;

        bool ShouldUseSprintSpeed() {
            if (recentlySteppedUp) return false;
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
        UpdateGroundedState();
        

        thisRigidbody.isKinematic = stopped || stepState != StepState.StepReady;

        if (timeSlices.Count >= 10) {
            timeSlices.RemoveAt(0);
        }
        timeSlices.Add(new TimeSlice(Time.time, transform.position));

        if (stopped || grounded.standingOnHeldObject) {
            allContactThisFrame.Clear();
            return;
        }

        UpdateJumping();

        Vector3 desiredVelocity = thisRigidbody.velocity;
        desiredVelocity = grounded.isGrounded ? CalculateGroundMovement(grounded.contact) : CalculateAirMovement();
        if (grounded.isGrounded && stepState == StepState.StepReady) {
            lastGroundVelocity = desiredVelocity;
        }
        
        // Prevent player from floating around on cubes they're holding...
        if (grounded.standingOnHeldObject) desiredVelocity += 5 * Physics.gravity * Time.fixedDeltaTime;

        UpdateStaircase(desiredVelocity);

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

        if (!input.LeftStickHeld && !input.JumpHeld && grounded.ground != null &&
            grounded.ground.CompareTag("Staircase"))
            thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        else
            thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        thisRigidbody.useGravity = !grounded.isGrounded;

        if (!thisRigidbody.isKinematic && stepState == StepState.StepReady) {
            debug.Log($"Player velocity: {thisRigidbody.velocity:F2}\nSetting it to: {desiredVelocity}");
            thisRigidbody.velocity = desiredVelocity;
        }

        // Apply wind resistance
        Vector3 projectedVertVelocity = ProjectedVerticalVelocity();
        if (!grounded.isGrounded && Vector3.Dot(Physics.gravity.normalized, projectedVertVelocity.normalized) > 0)
            thisRigidbody.AddForce(
                transform.up * projectedVertVelocity.magnitude * thisRigidbody.mass * windResistanceMultiplier
            );

        allContactThisFrame.Clear();
    }

    void OnCollisionStay(Collision collision) {
        allContactThisFrame.AddRange(collision.contacts);
    }

    /// <summary>
    ///     Calculates player movement when the player is on (or close enough) to the ground.
    ///     Movement is perpendicular to the ground's normal vector.
    /// </summary>
    /// <param name="ground">RaycastHit info for the walkable object that passes the IsGrounded test</param>
    /// <returns>Desired Velocity according to current input</returns>
    Vector3 CalculateGroundMovement(ContactPoint ground) {
        Vector3 up = ground.normal;
        Vector3 right = Vector3.Cross(Vector3.Cross(up, transform.right), up);
        Vector3 forward = Vector3.Cross(Vector3.Cross(up, transform.forward), up);

        Vector3 moveDirection = forward * input.LeftStick.y + right * input.LeftStick.x;
        if (autoRun) moveDirection = forward + right * input.LeftStick.x;

        // DEBUG:
        if (DEBUG) {
            //Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
            Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.blue, 0.1f);
        }

        // If no keys are pressed, decelerate to a stop
        if (!input.LeftStickHeld && !autoRun) {
            Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
            Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                Vector3.zero,
                decelerationLerpSpeed * Time.fixedDeltaTime
            );
            return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
        }

        float adjustedMovespeed = ground.otherCollider.CompareTag("Staircase") ? walkSpeed : effectiveMovespeed;
        debug.Log($"Movespeed: {effectiveMovespeed}");
        return Vector3.Lerp(
            thisRigidbody.velocity,
            moveDirection * adjustedMovespeed,
            accelerationLerpSpeed * Time.fixedDeltaTime
        );
    }

    /// <summary>
    ///     Handles player movement when the player is in the air.
    ///     Movement is perpendicular to Vector3.up.
    /// </summary>
    /// <returns>Desired Velocity according to current input</returns>
    Vector3 CalculateAirMovement() {
        Vector3 moveDirection = input.LeftStick.y * transform.forward + input.LeftStick.x * transform.right;
        if (autoRun) moveDirection = transform.forward;

        // DEBUG:
        Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

        // Handle mid-air collision with obstacles
        moveDirection = AirCollisionMovementAdjustment(moveDirection * effectiveMovespeed);

        // If no keys are pressed, decelerate to a horizontal stop
        if (!input.LeftStickHeld && !autoRun) {
            Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
            Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                Vector3.zero,
                decelerationLerpSpeed * Time.fixedDeltaTime
            );
            return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
        }
        else {
            Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
            Vector3 desiredHorizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                moveDirection,
                airspeedControlFactor * accelerationLerpSpeed * Time.fixedDeltaTime
            );
            return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
        }
    }

    /// <summary>
    ///     Checks the area in front of where the player wants to move for an obstacle.
    ///     If one is found, adjusts the player's movement to be parallel to the obstacle's face.
    /// </summary>
    /// <param name="movementVector"></param>
    /// <returns>True if there is something in the way of the player's desired movement vector, false otherwise.</returns>
    Vector3 AirCollisionMovementAdjustment(Vector3 movementVector) {
        float rayDistance = effectiveMovespeed * Time.fixedDeltaTime + thisCollider.radius;
        RaycastHit obstacle = new RaycastHit();
        Physics.Raycast(transform.position, movementVector, out obstacle, rayDistance);
        
        if (obstacle.collider == null || obstacle.collider.isTrigger ||
            (obstacle.collider.gameObject.GetComponent<PickupObject>()?.isHeld ?? false)) {
            return movementVector;
        }

        Vector3 newMovementVector = Vector3.ProjectOnPlane(movementVector, obstacle.normal);
        if (Vector3.Dot(ProjectedVerticalVelocity(), newMovementVector) > 0)
            debug.LogWarning("movementVector:" + movementVector + "\nnewMovementVector:" + newMovementVector);
        return newMovementVector;
    }

    IEnumerator PrintMaxHeight(Vector3 startPosition) {
        float maxHeight = 0;
        float maxAdjustedHeight = 0;
        yield return new WaitForSeconds(minJumpTime / 2f);
        while (!grounded.isGrounded) {
            float height = Vector3.Dot(transform.up, transform.position - startPosition);
            if (height > maxHeight) maxHeight = height;
            float adjustedHeight = Vector3.Dot(transform.up, transform.position - startPosition) / Player.instance.scale;
            if (adjustedHeight > maxAdjustedHeight) maxAdjustedHeight = adjustedHeight;
            yield return new WaitForFixedUpdate();
        }

        debug.Log($"Highest jump height: {maxHeight}, (adjusted: {maxAdjustedHeight})");
    }

    /// <summary>
    ///     Removes any current y-direction movement on the player, applies a one time impulse force to the player upwards,
    ///     then waits jumpCooldown seconds to be ready again.
    /// </summary>
    void Jump() {
        OnJump?.Invoke();
        AudioManager.instance.PlayOnGameObject(AudioName.PlayerJump, ID, this);

        timeSpentJumping = 0.0f;
        underMinJumpTime = true;
        grounded.isGrounded = false;

        Vector3 jumpVector = -Physics.gravity.normalized * jumpForce;
        
        if (stepState != StepState.StepReady) {
            stepState.Set(StepState.StepReady);
            thisRigidbody.isKinematic = false;
        }
        thisRigidbody.velocity = thisRigidbody.velocity.WithY(0);
        thisRigidbody.AddForce(jumpVector, ForceMode.Impulse);
        StartCoroutine(PrintMaxHeight(transform.position));

        jumpState = JumpState.Jumping;
    }

    void UpdateJumping() {
        switch (jumpState) {
            case JumpState.JumpReady:
                if (input.JumpHeld && grounded.isGrounded && !grounded.standingOnHeldObject) Jump();
                return;
            case JumpState.Jumping:
                timeSpentJumping += Time.fixedDeltaTime;
                underMinJumpTime = timeSpentJumping < minJumpTime;
                if (underMinJumpTime) return;
                else if (grounded.isGrounded) {
                    jumpCooldownRemaining = jumpCooldown;
                    OnJumpLanding?.Invoke();
                    jumpState = JumpState.JumpOnCooldown;
                }

                return;
            case JumpState.JumpOnCooldown:
                jumpCooldownRemaining = Mathf.Max(jumpCooldownRemaining - Time.fixedDeltaTime, 0.0f);
                if (jumpCooldownRemaining == 0.0f) jumpState = JumpState.JumpReady;
                return;
        }
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
    
#region Staircase Handling
    void InitStaircaseStateMachine() {
        // // Transition from moving vertically to moving horizontally once we've moved far enough vertically
        // stepState.AddStateTransition(StepState.SteppingVertical, StepState.SteppingHorizontal, () =>
        //     distanceMovedForStaircaseOffset >= currentStepUp.magnitude
        // );
        // // Transition from moving horizontally to being ready to find a new step once we've moved far enough horizontally
        // stepState.AddStateTransition(StepState.SteppingHorizontal, StepState.StepReady, () =>
        //     distanceMovedForStaircaseOffset >= currentStepForward.magnitude
        // );
        // Transition from moving diagonally to being ready to find a new step once we've moved far enough
        stepState.AddStateTransition(StepState.SteppingDiagonal, StepState.StepReady, () =>
            distanceMovedForStaircaseOffset >= currentStepDiagonal.magnitude
        );
        
        stepState.AddTrigger(StepState.SteppingDiagonal, () => Debug.DrawRay(bottomOfPlayer, currentStepDiagonal, Color.yellow, 10));
        
        // Reset distance moved whenever we change state
        stepState.OnStateChangeSimple += () => distanceMovedForStaircaseOffset = 0f;
    }

    void UpdateStaircase(Vector3 desiredVelocity) {
        void MoveAlongStep(Vector3 moveDirection) {
            // How much distance do we actually have left to go
            float distanceRemaining = moveDirection.magnitude - distanceMovedForStaircaseOffset;
            // Either set the distance to move to full speed or whatever's left to move
            float distanceToMove = Mathf.Min(stepSpeed * Time.fixedDeltaTime, distanceRemaining);
            Vector3 diff = moveDirection.normalized * distanceToMove;
            
            debug.LogWarning($"Offset this frame: {diff:F3}");
            // Move the player up, record the distance moved
            transform.Translate(diff, Space.World);
            distanceMovedForStaircaseOffset += distanceToMove;
        }
        
        switch (stepState.state) {
            case StepState.StepReady:
                currentStep = DetectStep(desiredVelocity, grounded.contact, grounded.isGrounded);
                if (currentStep != null) {
                    //stepState.Set(StepState.SteppingVertical);
                    stepState.Set(StepState.SteppingDiagonal);
                    Player.instance.cameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
                    if (Vector3.Dot(transform.up, currentStep.stepOffset) > 0) OnStaircaseStepUp?.Invoke();
                }
                break;
            case StepState.SteppingDiagonal:
                MoveAlongStep(currentStepDiagonal);
                break;
            // case StepState.SteppingVertical:
            //     MoveAlongStep(currentStepUp);
            //     break;
            // case StepState.SteppingHorizontal:
            //     MoveAlongStep(currentStepForward);
            //     break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    StepFound DetectStep(Vector3 desiredVelocity, ContactPoint ground, bool isGrounded) {
        // If player is not moving, don't do any raycasts, just return
        if (desiredVelocity.magnitude < 0.1f) return null;

        foreach (ContactPoint contact in allContactThisFrame) {
            if (contact.otherCollider == null) continue;

            bool isBelowMaxStepHeight =
                Mathf.Abs(Vector3.Dot(contact.point, transform.up) - Vector3.Dot(ground.point, transform.up)) <
                maxStepHeight;
            // Basically all this nonsense is to get the contact surface's normal rather than the "contact normal" which is different
            RaycastHit hitInfo = default;
            bool rayHit = false;
            if (isBelowMaxStepHeight) {
                Vector3 rayLowStartPos = bottomOfPlayer + transform.up * 0.01f;
                Vector3 bottomOfPlayerToContactPoint = contact.point - bottomOfPlayer;
                Vector3 rayDirection = Vector3.ProjectOnPlane(bottomOfPlayerToContactPoint, transform.up).normalized;
                if (rayDirection.magnitude > 0) {
                    Debug.DrawRay(rayLowStartPos, rayDirection * (thisCollider.radius * 2), Color.blue);
                    rayHit = contact.otherCollider.Raycast(
                        new Ray(rayLowStartPos, rayDirection),
                        out hitInfo,
                        thisCollider.radius * 2
                    );
                }
            }

            bool isWallNormal = rayHit && Mathf.Abs(Vector3.Dot(hitInfo.normal, transform.up)) < 0.1f;
            bool isInDirectionOfMovement = rayHit && Vector3.Dot(-hitInfo.normal, desiredVelocity.normalized) > 0f;
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
                               stepOverbiteMagnitude;

        // Start the raycast position directly above the contact point with the step, at the vertical position of the bottom of the player
        // According to my shitty vector math, this is equivalent to Proj(contact point onto up) + up * Dot(up, bottomOfPlayer)
        Vector3 smarterRaycastStartPos = Vector3.ProjectOnPlane(contact.point, transform.up) + transform.up * Vector3.Dot(transform.up, bottomOfPlayer);
        Debug.DrawRay(smarterRaycastStartPos, transform.up * maxStepHeight, Color.magenta, 10);
        
        // Old way of calculating raycastStartPos above contact point
        //Debug.DrawRay(contact.point, transform.up * maxStepHeight, Color.blue, 10);
        //Vector3 raycastStartPos = contact.point + transform.up * maxStepHeight;

        Vector3 raycastStartPos = smarterRaycastStartPos + transform.up * maxStepHeight;
        // Move the raycast inwards towards the stair (we will be raycasting down at the stair)
        Debug.DrawRay(raycastStartPos, stepOverbite, Color.red, 10);
        raycastStartPos += stepOverbite;
        Vector3 direction = -transform.up;

        Debug.DrawRay(raycastStartPos, direction * maxStepHeight, Color.green, 10);
        bool stepFound = contact.otherCollider.Raycast(
            new Ray(raycastStartPos, direction),
            out stepTest,
            maxStepHeight
        );
        if (stepFound) {
            bool stepIsGround = Vector3.Dot(stepTest.normal, transform.up) > isGroundThreshold;
            if (!stepIsGround) return false;
            
            float stepHeight = Vector3.Dot(transform.up, stepTest.point - bottomOfPlayer);

            if (stepHeight < minStepHeight) return false;

            Vector3 stepOffset = stepOverbite + transform.up * stepHeight;
            //Debug.DrawRay(smarterRaycastStartPos, stepOffset, Color.yellow, 10);
            step = new StepFound(contact, contactNormal, stepOffset);
            debug.Log($"Step: {contact}\n{stepOffset:F3}\nstepHeight:{stepHeight}");
        }

        return stepFound;
    }
#endregion

    public void UpdateGroundedState() {
        ContactPoint groundContactPoint = default;
        float maxGroundTest = isGroundThreshold; // Amount upwards-facing the most ground-like object is
        foreach (ContactPoint contact in allContactThisFrame) {
            float groundTest = Vector3.Dot(contact.normal, transform.up);
            if (groundTest > maxGroundTest) {
                groundContactPoint = contact;
                maxGroundTest = groundTest;
            }
        }

        // Was a ground object found?
        bool isGroundedNow = maxGroundTest > isGroundThreshold && !underMinJumpTime;
        if (isGroundedNow) {
            grounded.framesWaitedAfterLeavingGround = 0;
            grounded.contact = groundContactPoint;
        }
        // If we were grounded last FixedUpdate and not grounded now
        else if (grounded.isGrounded) {
            // Wait a few fixed updates before saying that the player is ungrounded
            if (grounded.framesWaitedAfterLeavingGround >= framesToWaitAfterLeavingGround) {
                grounded.isGrounded = false;
            }
            else
                grounded.framesWaitedAfterLeavingGround++;
        }
        // Not grounded anytime recently
        else
            grounded.contact = default;
    }

    bool IsStandingOnHeldObject(ContactPoint contact) {
        PickupObject maybeCube1 = null, maybeCube2 = null;
        if (contact.thisCollider != null) maybeCube1 = contact.thisCollider.GetComponent<PickupObject>();
        if (contact.otherCollider != null) maybeCube2 = contact.otherCollider.GetComponent<PickupObject>();
        bool cube1IsHeld = maybeCube1 != null && maybeCube1.isHeld;
        bool cube2IsHeld = maybeCube2 != null && maybeCube2.isHeld;
        //debug.Log($"Grounded: {grounded.isGrounded}\nCube1IsHeld: {cube1IsHeld}\nCube2IsHeld: {cube2IsHeld}");
        return cube1IsHeld || cube2IsHeld;
    }

    public void StopMovement() {
        thisRigidbody.velocity = Vector3.zero;
    }

    public bool WalkingOnGlass() {
        if (grounded.isGrounded == false) {
            return false;
        }

        bool onGlass = grounded
            .ground
            .GetMaybeComponent<Renderer>()
            .Exists(r => r.sharedMaterials.Where(m => m != null).ToArray()[0].name.ToLower().Contains("glass"));

        return onGlass;
    }

#region IsGrounded characteristics
    public struct GroundedState {
        public bool isGrounded {
            get => ground != null;
            // Only allow setting isGrounded to false (clear the state)
            set {
                if (!value) {
                    contact = default;
                    framesWaitedAfterLeavingGround = 0;
                }
            }
        }

        public Collider ground => contact.otherCollider;
        public bool standingOnHeldObject => instance.IsStandingOnHeldObject(contact);

        public ContactPoint contact;
        public float framesWaitedAfterLeavingGround;
    }

    public GroundedState grounded;

    const int framesToWaitAfterLeavingGround = 10;

    // Dot(face normal, transform.up) must be greater than this value to be considered "ground"
    public const float isGroundThreshold = 0.675f;
    public const float isGroundedSpherecastDistance = 0.5f;
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

        float jumpCooldownRemaining;

        int jumpState;
        float movespeed;
        float movespeedMultiplier;

        SerializableVector3 playerGravityDirection;

        bool thisRigidbodyKinematic;
        float thisRigidbodyMass;
        bool thisRigidbodyUseGravity;
        SerializableVector3 thisRigidbodyVelocity;
        float timeSpentJumping;
        bool underMinJumpTime;

        public PlayerMovementSave(PlayerMovement playerMovement) : base(playerMovement) {
            autoRun = playerMovement.autoRun;
            jumpState = (int) playerMovement.jumpState;
            timeSpentJumping = playerMovement.timeSpentJumping;

            jumpCooldownRemaining = playerMovement.jumpCooldownRemaining;
            underMinJumpTime = playerMovement.underMinJumpTime;
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
            playerMovement.jumpState = (JumpState) jumpState;
            playerMovement.timeSpentJumping = timeSpentJumping;

            playerMovement.jumpCooldownRemaining = jumpCooldownRemaining;
            playerMovement.underMinJumpTime = underMinJumpTime;
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