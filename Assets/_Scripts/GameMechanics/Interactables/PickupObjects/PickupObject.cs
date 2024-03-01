using System;
using System.Collections;
using System.Linq;
using Audio;
using DissolveObjects;
using GrowShrink;
using SuperspectiveUtils;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;
using static Audio.AudioManager;
using CubeSpawnerReference = SerializableClasses.SerializableReference<CubeSpawner, CubeSpawner.CubeSpawnerSave>;

[RequireComponent(typeof(UniqueId))]
public class PickupObject : SaveableObject<PickupObject, PickupObject.PickupObjectSave>, AudioJobOnGameObject {
    public delegate void PickupObjectAction(PickupObject obj);

    public delegate void PickupObjectSimpleAction();

    public const float pickupDropCooldown = 0.2f;
    public const float holdDistance = 1.5f;
    const float minDistanceFromPlayer = 0.25f;
    const float followSpeed = 15;
    const float followLerpSpeed = 15;
    public static PickupObjectSimpleAction OnAnyPickupSimple;
    public static PickupObjectSimpleAction OnAnyDropSimple;
    public static PickupObjectAction OnAnyPickup;
    public static PickupObjectAction OnAnyDrop;

    static float currentPitch = 1f;

    public bool isReplaceable = true;
    public bool isHeld;
    public CubeReceptacle receptacleHeldIn;
    private bool IsHeldInReceptacle => receptacleHeldIn != null;
    public bool shouldFollow = true;
    public GravityObject thisGravity;
    public bool freezeRigidbodyDueToNearbyPlayer = false;

    private bool RigidbodyShouldBeFrozen => GameManager.instance.IsCurrentlyLoading || freezeRigidbodyDueToNearbyPlayer || IsHeldInReceptacle;
    public Rigidbody thisRigidbody;
    public Collider thisCollider;

    public PortalableObject portalableObject;
    public GrowShrinkObject growShrinkObject;
    public CubeSpawnerReference spawnedFrom;

    public float scale => Mathf.Min(Player.instance.Scale, (growShrinkObject != null ? growShrinkObject.currentScale : 1f));
    const float scaleMultiplier = 1.5f;
    float currentCooldown;
    InteractableGlow interactableGlow;
    InteractableObject interactableObject;
    public PickupObjectAction OnDrop;
    public PickupObjectSimpleAction OnDropSimple;
    public PickupObjectAction OnPickup;
    public PickupObjectSimpleAction OnPickupSimple;
    AudioJob pickupSound => AudioManager.instance.GetOrCreateJob(AudioName.CubePickup, ID);
    Transform player;
    Transform playerCam;

    Vector3 playerCamPosLastFrame;

    readonly float rotateToRightAngleTime = 0.35f;

    public Transform GetObjectToPlayAudioOn(AudioJob _) => transform;

    public bool interactable = true;

    bool onCooldown => currentCooldown > 0;

    protected override void Awake() {
        base.Awake();
        AssignReferences();
    }

    void AssignReferences() {
        if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
        if (thisGravity == null) thisGravity = GetComponent<GravityObject>();
        if (thisCollider == null) thisCollider = GetComponent<Collider>();

        interactableObject = thisRigidbody.GetOrAddComponent<InteractableObject>();
        interactableObject.SkipSave = true;
        interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

        interactableGlow = interactableObject.GetOrAddComponent<InteractableGlow>();

        if (portalableObject == null) portalableObject = GetComponent<PortalableObject>();
        if (growShrinkObject == null) growShrinkObject = GetComponent<GrowShrinkObject>();
    }

