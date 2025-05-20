using System;
using System.Collections;
using System.Linq;
using Audio;
using DissolveObjects;
using GrowShrink;
using LevelManagement;
using NaughtyAttributes;
using NovaMenuUI;
using SuperspectiveUtils;
using PortalMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Audio.AudioManager;
using CubeSpawnerReference = SerializableClasses.SuperspectiveReference<CubeSpawner, CubeSpawner.CubeSpawnerSave>;

[RequireComponent(typeof(UniqueId))]
public class PickupObject : SuperspectiveObject<PickupObject, PickupObject.PickupObjectSave>, AudioJobOnGameObject {
    public delegate void PickupObjectAction(PickupObject obj);

    public delegate void PickupObjectSimpleAction();

    const float SLEEP_THRESHOLD = 0.005f;
    const float MAX_SCALE_DIFFERENCE_BETWEEN_PLAYER_CUBE = 6f;
    const float ROOT_3_ON_2 = 0.86602540378f;
    const float PICKUP_DROP_COOLDOWN = 0.2f;
    const float HOLD_DISTANCE = 2.25f;
    const float MIN_DISTANCE_FROM_PLAYER = 0.25f;
    const float FOLLOW_SPEED = 15;
    const float FOLLOW_LERP_SPEED = 15;
    const float RIGIDBODY_SLEEP_TIME = 0.25f;
    
    public static PickupObjectSimpleAction OnAnyPickupSimple;
    public static PickupObjectSimpleAction OnAnyDropSimple;
    public static PickupObjectAction OnAnyPickup;
    public static PickupObjectAction OnAnyDrop;

    static float currentPitch = 1f;

    public DynamicObject thisDynamicObject;
    public bool isReplaceable = true;
    public bool isHeld;
    public CubeReceptacle receptacleHeldIn;
    private bool IsHeldInReceptacle => receptacleHeldIn != null;
    public bool shouldFollow = true;
    public GravityObject thisGravity;
    public bool freezeRigidbodyDueToNearbyPlayer = false;

    private bool RigidbodyShouldBeFrozen => GameManager.instance.IsCurrentlyLoading ||
                                            freezeRigidbodyDueToNearbyPlayer ||
                                            IsHeldInReceptacle ||
                                            (CubeIsTooBigToPickUp && rigidbodySleepingStateMachine == RigidbodySleepingState.Sleeping);
    public Rigidbody thisRigidbody;
    public Collider thisCollider;

    public PortalableObject portalableObject;
    public GrowShrinkObject growShrinkObject;
    public CubeSpawnerReference spawnedFrom;
    
    // Assumes a transform scale of 1,1,1 corresponds to 1 unit^3 volume
    private float RadiusOfCircumscribedSphere => transform.lossyScale.x * ROOT_3_ON_2;

    public float HoldDistance => HOLD_DISTANCE * Scale;

    [SerializeField]
    private SuperspectiveRaycast lastRaycast;

    public float Scale => (growShrinkObject != null ? growShrinkObject.CurrentScale : 1f);
    public float MinOfPlayerCubeScales => Mathf.Min(Player.instance.Scale, Scale);
    
    public bool CubeIsTooBigToPickUp => Scale > MAX_SCALE_DIFFERENCE_BETWEEN_PLAYER_CUBE * Player.instance.Scale;
    
    const float scaleMultiplier = 1.5f;
    float currentCooldown;
    InteractableGlow interactableGlow;
    InteractableObject interactableObject;
    public PickupObjectAction OnDrop;
    public PickupObjectSimpleAction OnDropSimple;
    public PickupObjectAction OnPickup;
    public PickupObjectSimpleAction OnPickupSimple;
    AudioJob PickupSound => AudioManager.instance.GetOrCreateJob(AudioName.CubePickup, ID);
    Transform playerCam;

    private PhysicMaterial defaultPickupObjectPhysicsMaterial;
    public PhysicMaterial heldPickupObjectPhysicsMaterial;
    private PhysicMaterial EffectivePhysicsMaterial => isHeld ? heldPickupObjectPhysicsMaterial : defaultPickupObjectPhysicsMaterial;

    Vector3 playerLastPos;
    Vector3 playerCamPosLastFrame;

