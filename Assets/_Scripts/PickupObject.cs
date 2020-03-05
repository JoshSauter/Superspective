using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour {
	InteractableObject interactableObject;
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
	public Rigidbody thisRigidbody;
	Renderer thisRenderer;

	public delegate void PickupObjectSimpleAction();
	public delegate void PickupObjectAction(PickupObject obj);
	public PickupObjectSimpleAction OnPickupSimple;
	public PickupObjectSimpleAction OnDropSimple;
	public PickupObjectAction OnPickup;
	public PickupObjectAction OnDrop;

	void Awake() {
		thisRigidbody = GetComponent<Rigidbody>();
		originalParent = transform.parent;

		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
			interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
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
	}

	private void Update() {
		if (currentCooldown > 0) {
			currentCooldown -= Time.deltaTime;
		}
	}

	void OnCollisionStay(Collision other) {
		PillarDimensionObject thisDimensionObj = GetComponent<PillarDimensionObject>();
		PillarDimensionObject otherDimensionObj = Utils.FindDimensionObjectRecursively(other.gameObject.transform);
	}

	void FixedUpdate() {
		if (isHeld) {
			float distance = holdDistance + Mathf.Max(thisRenderer.bounds.size.x, thisRenderer.bounds.size.z);
			Vector3 targetPos = player.position + playerCam.forward * holdDistance;
			RaycastHit hitInfo;
			int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
			int layerMask = ~(1 << ignoreRaycastLayer | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Invisible"));
			int tempLayer = gameObject.layer;
			gameObject.layer = ignoreRaycastLayer;
			if (Physics.Raycast(player.position, playerCam.forward, out hitInfo, holdDistance, layerMask)) {
				targetPos = player.position + playerCam.forward * hitInfo.distance;
			}
			gameObject.layer = tempLayer;
			Vector3 diff = targetPos - thisRigidbody.position;
			Vector3 cubeToPlayer = Player.instance.collider.ClosestPointOnBounds(transform.position) - transform.position;
			cubeToPlayer = cubeToPlayer.normalized * (Mathf.Max(0.01f, cubeToPlayer.magnitude - thisRenderer.bounds.size.x/2f));
			Vector3 newVelocity = Vector3.Lerp(thisRigidbody.velocity, followSpeed * diff, followLerpSpeed * Time.fixedDeltaTime);
			bool movingTowardsPlayer = Vector3.Dot(newVelocity.normalized, cubeToPlayer.normalized) > 0.5f;
			if (cubeToPlayer.magnitude < minDistanceFromPlayer && movingTowardsPlayer) {
				newVelocity = Vector3.ProjectOnPlane(newVelocity, -cubeToPlayer.normalized);
			}
			thisRigidbody.AddForce(newVelocity - thisRigidbody.velocity, ForceMode.VelocityChange);
		}
	}

	void HandleDimensionObjectStateChange(VisibilityState nextState) {
		if (nextState == VisibilityState.invisible && isHeld) {
			Drop();
		}
	}

	public void Pickup() {
		if (!isHeld && !onCooldown && interactable) {
			DontDestroyOnLoad(gameObject);

			//transform.parent = Player.instance.transform;
			positionLastPhysicsFrame = transform.position;
			thisRigidbody.useGravity = false;
			thisRigidbody.isKinematic = false;
			isHeld = true;
			currentCooldown = pickupDropCooldown;

			OnPickupSimple?.Invoke();
			OnPickup?.Invoke(this);
		}
	}

	public void Drop() {
		if (isHeld && !onCooldown && interactable) {
			//transform.parent = originalParent;
			thisRigidbody.useGravity = true;
			//thisRigidbody.isKinematic = false;
			isHeld = false;
			currentCooldown = pickupDropCooldown;

			OnDropSimple?.Invoke();
			OnDrop?.Invoke(this);
		}
	}
}
