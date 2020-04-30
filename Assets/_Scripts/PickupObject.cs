using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.PortalUtils;
using PortalMechanics;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour {

	InteractableObject interactableObject;
	InteractableGlow interactableGlow;
	public void OnLeftMouseButtonDown() {
		Pickup();
	}

	public bool interactable = true;
	public bool isHeld = false;
	public float pickupDropCooldown = 0.1f;
	float currentCooldown = 0;
	bool onCooldown { get { return currentCooldown > 0; } }
	public float holdDistance = 3;
	float minDistanceFromPlayer = 0.25f;
	float followSpeed = 15;
	float followLerpSpeed = 25;
	Vector3 positionLastPhysicsFrame;
	Transform player;
	Transform playerCam;
	Transform originalParent;
	public GravityObject thisGravity;
	public Rigidbody thisRigidbody;
	Renderer thisRenderer;

	PortalableObject portalableObject;

	public delegate void PickupObjectSimpleAction();
	public delegate void PickupObjectAction(PickupObject obj);
	public PickupObjectSimpleAction OnPickupSimple;
	public PickupObjectSimpleAction OnDropSimple;
	public PickupObjectAction OnPickup;
	public PickupObjectAction OnDrop;

	void Awake() {
		thisRigidbody = GetComponent<Rigidbody>();
		thisGravity = GetComponent<GravityObject>();
		originalParent = transform.parent;

		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

		interactableGlow = GetComponent<InteractableGlow>();
		if (interactableGlow == null) {
			interactableGlow = gameObject.AddComponent<InteractableGlow>();
		}
	}

	void Start() {
		player = Player.instance.transform;
		playerCam = EpitaphScreen.instance.playerCamera.transform;
		positionLastPhysicsFrame = transform.position;
		thisRenderer = GetComponent<Renderer>();

		PlayerButtonInput.instance.OnAction1Press += Drop;

		PillarDimensionObject thisDimensionObject = Utils.FindDimensionObjectRecursively(transform);
		if (thisDimensionObject != null) {
			thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;
		}

		portalableObject = GetComponent<PortalableObject>();
	}

	private void Update() {
		if (currentCooldown > 0) {
			currentCooldown -= Time.deltaTime;
		}

		if (isHeld) {
			interactableGlow.TurnOnGlow();
		}
	}

	void FixedUpdate() {
		if (isHeld) {
			RaycastHits raycastHits;
			Vector3 targetPos = (portalableObject == null) ? TargetHoldPosition(out raycastHits) : TargetHoldPositionThroughPortal(out raycastHits);

			Vector3 diff = targetPos - thisRigidbody.position;
			Vector3 newVelocity = Vector3.Lerp(thisRigidbody.velocity, followSpeed * diff, followLerpSpeed * Time.fixedDeltaTime);
			bool movingTowardsPlayer = Vector3.Dot(newVelocity.normalized, -raycastHits.lastRaycast.ray.direction) > 0.5f;
			if (raycastHits.totalDistance < minDistanceFromPlayer && movingTowardsPlayer) {
				newVelocity = Vector3.ProjectOnPlane(newVelocity, raycastHits.lastRaycast.ray.direction);
			}
			thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
		}
	}

	Vector3 TargetHoldPosition(out RaycastHits raycastHits) {
		// TODO: Don't work with strings every frame, clean this up
		int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
		int layerMask = ~(1 << ignoreRaycastLayer | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible") | 1 << LayerMask.NameToLayer("CollideWithPlayerOnly"));
		int tempLayer = gameObject.layer;
		gameObject.layer = ignoreRaycastLayer;

		raycastHits = PortalUtils.RaycastThroughPortals(player.position, playerCam.forward, holdDistance, layerMask);
		Vector3 targetPos = raycastHits.finalPosition;
		gameObject.layer = tempLayer;

		return targetPos;
	}

	Vector3 TargetHoldPositionThroughPortal(out RaycastHits raycastHits) {
		// TODO: Don't work with strings every frame, clean this up
		int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
		int layerMask = ~(1 << ignoreRaycastLayer | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible") | 1 << LayerMask.NameToLayer("CollideWithPlayerOnly"));
		int tempLayer = gameObject.layer;
		gameObject.layer = ignoreRaycastLayer;

		int tempLayerPortalCopy = 0;
		if (portalableObject.copyIsEnabled) {
			tempLayerPortalCopy = portalableObject.fakeCopyInstance.layer;
			portalableObject.fakeCopyInstance.layer = ignoreRaycastLayer;
		}

		raycastHits = PortalUtils.RaycastThroughPortals(player.position, playerCam.forward, holdDistance, layerMask);
		Vector3 targetPos = raycastHits.finalPosition;

		gameObject.layer = tempLayer;
		if (portalableObject.copyIsEnabled) {
			portalableObject.fakeCopyInstance.layer = tempLayerPortalCopy;
		}

		bool throughOutPortalToInPortal = portalableObject.grabbedThroughPortal != null && portalableObject.grabbedThroughPortal.portalIsEnabled && !raycastHits.raycastHitAnyPortal;
		bool throughInPortalToOutPortal = portalableObject.grabbedThroughPortal == null && raycastHits.raycastHitAnyPortal;
		if (throughOutPortalToInPortal || throughInPortalToOutPortal) {
			Portal inPortal = throughOutPortalToInPortal ? portalableObject.grabbedThroughPortal : raycastHits.firstRaycast.portalHit.otherPortal;

			Vector3 localTargetPos = inPortal.transform.InverseTransformPoint(targetPos);
			localTargetPos = Quaternion.Euler(0, 180f, 0) * localTargetPos;
			targetPos = inPortal.otherPortal.transform.TransformPoint(localTargetPos);
		}

		return targetPos;
	}

	void HandleDimensionObjectStateChange(VisibilityState nextState) {
		if (nextState == VisibilityState.invisible && isHeld) {
			Drop();
		}
	}

	public void Pickup() {
		if (!isHeld && !onCooldown && interactable) {

			if (transform.parent != null) {
				transform.SetParent(null);
			}
			DontDestroyOnLoad(gameObject);

			//transform.parent = Player.instance.transform;
			positionLastPhysicsFrame = transform.position;
			thisGravity.useGravity = false;
			thisRigidbody.isKinematic = false;
			isHeld = true;
			currentCooldown = pickupDropCooldown;

			OnPickupSimple?.Invoke();
			OnPickup?.Invoke(this);
		}
	}

	public void Drop() {
		if (isHeld && !onCooldown && interactable) {
			thisGravity.gravityDirection = Physics.gravity.normalized;
			if (portalableObject?.grabbedThroughPortal != null) {
				thisGravity.ReorientGravityAfterPortaling(portalableObject.grabbedThroughPortal);
			}

			//transform.parent = originalParent;
			thisGravity.useGravity = true;
			//thisRigidbody.isKinematic = false;
			isHeld = false;
			currentCooldown = pickupDropCooldown;

			OnDropSimple?.Invoke();
			OnDrop?.Invoke(this);
		}
	}
}