    readonly float rotateToRightAngleTime = 0.35f;

    public Transform GetObjectToPlayAudioOn(AudioJob _) => transform;

    public bool interactable = true;

    bool OnCooldown => currentCooldown > 0;

    // For some reason, Unit's Rigidbody is not sleeping when it should be. This is a workaround.
    public enum RigidbodySleepingState : byte {
        Moving, // Rigidbody is moving
        Steady, // Rigidbody is not moving but not yet sleeping
        Sleeping // Rigidbody has been not moving for long enough to be considered sleeping
    }
    [SerializeField, ReadOnly]
    private StateMachine<RigidbodySleepingState> rigidbodySleepingStateMachine;

    // Lock the cube into 90 degree angles when the player presses the button for it
    public enum FreezeRotationState : byte {
        FreelyRotating,
        RotatingToRightAngle,
        Frozen
    }
    [SerializeField, ReadOnly]
    public StateMachine<FreezeRotationState> freezeRotationStateMachine;

    protected override void OnDisable() {
        base.OnDisable();
        PlayerButtonInput.instance.OnInteractPress -= Drop;
        Portal.OnAnyPortalPlayerTeleport -= UpdatePlayerPositionLastFrameAfterPortal;
        TeleportEnter.OnAnyTeleportSimple -= UpdatePlayerPositionLastFrameAfterTeleport;
    }

    protected override void Awake() {
        base.Awake();
        AssignReferences();
        
        rigidbodySleepingStateMachine = this.StateMachine(RigidbodySleepingState.Moving);
        freezeRotationStateMachine = this.StateMachine(FreezeRotationState.FreelyRotating);
        
        defaultPickupObjectPhysicsMaterial = thisCollider.material;
    }

    private void SetUpRigidbodySleepingStateMachine() {
        rigidbodySleepingStateMachine.AddStateTransition(RigidbodySleepingState.Moving, RigidbodySleepingState.Steady, () => thisRigidbody.GetMassNormalizedKineticEnergy() < thisRigidbody.sleepThreshold * Scale);
        rigidbodySleepingStateMachine.AddStateTransition(RigidbodySleepingState.Steady, RigidbodySleepingState.Sleeping, RIGIDBODY_SLEEP_TIME);
        
        rigidbodySleepingStateMachine.AddStateTransition(RigidbodySleepingState.Steady, RigidbodySleepingState.Moving, () => thisRigidbody.GetMassNormalizedKineticEnergy() >= thisRigidbody.sleepThreshold * Scale);
        rigidbodySleepingStateMachine.AddStateTransition(RigidbodySleepingState.Sleeping, RigidbodySleepingState.Moving, () => thisRigidbody.GetMassNormalizedKineticEnergy() >= thisRigidbody.sleepThreshold * Scale);
    }
    
    private void SetupFreezeRotationStateMachine() {
        freezeRotationStateMachine.AddStateTransition(FreezeRotationState.FreelyRotating, FreezeRotationState.RotatingToRightAngle, () => isHeld && PlayerButtonInput.instance.AlignObjectPressed);
        // State transition from RotatingToRightAngle to Frozen is handled in the coroutine RotateToRightAngleAndFreezeRotation
        freezeRotationStateMachine.AddStateTransition(FreezeRotationState.Frozen, FreezeRotationState.FreelyRotating, () => isHeld && PlayerButtonInput.instance.AlignObjectPressed);
        
        freezeRotationStateMachine.AddTrigger(FreezeRotationState.RotatingToRightAngle, () => {
            StartCoroutine(RotateToRightAngleAndFreezeRotation(RightAngleRotations.GetNearest(transform.rotation)));
        });
        
        freezeRotationStateMachine.AddTrigger(FreezeRotationState.Frozen, () => {
            thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        });
        
        freezeRotationStateMachine.AddTrigger(FreezeRotationState.FreelyRotating, () => {
            thisRigidbody.constraints = RigidbodyConstraints.None;
        });
    }

