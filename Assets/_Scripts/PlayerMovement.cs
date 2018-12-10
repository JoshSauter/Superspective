using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
	public bool DEBUG = false;
	public Vector3 curVelocity {
		get { return thisRigidbody.velocity; }
	}
	float acceleration = 75f;
	float backwardsSpeed = 0.7f;
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

#region IsGrounded characteristics
	// Dot(face normal, Vector3.up) must be greater than this value to be considered "ground"
	public float isGroundThreshold = 0.6f;
#endregion

	private void Awake() {
		input = PlayerButtonInput.instance;
	}

	// Use this for initialization
	void Start() {
		movespeed = walkSpeed;
		thisRigidbody = GetComponent<Rigidbody>();
		thisCollider = GetComponent<CapsuleCollider>();
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

		if (grounded) {
			HandleGroundMovement(ground);

			// Handle jumping
			if (input.SpaceHeld && !jumpIsOnCooldown) {
				StartCoroutine(Jump());
			}
		}
		else {
			HandleAirMovement();
		}

		if (!input.LeftStickHeld && !input.SpaceHeld && ground.collider != null && ground.collider.tag == "Staircase") {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
		}
		else {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		}
	}

	/// <summary>
	/// Handles player movement when the player is on (or close enough) to the ground.
	/// Movement is perpendicular to the ground's normal vector.
	/// </summary>
	/// <param name="ground">RaycastHit info for the WalkableObject that passes the IsGrounded test</param>
	void HandleGroundMovement(RaycastHit ground) {
		Vector3 up = ground.normal;
		Vector3 right = Vector3.Cross(Vector3.Cross(up, transform.right), up);
		Vector3 forward = Vector3.Cross(Vector3.Cross(up, transform.forward), up);

		Vector3 moveDirection = forward * input.LeftStick.y + right * input.LeftStick.x;

		Physics.gravity = -up * Physics.gravity.magnitude;

		// DEBUG:
		if (DEBUG) {
			Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
			Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.blue, 0.1f);
		}

		// If no keys are pressed, decelerate to a stop
		if (!input.LeftStickHeld) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 12 * Time.fixedDeltaTime);
			thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		else {
			float adjustedMovespeed = (ground.collider.tag == "Staircase") ? walkSpeed : movespeed;
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, moveDirection * adjustedMovespeed, 0.2f);
		}
	}

	/// <summary>
	/// Handles player movement when the player is in the air.
	/// Movement is perpendicular to Vector3.up.
	/// </summary>
	void HandleAirMovement() {
		Vector3 moveDirection = input.LeftStick.y * transform.forward + input.LeftStick.x * transform.right;

		Physics.gravity = Vector3.down * Physics.gravity.magnitude;

		// DEBUG:
		Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

		// Handle mid-air collision with obstacles
		moveDirection = AirCollisionMovementAdjustment(moveDirection * movespeed);

		// If no keys are pressed, decelerate to a horizontal stop
		if (!input.LeftStickHeld) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 0.15f);
			thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		else {
			Vector2 horizontalVelocity = HorizontalVelocity();
			Vector2 desiredHorizontalVelocity = new Vector2(moveDirection.x, moveDirection.z);
			Vector2 newHorizontalVelocity = Vector2.Lerp(horizontalVelocity, desiredHorizontalVelocity, 0.075f);
			thisRigidbody.velocity = new Vector3(newHorizontalVelocity.x, thisRigidbody.velocity.y + moveDirection.y, newHorizontalVelocity.y);
		}

		// Apply wind resistance
		if (thisRigidbody.velocity.y < 0) {
			thisRigidbody.AddForce(Vector3.up * (-thisRigidbody.velocity.y) * windResistanceMultiplier);
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
		RaycastHit[] allHit = Physics.SphereCastAll(transform.position, thisCollider.radius - 0.02f, -transform.up, (transform.localScale.y * thisCollider.height) / 2f + .02f);
		if (DEBUG) {
			Debug.DrawRay(transform.position, -transform.up * ((transform.localScale.y * thisCollider.height) / 2f + 0.02f), Color.yellow, 0.2f);
		}
		foreach (RaycastHit curHitInfo in allHit) {
			if (!(curHitInfo.collider.tag == "Ground" || curHitInfo.collider.tag == "Staircase")) continue;
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
