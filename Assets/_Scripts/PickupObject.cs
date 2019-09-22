using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour, InteractableObject {

	public void OnLeftMouseButton() { }
	public void OnLeftMouseButtonFocusLost() { }
	public void OnLeftMouseButtonDown() {
		Pickup();
	}
	public void OnLeftMouseButtonUp() {

	}

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
	Rigidbody thisRigidbody;
	Renderer thisRenderer;


	void Start() {
		thisRigidbody = GetComponent<Rigidbody>();
		originalParent = transform.parent;
		player = Player.instance.transform;
		playerCam = EpitaphScreen.instance.playerCamera.transform;
		positionLastPhysicsFrame = transform.position;
		thisRenderer = GetComponent<Renderer>();
		PlayerButtonInput.instance.OnAction1Press += Drop;
	}

	private void Update() {
		if (currentCooldown > 0) {
			currentCooldown -= Time.deltaTime;
		}
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
			thisRigidbody.velocity = newVelocity;
		}
	}

	void Pickup() {
		if (!isHeld && !onCooldown) {
			//transform.parent = Player.instance.transform;
			positionLastPhysicsFrame = transform.position;
			thisRigidbody.useGravity = false;
			//thisRigidbody.isKinematic = true;
			isHeld = true;
			currentCooldown = pickupDropCooldown;
		}
	}

	void Drop() {
		if (isHeld && !onCooldown) {
			//transform.parent = originalParent;
			thisRigidbody.useGravity = true;
			//thisRigidbody.isKinematic = false;
			isHeld = false;
			currentCooldown = pickupDropCooldown;
		}
	}
}