    void AssignReferences() {
        if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
        if (thisGravity == null) thisGravity = GetComponent<GravityObject>();
        if (thisCollider == null) thisCollider = GetComponent<Collider>();
        if (thisDynamicObject == null) thisDynamicObject = GetComponent<DynamicObject>();

        interactableObject = thisRigidbody.GetOrAddComponent<InteractableObject>();
        interactableObject.SkipSave = true;
        interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

        interactableGlow = interactableObject.GetOrAddComponent<InteractableGlow>();

        if (portalableObject == null) portalableObject = GetComponent<PortalableObject>();
        if (growShrinkObject == null) growShrinkObject = GetComponent<GrowShrinkObject>();
    }

    protected override void Start() {
        base.Start();
        playerCam = SuperspectiveScreen.instance.playerCamera.transform;

        PlayerButtonInput.instance.OnInteractPress += Drop;
        
        PillarDimensionObject thisDimensionObject = transform.FindDimensionObjectRecursively<PillarDimensionObject>();
        if (thisDimensionObject != null) thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;

        playerCamPosLastFrame = playerCam.transform.position;

        Portal.OnAnyPortalPlayerTeleport += UpdatePlayerPositionLastFrameAfterPortal;
        TeleportEnter.OnAnyTeleportSimple += UpdatePlayerPositionLastFrameAfterTeleport;

        AudioManager.instance.GetOrCreateJob(AudioName.CubePickup, ID);
        
        // TODO: Remove this; temp for testing rolling sphere
        thisRigidbody.maxAngularVelocity = 120;

        SetUpRigidbodySleepingStateMachine();
        SetupFreezeRotationStateMachine();
    }

    protected override void Init() {
        base.Init();
        // This line is important to get the state machine to Init its events properly :/
        freezeRotationStateMachine.Set(freezeRotationStateMachine.State, true);
    }

    void Update() {
        RecalculateRigidbodyKinematics();
        
        if (interactable) {
            if (CubeIsTooBigToPickUp) {
                interactableObject.SetAsDisabled("(Too large)");
            }
            else {
                interactableObject.SetAsInteractable(isHeld ? "Drop" : "Pick up");
            }
        }
        else {
            interactableObject.SetAsHidden();
        }
        
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        // Don't allow clicks in the menu to propagate to picking up/dropping the cube
        if (NovaPauseMenu.instance.PauseMenuIsOpen) currentCooldown = PICKUP_DROP_COOLDOWN;

        if (thisDynamicObject) thisDynamicObject.globalMode = isHeld ? DynamicObject.GlobalMode.NoContactSceneChange : DynamicObject.GlobalMode.Global;
        
        if (isHeld) {
            interactableGlow.TurnOnGlow();
            
            // Change scenes dynamically if the cube is picked up and the player moves to a different scene
            Scene activeScene = SceneManager.GetSceneByName(LevelManager.instance.activeSceneName);
            if (gameObject.scene != activeScene && thisDynamicObject) {
                // Force scene change regardless of normal settings
                DynamicObject.GlobalMode temp = thisDynamicObject.globalMode;
                thisDynamicObject.globalMode = DynamicObject.GlobalMode.NoContactSceneChange;
                thisDynamicObject.ChangeScene(activeScene);
                thisDynamicObject.globalMode = temp;
            }
        }
        else {
            thisRigidbody.sleepThreshold = SLEEP_THRESHOLD * Scale;
        }
    }

    private float _objectWidthTowardsCamera;
    float GetObjectWidthTowardsCamera() {
        Vector3 camPos = playerCam.position;
        Vector3 playerCamToPickupObj = transform.position - camPos;
        Ray ray = new Ray(camPos, playerCamToPickupObj);
        if (thisCollider.Raycast(ray, out RaycastHit raycastHit, playerCamToPickupObj.magnitude)) {
            Vector3 hitPos = raycastHit.point;
            _objectWidthTowardsCamera = playerCamToPickupObj.magnitude - Vector3.Distance(hitPos, camPos);
        }

        return _objectWidthTowardsCamera;
    }

