using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.PortalUtils;
using PortalMechanics;
using NaughtyAttributes;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour {
	public bool DEBUG = false;
	DebugLogger debug;
	InteractableObject interactableObject;
	InteractableGlow interactableGlow;
	public void OnLeftMouseButtonDown() {
		Pickup();
	}

	public bool isReplaceable = true;
	private bool _interactable = true;
	public bool interactable {
		get { return _interactable; }
		set {
			interactableObject.interactable = value;
			_interactable = value;
		}
	}
	public bool isHeld = false;
	public float pickupDropCooldown = 0.1f;
	float currentCooldown = 0;
	bool onCooldown { get { return currentCooldown > 0; } }
	public float holdDistance = 3;
	float minDistanceFromPlayer = 0.25f;
	float followSpeed = 15;
	float followLerpSpeed = 15;
	Transform player;
	Transform playerCam;
	public GravityObject thisGravity;
	public Rigidbody thisRigidbody;

	PortalableObject portalableObject;

	public delegate void PickupObjectSimpleAction();
	public delegate void PickupObjectAction(PickupObject obj);
	public PickupObjectSimpleAction OnPickupSimple;
	public PickupObjectSimpleAction OnDropSimple;
	public PickupObjectAction OnPickup;
	public PickupObjectAction OnDrop;
	public static PickupObjectSimpleAction OnAnyPickupSimple;
	public static PickupObjectSimpleAction OnAnyDropSimple;
	public static PickupObjectAction OnAnyPickup;
	public static PickupObjectAction OnAnyDrop;

	Vector3 playerPosLastFrame;
	public SoundSettings pickupSound, dropSound;

	[Button("Test PickupSound")]
	void PlayPickupSound() {
		pickupSound.gameObjectAttachedTo = gameObject;
		SoundManager.instance.Play("PickupObject", pickupSound, true);
	}
	[Button("Test DropSound")]
	void PlayDropSound() {
		dropSound.gameObjectAttachedTo = gameObject;
		SoundManager.instance.Play("DropObject", dropSound, true);
	}

	void Awake() {
		debug = new DebugLogger(gameObject, () => DEBUG);
		thisRigidbody = GetComponent<Rigidbody>();
		thisGravity = GetComponent<GravityObject>();

		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

		interactableGlow = GetComponent<InteractableGlow>();
		if (interactableGlow == null) {
			interactableGlow = gameObject.AddComponent<InteractableGlow>();
		}

		pickupSound.gameObjectAttachedTo = dropSound.gameObjectAttachedTo = gameObject;
	}

	void Start() {
		player = Player.instance.transform;
		playerCam = EpitaphScreen.instance.playerCamera.transform;

		PlayerButtonInput.instance.OnAction1Press += Drop;

		PillarDimensionObject thisDimensionObject = Utils.FindDimensionObjectRecursively(transform);
		if (thisDimensionObject != null) {
			thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;
		}

		portalableObject = GetComponent<PortalableObject>();
		playerPosLastFrame = Player.instance.transform.position;

		Portal.OnAnyPortalTeleport += UpdatePlayerPositionLastFrameAfterPortal;
		TeleportEnter.OnAnyTeleportSimple += UpdatePlayerPositionLastFrameAfterTeleport;
	}

	private void Update() {
		if (currentCooldown > 0) {
			currentCooldown -= Time.deltaTime;
		}

		if (isHeld) {
			interactableGlow.TurnOnGlow();
		}
	}

	void UpdatePlayerPositionLastFrameAfterPortal(Portal inPortal, Collider objPortaled) {
		if (objPortaled.gameObject == Player.instance.gameObject) {
			playerPosLastFrame = inPortal.TransformPoint(playerPosLastFrame);
		}
	}

	void UpdatePlayerPositionLastFrameAfterTeleport() {
		// TODO:
	}

	void FixedUpdate() {
		if (isHeld) {
			RaycastHits raycastHits;
			Vector3 targetPos = (portalableObject == null) ? TargetHoldPosition(out raycastHits) : TargetHoldPositionThroughPortal(out raycastHits);

			Vector3 playerPositionalDiff = Player.instance.transform.position - playerPosLastFrame;
			if (portalableObject != null && portalableObject.grabbedThroughPortal != null) {
				playerPositionalDiff = portalableObject.grabbedThroughPortal.TransformDirection(playerPositionalDiff);
			}
			debug.Log($"Positional diff: {playerPositionalDiff:F3}");
			thisRigidbody.MovePosition(transform.position + playerPositionalDiff);

			Vector3 diff = targetPos - thisRigidbody.position;
			Vector3 newVelocity = Vector3.Lerp(thisRigidbody.velocity, followSpeed * diff, followLerpSpeed * Time.fixedDeltaTime);
			bool movingTowardsPlayer = Vector3.Dot(newVelocity.normalized, -raycastHits.lastRaycast.ray.direction) > 0.5f;
			if (raycastHits.totalDistance < minDistanceFromPlayer && movingTowardsPlayer) {
				newVelocity = Vector3.ProjectOnPlane(newVelocity, raycastHits.lastRaycast.ray.direction);
			}

			Vector3 velBefore = thisRigidbody.velocity;
			thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
			//debug.Log("Before: " + velBefore.ToString("F3") + "\nAfter: " + thisRigidbody.velocity.ToString("F3"));
		}

		playerPosLastFrame = Player.instance.transform.position;
	}

	Vector3 TargetHoldPosition(out RaycastHits raycastHits) {
		// TODO: Don't work with strings every frame, clean this up
		int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
		int layerMask = ~(1 << ignoreRaycastLayer | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible") | 1 << LayerMask.NameToLayer("CollideWithPlayerOnly"));
		int tempLayer = gameObject.layer;
		gameObject.layer = ignoreRaycastLayer;

		raycastHits = PortalUtils.RaycastThroughPortals(playerCam.position, playerCam.forward, holdDistance, layerMask);
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
			tempLayerPortalCopy = portalableObject.fakeCopyInstance.gameObject.layer;
			portalableObject.fakeCopyInstance.gameObject.layer = ignoreRaycastLayer;
		}

		raycastHits = PortalUtils.RaycastThroughPortals(playerCam.position, playerCam.forward, holdDistance, layerMask);
		Vector3 targetPos = raycastHits.finalPosition;

		gameObject.layer = tempLayer;
		if (portalableObject.copyIsEnabled) {
			portalableObject.fakeCopyInstance.gameObject.layer = tempLayerPortalCopy;
		}

		bool throughOutPortalToInPortal = portalableObject.grabbedThroughPortal != null && portalableObject.grabbedThroughPortal.portalIsEnabled && !raycastHits.raycastHitAnyPortal;
		bool throughInPortalToOutPortal = portalableObject.grabbedThroughPortal == null && raycastHits.raycastHitAnyPortal;
		if (throughOutPortalToInPortal || throughInPortalToOutPortal) {
			Portal inPortal = throughOutPortalToInPortal ? portalableObject.grabbedThroughPortal : raycastHits.firstRaycast.portalHit.otherPortal;

			targetPos = inPortal.TransformPoint(targetPos);
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

			thisGravity.useGravity = false;
			thisRigidbody.isKinematic = false;
			isHeld = true;
			currentCooldown = pickupDropCooldown;

			SoundManager.instance.Play("PickupObject", pickupSound, true);

			OnPickupSimple?.Invoke();
			OnPickup?.Invoke(this);
			OnAnyPickupSimple?.Invoke();
			OnAnyPickup?.Invoke(this);
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
			thisRigidbody.velocity += PlayerMovement.instance.thisRigidbody.velocity;
			isHeld = false;
			currentCooldown = pickupDropCooldown;

			SoundManager.instance.Play("DropObject", dropSound, true);

			OnDropSimple?.Invoke();
			OnDrop?.Invoke(this);
			OnAnyDropSimple?.Invoke();
			OnAnyDrop?.Invoke(this);
		}
	}


}
