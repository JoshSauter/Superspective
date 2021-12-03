﻿using System;
using System.Collections;
using System.Linq;
using Audio;
using SuperspectiveUtils;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;
using static Audio.AudioManager;

[RequireComponent(typeof(UniqueId))]
public class PickupObject : SaveableObject<PickupObject, PickupObject.PickupObjectSave>, AudioJobOnGameObject {
    public delegate void PickupObjectAction(PickupObject obj);

    public delegate void PickupObjectSimpleAction();

    public const float pickupDropCooldown = 0.1f;
    public const float holdDistance = 3;
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
    public bool shouldFollow = true;
    public GravityObject thisGravity;
    public Rigidbody thisRigidbody;

    public PortalableObject portalableObject;
    bool _interactable = true;
    float currentCooldown;
    InteractableGlow interactableGlow;
    InteractableObject interactableObject;
    public PickupObjectAction OnDrop;
    public PickupObjectSimpleAction OnDropSimple;
    public PickupObjectAction OnPickup;
    public PickupObjectSimpleAction OnPickupSimple;
    AudioJob pickupSound;
    Transform player;
    Transform playerCam;

    Vector3 playerCamPosLastFrame;

    readonly float rotateToRightAngleTime = 0.35f;

    public Transform GetObjectToPlayAudioOn(AudioJob _) => transform;
    
    public bool interactable {
        get => _interactable;
        set {
            if (value) {
                interactableObject.SetAsInteractable();
            }
            else {
                interactableObject.SetAsHidden();
            }
            _interactable = value;
        }
    }

    bool onCooldown => currentCooldown > 0;

    protected override void Awake() {
        base.Awake();
        AssignReferences();
    }

    void AssignReferences() {
        if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
        if (thisGravity == null) thisGravity = GetComponent<GravityObject>();

        interactableObject = thisRigidbody.GetOrAddComponent<InteractableObject>();
        interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

        interactableGlow = interactableObject.GetOrAddComponent<InteractableGlow>();

        if (portalableObject == null) portalableObject = GetComponent<PortalableObject>();
    }

    protected override void Start() {
        base.Start();
        player = Player.instance.transform;
        playerCam = SuperspectiveScreen.instance.playerCamera.transform;

        PlayerButtonInput.instance.OnAction1Press += Drop;

        PillarDimensionObject thisDimensionObject = Utils.FindDimensionObjectRecursively<PillarDimensionObject>(transform);
        if (thisDimensionObject != null) thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;

        playerCamPosLastFrame = playerCam.transform.position;

        Portal.OnAnyPortalTeleport += UpdatePlayerPositionLastFrameAfterPortal;
        TeleportEnter.OnAnyTeleportSimple += UpdatePlayerPositionLastFrameAfterTeleport;

        pickupSound = AudioManager.instance.GetOrCreateJob(AudioName.CubePickup, ID);
    }

    void Update() {
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        // Don't allow clicks in the menu to propagate to picking up/dropping the cube
        if (MainCanvas.instance.tempMenu.menuIsOpen) currentCooldown = pickupDropCooldown;

        if (isHeld) {
            interactableGlow.TurnOnGlow();

            if (PlayerButtonInput.instance.Action3Pressed)
                StartCoroutine(RotateToRightAngle(RightAngleRotations.GetNearest(transform.rotation)));
        }
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

            float holdDistanceToUse = holdDistance +
                                      0.5f * Mathf.Abs(Vector3.Dot(Player.instance.transform.up, playerCam.forward));
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
        // TODO: Don't work with strings every frame, clean this up
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        int layerMask = ~((1 << ignoreRaycastLayer) | (1 << LayerMask.NameToLayer("Player")) |
                          (1 << LayerMask.NameToLayer("Invisible")) |
                          (1 << LayerMask.NameToLayer("CollideWithPlayerOnly")));
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
        // TODO: Don't work with strings every frame, clean this up
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        int layerMask = ~((1 << ignoreRaycastLayer) | (1 << LayerMask.NameToLayer("Player")) |
                          (1 << LayerMask.NameToLayer("Invisible")) |
                          (1 << LayerMask.NameToLayer("CollideWithPlayerOnly")));
        int tempLayer = gameObject.layer;
        gameObject.layer = ignoreRaycastLayer;

        int tempLayerPortalCopy = 0;
        if (portalableObject.copyIsEnabled) {
            tempLayerPortalCopy = portalableObject.fakeCopyInstance.gameObject.layer;
            portalableObject.fakeCopyInstance.gameObject.layer = ignoreRaycastLayer;
        }

        raycastHits = RaycastUtils.Raycast(playerCam.position, playerCam.forward, holdDistance, layerMask);
        Vector3 targetPos = PositionAtFirstObjectOrEndOfRaycast(raycastHits);

        gameObject.layer = tempLayer;
        if (portalableObject.copyIsEnabled) portalableObject.fakeCopyInstance.gameObject.layer = tempLayerPortalCopy;

        bool throughOutPortalToInPortal = portalableObject.grabbedThroughPortal != null &&
                                          portalableObject.grabbedThroughPortal.portalIsEnabled &&
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
            //thisRigidbody.isKinematic = false;
            thisRigidbody.velocity += PlayerMovement.instance.thisRigidbody.velocity;
            isHeld = false;
            currentCooldown = pickupDropCooldown;

            AudioManager.instance.PlayOnGameObject(AudioName.CubeDrop, ID, this, true);

            OnDropSimple?.Invoke();
            OnDrop?.Invoke(this);
            OnAnyDropSimple?.Invoke();
            OnAnyDrop?.Invoke(this);
        }
    }

#region Saving

    [Serializable]
    public class PickupObjectSave : SerializableSaveObject<PickupObject> {
        SerializableVector3 angularVelocity;
        float currentCooldown;
        bool interactable;
        bool isHeld;

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
            obj.currentCooldown = currentCooldown;
        }
    }
#endregion
}