    void OffsetByCameraMovement() {
        // Move the cube by the same amount the player's camera moved
        Vector3 playerCamPositionalDiff = Player.instance.cameraFollow.transform.position - playerCamPosLastFrame;
        // Calculate the positional difference relative to the player's gravity direction
        if (portalableObject && portalableObject.IsHeldThroughPortal) {
            playerCamPositionalDiff = portalableObject.PortalHeldThrough.TransformDirection(playerCamPositionalDiff);
        }

        debug.Log($"Positional diff: {playerCamPositionalDiff:F3}");
        Vector3 positionBefore = thisRigidbody.position;
        thisRigidbody.MovePosition(transform.position + playerCamPositionalDiff);
        Vector3 positionAfter = thisRigidbody.position;
        debug.Log($"Position before: {positionBefore:F3}\nPosition after: {positionAfter:F3}\nActual positional diff: {(positionAfter - positionBefore):F3}");
    }

    private Vector3 debugTargetPos = Vector3.zero;
    void FixedUpdate() {
        if (!GameManager.instance.gameHasLoaded) return;
        
        if (thisCollider.sharedMaterial != EffectivePhysicsMaterial) thisCollider.sharedMaterial = EffectivePhysicsMaterial;
        
        if (isHeld && shouldFollow) {
            OffsetByCameraMovement();

            float pickupObjRadiusSpacer = GetObjectWidthTowardsCamera();
            debug.Log($"Pickup object radius spacer: {pickupObjRadiusSpacer:F3}, circumscribed radius: {RadiusOfCircumscribedSphere:F3}");

            Vector3 targetPos = !portalableObject
                ? TargetHoldPosition(out SuperspectiveRaycast raycastHits)
                : TargetHoldPositionThroughPortal(out raycastHits);
            
            debugTargetPos = targetPos;

            Vector3 cubeToTarget = SuperspectivePhysics.ShortestVectorPointToPoint(thisRigidbody.position,  targetPos);
            Vector3 newVelocity = Vector3.Lerp(
                thisRigidbody.velocity,
                // Slow down the cube if it's larger than the player (since it's large, it takes more force to move around)
                cubeToTarget * (FOLLOW_SPEED * Mathf.Min(1f, Player.instance.Scale / Scale)),
                FOLLOW_LERP_SPEED * Time.fixedDeltaTime
            );
            bool movingTowardsPlayer =
                Vector3.Dot(newVelocity.normalized, -raycastHits.raycastParts.Last().ray.direction) > 0.5f;
            if (raycastHits.distance < MIN_DISTANCE_FROM_PLAYER && movingTowardsPlayer) {
                newVelocity = Vector3.ProjectOnPlane(newVelocity, raycastHits.raycastParts.Last().ray.direction);
            }

            //Vector3 velBefore = thisRigidbody.velocity;
            thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
            //debug.Log("Before: " + velBefore.ToString("F3") + "\nAfter: " + thisRigidbody.velocity.ToString("F3"));
        }

        ResolveCollision();

        playerCamPosLastFrame = playerCam.transform.position;
        playerLastPos = PlayerMovement.instance.lastPlayerPosition;
    }
    
    public void OnLeftMouseButtonDown() {
        Pickup();
    }

    void UpdatePlayerPositionLastFrameAfterPortal(Portal inPortal) {
        playerCamPosLastFrame = inPortal.TransformPoint(playerCamPosLastFrame);
    }

    void UpdatePlayerPositionLastFrameAfterTeleport() {
        // TODO:
    }

    IEnumerator RotateToRightAngleAndFreezeRotation(Quaternion destinationRotation) {
        thisRigidbody.angularVelocity = Vector3.zero;

        float timeElapsed = 0f;

        Quaternion startRot = transform.rotation;

        while (timeElapsed < rotateToRightAngleTime) {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / rotateToRightAngleTime;

            transform.rotation = Quaternion.Lerp(startRot, destinationRotation, t);

            yield return null;
        }

        transform.rotation = destinationRotation;
        // An attempt to fix an esoteric bug causing a cube to reset its rotation. I don't know why the rotation resets sometimes
        yield return new WaitForFixedUpdate();
        transform.rotation = destinationRotation;
        freezeRotationStateMachine.Set(FreezeRotationState.Frozen);
    }

