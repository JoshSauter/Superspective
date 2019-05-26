using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
	public bool DEBUG = false;
	public Vector3 curVelocity {
		get { return thisRigidbody.velocity; }
	}
	float accelerationLerpSpeed = 0.2f;
	float decelerationLerpSpeed = 0.15f;
	float backwardsSpeed = 0.8f;
	public float walkSpeed = 12f;
	public float runSpeed = 18f;
	public float jumpForce = 30;
	public float windResistanceMultiplier = 0.2f;

	bool jumpIsOnCooldown = false;				// Prevents player from jumping again while true
	float jumpCooldown = 0.2f;					// Time after landing before jumping is available again
	bool underMinJumpTime = false;				// Used to delay otherwise immediate checks for isGrounded right after jumping
	float minJumpTime = 0.5f;					// as long as underMinJumpTime
	float movespeed;
	private Rigidbody thisRigidbody;
	private PlayerButtonInput input;
	bool grounded = false;

	CapsuleCollider thisCollider;
	MeshRenderer thisRenderer;

#region IsGrounded characteristics
	// Dot(face normal, Vector3.up) must be greater than this value to be considered "ground"
	public float isGroundThreshold = 0.6f;
	public float isGroundedSpherecastDistance = 0.5f;
#endregion

	private void Awake() {
		input = PlayerButtonInput.instance;
	}

	// Use this for initialization
	void Start() {
		movespeed = walkSpeed;
		thisRigidbody = GetComponent<Rigidbody>();
		thisCollider = GetComponent<CapsuleCollider>();
		thisRenderer = GetComponentInChildren<MeshRenderer>();
	}

	private void Update() {
		if (input.ShiftHeld) {
			movespeed = walkSpeed;
		}
		else {
			movespeed = runSpeed;
		}
	}
	
	void FixedUpdate() {
		RaycastHit ground = new RaycastHit();
		grounded = IsGrounded(out ground);

		Vector3 desiredVelocity = thisRigidbody.velocity;
		if (grounded) {
			desiredVelocity = CalculateGroundMovement(ground);

			// Handle jumping
			if (input.SpaceHeld && !jumpIsOnCooldown) {
				StartCoroutine(Jump());
			}
		}
		else {
			desiredVelocity = CalculateAirMovement();
		}

		bool movingBackward = Vector2.Dot(new Vector2(desiredVelocity.x, desiredVelocity.z), new Vector2(transform.forward.x, transform.forward.z)) < -0.5f;
		if (movingBackward) {
			desiredVelocity.x *= backwardsSpeed;
			desiredVelocity.z *= backwardsSpeed;
		}

		if (!input.LeftStickHeld && !input.SpaceHeld && ground.collider != null && ground.collider.CompareTag("Staircase")) {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
		}
		else {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		}

		desiredVelocity = DetectStaircase(desiredVelocity);

		thisRigidbody.velocity = desiredVelocity;

		// Apply wind resistance
		if (!grounded && thisRigidbody.velocity.y < 0) {
			thisRigidbody.AddForce(Vector3.up * (-thisRigidbody.velocity.y) * windResistanceMultiplier);
		}
	}

	/// <summary>
	/// Calculates player movement when the player is on (or close enough) to the ground.
	/// Movement is perpendicular to the ground's normal vector.
	/// </summary>
	/// <param name="ground">RaycastHit info for the WalkableObject that passes the IsGrounded test</param>
	/// <returns>Desired Velocity according to current input</returns>
	Vector3 CalculateGroundMovement(RaycastHit ground) {
		Vector3 up = ground.normal;
		Vector3 right = Vector3.Cross(Vector3.Cross(up, transform.right), up);
		Vector3 forward = Vector3.Cross(Vector3.Cross(up, transform.forward), up);

		Vector3 moveDirection = forward * input.LeftStick.y + right * input.LeftStick.x;

		Physics.gravity = -up * Physics.gravity.magnitude;

		// DEBUG:
		if (DEBUG) {
			//Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
			Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.blue, 0.1f);
		}

		// If no keys are pressed, decelerate to a stop
		if (!input.LeftStickHeld) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 12 * Time.fixedDeltaTime);
			return new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		else {
			float adjustedMovespeed = (ground.collider.CompareTag("Staircase")) ? walkSpeed : movespeed;
			return Vector3.Lerp(thisRigidbody.velocity, moveDirection * adjustedMovespeed, accelerationLerpSpeed);
		}
	}

	/// <summary>
	/// Handles player movement when the player is in the air.
	/// Movement is perpendicular to Vector3.up.
	/// </summary>
	/// <returns>Desired Velocity according to current input</returns>
	Vector3 CalculateAirMovement() {
		Vector3 moveDirection = input.LeftStick.y * transform.forward + input.LeftStick.x * transform.right;

		Physics.gravity = Vector3.down * Physics.gravity.magnitude;

		// DEBUG:
		Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

		// Handle mid-air collision with obstacles
		moveDirection = AirCollisionMovementAdjustment(moveDirection * movespeed);

		// If no keys are pressed, decelerate to a horizontal stop
		if (!input.LeftStickHeld) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, decelerationLerpSpeed);
			return new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		else {
			Vector2 horizontalVelocity = HorizontalVelocity();
			Vector2 desiredHorizontalVelocity = new Vector2(moveDirection.x, moveDirection.z);
			Vector2 newHorizontalVelocity = Vector2.Lerp(horizontalVelocity, desiredHorizontalVelocity, 0.075f);
			return new Vector3(newHorizontalVelocity.x, thisRigidbody.velocity.y + moveDirection.y, newHorizontalVelocity.y);
		}
	}

	/// <summary>
	/// Checks the area in front of where the player wants to move for an obstacle.
	/// If one is found, adjusts the player's movement to be parallel to the obstacle's face.
	/// </summary>
	/// <param name="movementVector"></param>
	/// <returns>True if there is something in the way of the player's desired movement vector, false otherwise.</returns>
	Vector3 AirCollisionMovementAdjustment(Vector3 movementVector) {
		float rayDistance = movespeed * Time.fixedDeltaTime + thisCollider.radius;
		RaycastHit obstacle = new RaycastHit();
		Physics.Raycast(transform.position, movementVector, out obstacle, rayDistance);
		
		if (obstacle.collider == null || obstacle.collider.isTrigger) {
			return movementVector;
		}
		else {
			Vector3 newMovementVector = Vector3.ProjectOnPlane(movementVector, obstacle.normal);
			return newMovementVector;
		}
	}

	IEnumerator PrintMaxHeight(float startHeight) {
		float maxHeight = startHeight;
		while (!grounded) {
			if (transform.position.y > maxHeight) {
				maxHeight = transform.position.y;
			}
			yield return new WaitForFixedUpdate();
		}
		if (DEBUG) {
			print("Highest jump height: " + (maxHeight - startHeight));
		}
	}

	/// <summary>
	/// Removes any current y-direction movement on the player, applies a one time impulse force to the player upwards,
	/// then waits jumpCooldown seconds to be ready again.
	/// </summary>
	IEnumerator Jump() {
		jumpIsOnCooldown = true;
		underMinJumpTime = true;
		grounded = false;
		
		Vector3 jumpVector = -Physics.gravity.normalized * jumpForce;
		thisRigidbody.AddForce(jumpVector, ForceMode.Impulse);
		float startHeight = transform.position.y;
		Coroutine p = StartCoroutine(PrintMaxHeight(startHeight));
		yield return new WaitForSeconds(minJumpTime);
		underMinJumpTime = false;
		yield return new WaitUntil(() => grounded);
		yield return new WaitForSeconds(jumpCooldown);

		if (p != null)
			StopCoroutine(p);
		jumpIsOnCooldown = false;
	}

	public Vector2 HorizontalVelocity() {
		return new Vector2(thisRigidbody.velocity.x, thisRigidbody.velocity.z);
	}

	public Vector3 DetectStaircase(Vector3 desiredVelocity) {
		float maxStepHeight = 0.6f;
		Vector3 bottomOfPlayer = new Vector3(transform.position.x, thisRenderer.bounds.min.y, transform.position.z);
		Vector3 rayLowStartPos = bottomOfPlayer + Vector3.up * 0.01f;
		Vector3 rayHighStartPos = bottomOfPlayer + Vector3.up * maxStepHeight;
		Vector3 rayDirection = desiredVelocity;
		rayDirection.y = 0;
		if (rayDirection.magnitude < 0.01f) return desiredVelocity;
		rayDirection.Normalize();

		float approxDistanceThisFrame = new Vector2(desiredVelocity.x, desiredVelocity.z).magnitude * Time.fixedDeltaTime;
		float rayDistance = thisCollider.radius + approxDistanceThisFrame + 0.1f;

		Debug.DrawRay(rayLowStartPos, rayDirection * rayDistance, Color.red);
		Debug.DrawRay(rayHighStartPos, rayDirection * rayDistance, Color.yellow);

		RaycastHit bottomRaycast;
		RaycastHit stepUpRaycast;

		if (Physics.Raycast(rayLowStartPos, rayDirection, out bottomRaycast, rayDistance) && Mathf.Abs(Vector3.Dot(bottomRaycast.normal, Vector3.up)) < 0.01f) {
			if (Physics.GetIgnoreLayerCollision(gameObject.layer, bottomRaycast.collider.gameObject.layer)) return desiredVelocity;
			//print("Wall found: " + bottomRaycast.collider.name);
			if (!Physics.Raycast(rayHighStartPos, rayDirection, out stepUpRaycast, rayDistance)) {
				//print("Step found: " + bottomRaycast.collider.name);
				Vector3 projectedVelocity = Vector3.ProjectOnPlane(desiredVelocity, bottomRaycast.normal);
				projectedVelocity.y = 6f;
				desiredVelocity = projectedVelocity;
			}
		}
		
		return desiredVelocity;
	}

	/// <summary>
	/// Checks to see if any WalkableObject is below the player and within the threshold to be considered ground.
	/// </summary>
	/// <param name="hitInfo">RaycastHit info about the WalkableObject that's hit by the raycast and passes the Dot test with Vector3.up</param>
	/// <returns>True if the player is grounded, otherwise false.</returns>
	public bool IsGrounded(out RaycastHit hitInfo) {
		// If the player just jumped, don't consider them grounded no matter what
		if (underMinJumpTime) {
			hitInfo = new RaycastHit();
			return false;
		}
		float radius = thisCollider.radius - 0.02f;
		// Transform.position does not give a good y-value, we want to start close to the bottom of the collider
		Vector3 startPos = transform.position;
		startPos.y = thisRenderer.bounds.min.y + radius + 0.02f;
		float distance = isGroundedSpherecastDistance;
		RaycastHit[] allHit = Physics.SphereCastAll(
			origin: startPos,
			radius: radius,
			direction: -transform.up,
			maxDistance: distance
		);

		if (DEBUG) {
			Debug.DrawRay(startPos, -transform.up * (distance + radius), Color.yellow, 0.2f);
		}
		foreach (RaycastHit curHitInfo in allHit) {
            //if (!(curHitInfo.collider.CompareTag("Ground") || curHitInfo.collider.CompareTag("Staircase"))) continue;
            if (curHitInfo.collider.TaggedAsPlayer() || Physics.GetIgnoreLayerCollision(gameObject.layer, curHitInfo.collider.gameObject.layer)) continue;
			float groundTest = Vector3.Dot(curHitInfo.normal, transform.up);

			// Return the first ground-like object hit
			if (groundTest > isGroundThreshold) {
				hitInfo = curHitInfo;
				return true;
			}
			else {
				continue;
			}
		}
		hitInfo = new RaycastHit();
		return false;

	}
}
