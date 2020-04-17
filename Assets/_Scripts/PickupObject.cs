using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using EpitaphUtils.PortalUtils;

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

	public Portal hoveredThroughPortal;
	public Portal grabbedThroughPortal;

	void Awake() {
		thisRigidbody = GetComponent<Rigidbody>();
		thisGravity = GetComponent<GravityObject>();
		originalParent = transform.parent;

		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
		interactableObject.OnMouseHoverEnter += () => hoveredThroughPortal = DetermineHoveredThroughPortal();
		interactableObject.OnMouseHoverExit += () => hoveredThroughPortal = null;

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
		portalableObject = GetComponent<PortalableObject>();
		if (portalableObject != null) {
			portalableObject.BeforeObjectTeleported += (Portal inPortal) => ToggleGrabbedThroughPortalIfRelevant(inPortal, false);
		}
		Portal.BeforeAnyPortalTeleport += (Portal inPortal, Collider objBeingTeleported) => ToggleGrabbedThroughPortalIfRelevant(inPortal, true);
		PlayerButtonInput.instance.OnAction1Press += Drop;

		PillarDimensionObject thisDimensionObject = Utils.FindDimensionObjectRecursively(transform);
		if (thisDimensionObject != null) {
			thisDimensionObject.OnStateChange += HandleDimensionObjectStateChange;
		}
	}

	void ToggleGrabbedThroughPortalIfRelevant(Portal inPortal, bool playerTeleported) {
		if (isHeld) {
			Portal compareTo = playerTeleported ? inPortal : inPortal.otherPortal;
			if (grabbedThroughPortal == compareTo) {
				grabbedThroughPortal = null;
			}
			else {
				grabbedThroughPortal = playerTeleported ? inPortal.otherPortal : inPortal;
			}
		}
	}

	private void Update() {
		if (currentCooldown > 0) {
			currentCooldown -= Time.deltaTime;
		}

		if (hoveredThroughPortal != null || grabbedThroughPortal != null) {
			Portal portal = grabbedThroughPortal ?? hoveredThroughPortal;
			portalableObject.EnableAndUpdatePortalCopy(portal.otherPortal);
			portalableObject.fakeCopyInstance.GetComponent<InteractableGlow>().TurnOnGlow();
		}
		else if (portalableObject != null && portalableObject.fakeCopyInstance != null) {
			portalableObject.fakeCopyInstance?.GetComponent<InteractableGlow>().TurnOffGlow();
			portalableObject.DisablePortalCopy();
		}

		if (isHeld) {
			interactableGlow.TurnOnGlow();

			if (hoveredThroughPortal == null && grabbedThroughPortal != null) {
				Drop();
			}
		}
	}

	void FixedUpdate() {
		if (isHeld) {
			int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
			int layerMask = ~(1 << ignoreRaycastLayer | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible"));
			int tempLayer = gameObject.layer;
			gameObject.layer = ignoreRaycastLayer;

			int tempLayerPortalCopy = 0;
			if (portalableObject?.fakeCopyInstance != null) {
				tempLayerPortalCopy = portalableObject.fakeCopyInstance.layer;
				portalableObject.fakeCopyInstance.layer = ignoreRaycastLayer;
			}

			RaycastHits raycastHits = PortalUtils.RaycastThroughPortals(player.position, playerCam.forward, holdDistance, layerMask);
			Vector3 targetPos = raycastHits.finalPosition;

			bool throughOutPortalToInPortal = grabbedThroughPortal != null && grabbedThroughPortal.isEnabled && !raycastHits.raycastHitAnyPortal;
			bool throughInPortalToOutPortal = grabbedThroughPortal == null && raycastHits.raycastHitAnyPortal;
			if (throughOutPortalToInPortal || throughInPortalToOutPortal) {
				Portal inPortal = throughOutPortalToInPortal ? grabbedThroughPortal : raycastHits.firstRaycast.portalHit.otherPortal;

				Vector3 localTargetPos = inPortal.transform.InverseTransformPoint(targetPos);
				localTargetPos = Quaternion.Euler(0, 180f, 0) * localTargetPos;
				targetPos = inPortal.otherPortal.transform.TransformPoint(localTargetPos);
			}

			gameObject.layer = tempLayer;
			if (portalableObject?.fakeCopyInstance != null) {
				portalableObject.fakeCopyInstance.layer = tempLayerPortalCopy;
			}
			Vector3 diff = targetPos - thisRigidbody.position;
			Vector3 newVelocity = Vector3.Lerp(thisRigidbody.velocity, followSpeed * diff, followLerpSpeed * Time.fixedDeltaTime);
			bool movingTowardsPlayer = Vector3.Dot(newVelocity.normalized, -raycastHits.lastRaycast.ray.direction) > 0.5f;
			if (raycastHits.totalDistance < minDistanceFromPlayer && movingTowardsPlayer) {
				newVelocity = Vector3.ProjectOnPlane(newVelocity, raycastHits.lastRaycast.ray.direction);
			}
			thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
		}
	}

	void HandleDimensionObjectStateChange(VisibilityState nextState) {
		if (nextState == VisibilityState.invisible && isHeld) {
			Drop();
		}
	}

	Portal DetermineHoveredThroughPortal() {
		RaycastHits reticleHitInfo = Interact.instance.GetRaycastHits();
		bool portalCopyExists = portalableObject.fakeCopyInstance != null;
		bool clickedOnPortalCopy = portalCopyExists && reticleHitInfo.raycastWasAHit && reticleHitInfo.lastRaycast.hitInfo.collider.gameObject == portalableObject.fakeCopyInstance.gameObject;
		Portal hoveredThroughPortal = Interact.instance.GetRaycastHits().firstRaycast.portalHit;
		if (portalableObject.sittingInPortal == hoveredThroughPortal) {
			hoveredThroughPortal = null;
		}
		else if (clickedOnPortalCopy) {
			hoveredThroughPortal = portalableObject.sittingInPortal?.otherPortal;
		}

		return hoveredThroughPortal;
	}

	public void Pickup() {
		if (!isHeld && !onCooldown && interactable) {
			if (portalableObject != null) {
				grabbedThroughPortal = DetermineHoveredThroughPortal();
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
			if (grabbedThroughPortal) {
				thisGravity.ReorientGravityAfterPortaling(grabbedThroughPortal);
			}

			grabbedThroughPortal = null;

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