    Vector3 TargetHoldPosition(out SuperspectiveRaycast raycastHits) {
        int ignoreRaycastLayer = SuperspectivePhysics.IgnoreRaycastLayer;
        int layerMask = SuperspectivePhysics.PhysicsRaycastLayerMask;
        int tempLayer = thisCollider.gameObject.layer;
        thisCollider.gameObject.layer = ignoreRaycastLayer;

        raycastHits = DoRaycast();
        
        Vector3 targetPos = PositionAtFirstObjectOrEndOfRaycast(raycastHits);
        thisCollider.gameObject.layer = tempLayer;

        return targetPos;
    }
    
    // Sets the layer mask for the object and its portal copy (if relevant), and returns the previous layer mask for the object and its portal copy
    (int, int) SetLayerMasks(int layer, int portalCopyLayer = -1) {
        if (portalCopyLayer < 0) portalCopyLayer = layer; // Default to the same layer as the object
            
        int prevLayer = thisCollider.gameObject.layer;
        thisCollider.gameObject.layer = layer;

        int prevLayerPortalCopy = -1;
        if (portalCopyLayer >= 0) {
            if (portalCopyLayer != portalableObject.PortalCopy.gameObject.layer) {
                prevLayerPortalCopy = portalableObject.PortalCopy.gameObject.layer;
                foreach (Collider portalCopyCollider in portalableObject.PortalCopy.colliders) {
                    portalCopyCollider.gameObject.layer = portalCopyLayer;
                }
            }
            else {
                prevLayerPortalCopy = prevLayer;
            }
        }

        return (prevLayer, prevLayerPortalCopy);
    }
    
    Vector3 TargetHoldPositionThroughPortal(out SuperspectiveRaycast raycastHits) {
        // Temporarily sets the layer mask for the object and its portal copy (if relevant) to the IgnoreRaycast layer
        (int prevLayer, int prevLayerPortalCopy) = SetLayerMasks(SuperspectivePhysics.IgnoreRaycastLayer);

        raycastHits = DoRaycast();
        
        // Restores the layer mask for the object and its portal copy
        SetLayerMasks(prevLayer, prevLayerPortalCopy);
        
        return PositionAtFirstObjectOrEndOfRaycast(raycastHits);
    }

    SuperspectiveRaycast DoRaycast() {
        Vector3 raycastStartPos = Player.instance.AdjustedCamPos;
        lastRaycast = RaycastUtils.Raycast(raycastStartPos, playerCam.forward, HoldDistance + RadiusOfCircumscribedSphere, SuperspectivePhysics.PhysicsRaycastLayerMask, true);
        return lastRaycast;
    }

    Vector3 PositionAtFirstObjectOrEndOfRaycast(SuperspectiveRaycast raycast) {
        Vector3 result = raycast.DidHitObject ? raycast.FirstObjectHit.point : raycast.FinalPosition;
        if (DEBUG) {
            DebugDraw.Sphere(ID, result, 0.25f, Color.green);
        }

        return result;
    }

    void HandleDimensionObjectStateChange(DimensionObject dimObj) {
        if (dimObj.visibilityState == VisibilityState.Invisible && isHeld) Drop();
    }

    // Singular place to set isKinematic so I can track it in one place when debugging
    public void SetRigidbodyIsKinematic(bool isKinematic) {
        if (thisRigidbody.isKinematic != isKinematic) {
            debug.Log($"Setting thisRigidbody.isKinematic to {isKinematic}");
            thisRigidbody.isKinematic = isKinematic;
        }
    }