    protected override void Start() {
        base.Start();
        player = Player.instance.transform;
        playerCam = SuperspectiveScreen.instance.playerCamera.transform;

        PlayerButtonInput.instance.OnInteractPress += Drop;

        PillarDimensionObject thisDimensionObject = Utils.FindDimensionObjectRecursively<PillarDimensionObject>(transform);
        if (thisDimensionObject != null) thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;

        playerCamPosLastFrame = playerCam.transform.position;

        Portal.OnAnyPortalTeleport += UpdatePlayerPositionLastFrameAfterPortal;
        TeleportEnter.OnAnyTeleportSimple += UpdatePlayerPositionLastFrameAfterTeleport;

        AudioManager.instance.GetOrCreateJob(AudioName.CubePickup, ID);
        
        // TODO: Remove this; temp for testing rolling sphere
        thisRigidbody.maxAngularVelocity = 120;
    }

    void Update() {
        RecalculateRigidbodyKinematics();
        
        if (interactable) {
            if (scale > 6f * Player.instance.Scale) {
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
        if (NovaPauseMenu.instance.PauseMenuIsOpen) currentCooldown = pickupDropCooldown;

        if (isHeld) {
            interactableGlow.TurnOnGlow();

            if (PlayerButtonInput.instance.AlignObjectPressed)
                StartCoroutine(RotateToRightAngle(RightAngleRotations.GetNearest(transform.rotation)));
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

    void FixedUpdate() {
        if (isHeld && shouldFollow) {
            Vector3 playerCamPositionalDiff = Player.instance.cameraFollow.transform.position - playerCamPosLastFrame;
            if (portalableObject != null && portalableObject.grabbedThroughPortal != null)
                playerCamPositionalDiff =
                    portalableObject.grabbedThroughPortal.TransformDirection(playerCamPositionalDiff);
            //debug.Log($"Positional diff: {playerCamPositionalDiff:F3}");
            thisRigidbody.MovePosition(transform.position + playerCamPositionalDiff);
            if (portalableObject != null && portalableObject.copyIsEnabled && portalableObject.copyShouldBeEnabled)
                portalableObject.fakeCopyInstance.TransformCopy();

            float pickupObjRadiusSpacer = GetObjectWidthTowardsCamera();
            //float holdDistanceToUse = pickupObjRadiusSpacer + holdDistance + playerRadiusSpacer;
            float holdDistanceToUse = pickupObjRadiusSpacer + holdDistance * Mathf.Max(scale, Mathf.Pow(scale, scaleMultiplier));
            debug.Log($"Final hold distance: {holdDistanceToUse}\nPickupObjRadiusSpacer: {pickupObjRadiusSpacer}\nScaled hold distance: {holdDistance * Mathf.Pow(scale, scaleMultiplier)}");
            Vector3 targetPos = portalableObject == null
                ? TargetHoldPosition(holdDistanceToUse, out SuperspectiveRaycast raycastHits)
                : TargetHoldPositionThroughPortal(holdDistanceToUse, out raycastHits);

            Vector3 diff = targetPos - thisRigidbody.position;
            Vector3 newVelocity = Vector3.Lerp(
                thisRigidbody.velocity,
                followSpeed * diff,
                followLerpSpeed * Time.fixedDeltaTime
            );
            bool movingTowardsPlayer =
                Vector3.Dot(newVelocity.normalized, -raycastHits.raycastParts.Last().ray.direction) > 0.5f;
            if (raycastHits.distance < minDistanceFromPlayer && movingTowardsPlayer)
                newVelocity = Vector3.ProjectOnPlane(newVelocity, raycastHits.raycastParts.Last().ray.direction);

            //Vector3 velBefore = thisRigidbody.velocity;
            thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
            //debug.Log("Before: " + velBefore.ToString("F3") + "\nAfter: " + thisRigidbody.velocity.ToString("F3"));
        }

        ResolveCollision();

        playerCamPosLastFrame = playerCam.transform.position;
    }

    public void OnLeftMouseButtonDown() {
        Pickup();
    }

    void UpdatePlayerPositionLastFrameAfterPortal(Portal inPortal, Collider objPortaled) {
        if (objPortaled.gameObject == Player.instance.gameObject)
            playerCamPosLastFrame = inPortal.TransformPoint(playerCamPosLastFrame);
    }

    void UpdatePlayerPositionLastFrameAfterTeleport() {
        // TODO:
    }

    IEnumerator RotateToRightAngle(Quaternion destinationRotation) {
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
    }

    Vector3 TargetHoldPosition(float holdDistance, out SuperspectiveRaycast raycastHits) {
        int ignoreRaycastLayer = SuperspectivePhysics.IgnoreRaycastLayer;
        int layerMask = SuperspectivePhysics.PhysicsRaycastLayerMask;
        int tempLayer = gameObject.layer;
        gameObject.layer = ignoreRaycastLayer;

        raycastHits = RaycastUtils.Raycast(playerCam.position, playerCam.forward, holdDistance, layerMask);
        
        Vector3 targetPos = PositionAtFirstObjectOrEndOfRaycast(raycastHits);
        gameObject.layer = tempLayer;

        return targetPos;
    }
    
    Vector3 PositionAtFirstObjectOrEndOfRaycast(SuperspectiveRaycast raycast) {
        if (raycast.hitObject) {
            // Assumes a transform scale of 1,1,1 corresponds to 1 unit^3 volume
            return raycast.firstObjectHit.point - transform.lossyScale.x * raycast.firstObjectHit.normal;
        }
        else {
            SuperspectiveRaycastPart lastPart = raycast.raycastParts.Last();
            return lastPart.ray.origin + lastPart.ray.direction * lastPart.distance;
        }
    }

    Vector3 TargetHoldPositionThroughPortal(float holdDistance, out SuperspectiveRaycast raycastHits) {
        int layerMask = ~((1 << SuperspectivePhysics.IgnoreRaycastLayer) | (1 << LayerMask.NameToLayer("Player")) |
                          (1 << SuperspectivePhysics.InvisibleLayer) |
                          (1 << SuperspectivePhysics.CollideWithPlayerOnlyLayer));
        int tempLayer = gameObject.layer;
        gameObject.layer = SuperspectivePhysics.IgnoreRaycastLayer;

        int tempLayerPortalCopy = 0;
        if (portalableObject.copyIsEnabled) {
            tempLayerPortalCopy = portalableObject.fakeCopyInstance.gameObject.layer;
            portalableObject.fakeCopyInstance.gameObject.layer = SuperspectivePhysics.IgnoreRaycastLayer;
        }

        raycastHits = RaycastUtils.Raycast(playerCam.position, playerCam.forward, holdDistance, layerMask);
        Vector3 targetPos = PositionAtFirstObjectOrEndOfRaycast(raycastHits);

        gameObject.layer = tempLayer;
        if (portalableObject.copyIsEnabled) portalableObject.fakeCopyInstance.gameObject.layer = tempLayerPortalCopy;

        bool throughOutPortalToInPortal = portalableObject.grabbedThroughPortal != null &&
                                          portalableObject.grabbedThroughPortal.PortalRenderingIsEnabled &&
                                          !raycastHits.hitPortal;
        bool throughInPortalToOutPortal =
            portalableObject.grabbedThroughPortal == null && raycastHits.hitPortal;
        if (throughOutPortalToInPortal || throughInPortalToOutPortal) {
            Portal inPortal = throughOutPortalToInPortal
                ? portalableObject.grabbedThroughPortal
                : raycastHits.firstValidPortalHit.otherPortal;

            targetPos = inPortal.TransformPoint(targetPos);
        }

        return targetPos;
    }

    void HandleDimensionObjectStateChange(DimensionObject dimObj) {
        if (dimObj.visibilityState == VisibilityState.invisible && isHeld) Drop();
    }

    public void Pickup() {
        if (!isHeld && !onCooldown && interactable) {
            thisGravity.useGravity = false;
            thisRigidbody.isKinematic = false;
            isHeld = true;
            currentCooldown = pickupDropCooldown;

            // Pitch goes 1 -> 1.25 -> 1.5 -> 1
            currentPitch = (currentPitch - .75f) % .75f + 1f;
            pickupSound.basePitch = currentPitch;
            AudioManager.instance.PlayOnGameObject(AudioName.CubePickup, ID, this, true);
            
            SuperspectivePhysics.IgnoreCollision(thisCollider, Player.instance.collider);

            OnPickupSimple?.Invoke();
            OnPickup?.Invoke(this);
            OnAnyPickupSimple?.Invoke();
            OnAnyPickup?.Invoke(this);
        }
    }

    public void Drop() {
        if (isHeld && !onCooldown && interactable) {
            thisGravity.gravityDirection = Physics.gravity.normalized;
            if (portalableObject?.grabbedThroughPortal != null)
                thisGravity.ReorientGravityAfterPortaling(portalableObject.grabbedThroughPortal);

            //transform.parent = originalParent;
            thisGravity.useGravity = true;
            thisRigidbody.velocity += PlayerMovement.instance.thisRigidbody.velocity;
            isHeld = false;
            currentCooldown = pickupDropCooldown;

            AudioManager.instance.PlayOnGameObject(AudioName.CubeDrop, ID, this, true);
            
            SuperspectivePhysics.RestoreCollision(thisCollider, Player.instance.collider);

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
        gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");

        if (dissolve.state == DissolveObject.State.Materialized) {
            dissolve.Dematerialize();
            AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerDespawn, ID, transform.position);
            AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, transform.position);
            StartCoroutine(DestroyObjectAfterDissolve(dissolve, dynamicObject));
        }
    }

    private IEnumerator DestroyObjectAfterDissolve(DissolveObject dissolve, SerializableDynamicReference dynamicObjRef) {
        yield return new WaitUntil(() => dissolve.state == DissolveObject.State.Dematerialized);

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
        Collider[] neighbors = Physics.OverlapSphere(transform.position, transform.localScale.x * scale);

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
        Collider[] neighborsBelow = Physics.OverlapSphere(transform.position + thisGravity.gravityDirection * 0.125f * scale, transform.localScale.x * scale * .5f);

        return neighborsBelow.Where(c => c.gameObject != this.gameObject && !c.TaggedAsPlayer()).ToList().Count > 0;
    }

    public void RecalculateRigidbodyKinematics() {
        if (RigidbodyShouldBeFrozen) {
            thisRigidbody.isKinematic = true;
        }
        else {
            thisRigidbody.isKinematic = false;
        }
    }
    
#endregion

#region Saving

    [Serializable]
    public class PickupObjectSave : SerializableSaveObject<PickupObject> {
        SerializableVector3 angularVelocity;
        float currentCooldown;
        bool interactable;
        bool isHeld;

        SerializableReference<CubeReceptacle, CubeReceptacle.CubeReceptacleSave> receptacleHeldIn;

        public bool isReplaceable;
        SerializableVector3 localScale;
        float mass;

        SerializableVector3 playerCamPosLastFrame;
        SerializableVector3 position;
        SerializableQuaternion rotation;

        bool kinematicRigidbody;
        SerializableVector3 velocity;

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
            interactable = obj.interactable;
            isHeld = obj.isHeld;
            receptacleHeldIn = obj.receptacleHeldIn;
            currentCooldown = obj.currentCooldown;
        }

        public override void LoadSave(PickupObject obj) {
            obj.AssignReferences();

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = localScale;

            if (obj.thisRigidbody != null) {
                obj.thisRigidbody.velocity = velocity;
                obj.thisRigidbody.angularVelocity = angularVelocity;
                obj.thisRigidbody.mass = mass;
                obj.thisRigidbody.isKinematic = kinematicRigidbody;
            }

            obj.isReplaceable = isReplaceable;
            obj.interactable = interactable;
            obj.isHeld = isHeld;
            obj.receptacleHeldIn = receptacleHeldIn.GetOrNull();
            obj.currentCooldown = currentCooldown;
        }
    }
#endregion
}