    void SetHeldThroughPortal() {
        if (!portalableObject) return;
        // Three possible conditions:
        // 1) The cube is all the way through a portal (not sitting in it), and the player is interacting with it through that portal
        // 2) The cube is sitting in the portal closer to the player, and

        SuperspectiveRaycast interactRaycast = Interact.instance.raycast;

        // The cube is sitting all the way through a portal:
        if (!portalableObject.IsInPortal && interactRaycast.DidHitPortal) {
            portalableObject.SetPortalHeldThrough(interactRaycast.FirstValidPortalHit);
        }
        else if (portalableObject.IsInPortal) {
            bool interactedWithPortalCopy = interactRaycast.DidHitObject && portalableObject.PortalCopy.colliders.Contains(interactRaycast.FirstObjectHit.collider);
            if (interactedWithPortalCopy == interactRaycast.DidHitPortal) {
                portalableObject.SetPortalHeldThrough(null);
            }
            else {
                portalableObject.SetPortalHeldThrough(portalableObject.Portal.otherPortal);
            }
            // This code is simplified to the above ^
            // (interactedWithPortalCopy, interactRaycast.DidHitPortal) switch {
            //     // Interacted with the PortalCopy through a portal, the cube is thus not held through a portal
            //     (true, true) => portalableObject.SetPortalHeldThrough(null),
            //     // Interacted with the PortalCopy, the cube is on the other side of the portal
            //     (true, false) => portalableObject.SetPortalHeldThrough(portalableObject.Portal.otherPortal),
            //     // Interacted with the real cube through a portal, the cube is held through that portal
            //     (false, true) => portalableObject.SetPortalHeldThrough(portalableObject.Portal.otherPortal),
            //     // Interacted with the cube with no portal hit, the cube is not held through a portal
            //     (false, false) => portalableObject.SetPortalHeldThrough(null)
            // };
        }
        else {
            portalableObject.SetPortalHeldThrough(null);
        }
    }

    private void UpdateGravity() {
        Vector3 targetGravityDirection = Physics.gravity.normalized;
        if (portalableObject && portalableObject.IsHeldThroughPortal) {
            targetGravityDirection = portalableObject.PortalHeldThrough.TransformDirection(Physics.gravity.normalized);
        }
        thisGravity.GravityDirection = targetGravityDirection;
    }

    public void Pickup() {
        if (!isHeld && !OnCooldown && interactable) {
            thisGravity.useGravity = false;
            SetHeldThroughPortal();
            UpdateGravity();
            SetRigidbodyIsKinematic(false);
            isHeld = true;
            currentCooldown = PICKUP_DROP_COOLDOWN;

            // Pitch goes 1 -> 1.25 -> 1.5 -> 1
            currentPitch = (currentPitch - .75f) % .75f + 1f;
            PickupSound.basePitch = currentPitch;
            AudioManager.instance.PlayOnGameObject(AudioName.CubePickup, ID, this, true);
            
            SuperspectivePhysics.IgnoreCollision(thisCollider, Player.instance.collider, ID);

            OnPickupSimple?.Invoke();
            OnPickup?.Invoke(this);
            OnAnyPickupSimple?.Invoke();
            OnAnyPickup?.Invoke(this);
        }
    }

    public void Drop() {
        if (isHeld && !OnCooldown && interactable) {
            thisGravity.useGravity = true;
            thisRigidbody.velocity += (portalableObject && portalableObject.IsHeldThroughPortal) ?
                portalableObject.PortalHeldThrough.TransformDirection(PlayerMovement.instance.thisRigidbody.velocity) :
                PlayerMovement.instance.thisRigidbody.velocity;

            isHeld = false;
            currentCooldown = PICKUP_DROP_COOLDOWN;

            UpdateGravity();
            
            debug.Log("Dropping cube!");

            AudioManager.instance.PlayOnGameObject(AudioName.CubeDrop, ID, this, true);
            
            SuperspectivePhysics.RestoreCollision(thisCollider, Player.instance.collider, ID);
            
            freezeRotationStateMachine.Set(FreezeRotationState.FreelyRotating);

            OnDropSimple?.Invoke();
            OnDrop?.Invoke(this);
            OnAnyDropSimple?.Invoke();
            OnAnyDrop?.Invoke(this);
        }
    }

    public void Dematerialize() {
        DynamicObject dynamicObject = gameObject.GetComponent<DynamicObject>();
        DissolveObject dissolve = gameObject.GetOrAddComponent<DissolveObject>();
        const float dematerializeTime = 2f;
        dissolve.materializeTime = dematerializeTime;
        
        // Don't allow shrinking cubes to be picked up
        Drop();
        interactable = false;
        // Trick to get the cube to not interact with the player anymore but still collide with ground
        gameObject.layer = SuperspectivePhysics.VisibleButNoPlayerCollisionLayer;

        if (dissolve.stateMachine == DissolveObject.State.Materialized) {
            dissolve.Dematerialize();
            AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerDespawn, ID, transform.position);
            AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, transform.position);
            StartCoroutine(DestroyObjectAfterDissolve(dissolve, dynamicObject));
        }
    }

    private IEnumerator DestroyObjectAfterDissolve(DissolveObject dissolve, SuperspectiveDynamicReference dynamicObjRef) {
        yield return new WaitUntil(() => dissolve.stateMachine == DissolveObject.State.Dematerialized);

        if (spawnedFrom != null) {
            spawnedFrom.Reference.MatchAction(
                obj => obj.HandleSpawnedCubeBeingDestroyed(),
                save => save.HandleSpawnedCubeBeingDestroyed()
            );
        }
        dynamicObjRef?.Reference.MatchAction(
            dynamicObj => dynamicObj.Destroy(),
            save => save.Destroy()
        );
    }
    
#region Custom Collision Logic

    void ResolveCollision() {
        // TODO: Assumes a Cube shape, if not Cube shape, will need to change this
        Collider[] neighbors = Physics.OverlapBox(transform.position, transform.lossyScale / 2f);

        for (int i = 0; i < neighbors.Length; i++) {
            Collider neighbor = neighbors[i];
            if (neighbor == thisCollider ||
                neighbor.TaggedAsPlayer() ||
                Physics.GetIgnoreLayerCollision(gameObject.layer, neighbor.gameObject.layer) ||
                SuperspectivePhysics.CollisionsAreIgnored(thisCollider, neighbor)) continue;

            Vector3 neighborPosition = neighbor.transform.position;
            Quaternion neighborRotation = neighbor.transform.rotation;

            Vector3 resolveDirection;
            float resolveDistance;

            if (Physics.ComputePenetration(
                thisCollider, transform.position, transform.rotation,
                neighbor, neighborPosition, neighborRotation,
                out resolveDirection, out resolveDistance)) {
                Debug.DrawRay(transform.position, resolveDirection * resolveDistance, new Color(8f, .15f, .10f));
                transform.position += resolveDirection * resolveDistance;
                Physics.SyncTransforms();
            }
        }
    }

    public bool IsGrounded() {
        Collider[] neighborsBelow = Physics.OverlapSphere(transform.position + thisGravity.GravityDirection * 0.125f * Scale, transform.localScale.x * Scale * .5f);

        return neighborsBelow.Where(c => c.gameObject != this.gameObject && !c.TaggedAsPlayer()).ToList().Count > 0;
    }

    public void RecalculateRigidbodyKinematics() {
        SetRigidbodyIsKinematic(RigidbodyShouldBeFrozen);
    }
    
#endregion

    private void OnDrawGizmosSelected() {
        Color prevColor = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(debugTargetPos, 0.1f);
        Gizmos.color = prevColor;
    }

#region Saving

    public override void LoadSave(PickupObjectSave save) {
        AssignReferences();

        transform.position = save.position;
        transform.rotation = save.rotation;
        transform.localScale = save.localScale;

        if (thisRigidbody != null) {
            thisRigidbody.position = transform.position;
            thisRigidbody.isKinematic = save.kinematicRigidbody;
            if (!thisRigidbody.isKinematic) {
                thisRigidbody.velocity = save.velocity;
                thisRigidbody.angularVelocity = save.angularVelocity;
            }
            thisRigidbody.mass = save.mass;
        }
        
        isReplaceable = save.isReplaceable;
        if (isHeld) {
            Player.instance.heldObject = this;
        }
    }

    [Serializable]
    public class PickupObjectSave : SaveObject<PickupObject> {
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public SerializableVector3 angularVelocity;
        public SerializableVector3 localScale;
        public SerializableVector3 velocity;
        public float mass;
        public bool kinematicRigidbody;
        public bool isReplaceable;

        public PickupObjectSave(PickupObject obj) : base(obj) {
            position = obj.transform.position;
            rotation = obj.transform.rotation;
            localScale = obj.transform.localScale;

            if (obj.thisRigidbody != null) {
                velocity = obj.thisRigidbody.velocity;
                angularVelocity = obj.thisRigidbody.angularVelocity;
                mass = obj.thisRigidbody.mass;
                kinematicRigidbody = obj.thisRigidbody.isKinematic;
            }
            
            isReplaceable = obj.isReplaceable;
        }
    }
#endregion
}
