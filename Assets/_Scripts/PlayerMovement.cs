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
	public float walkSpeed = 4f;
	public float runSpeed = 12f;
	public float jumpForce = 30;
	public float windResistanceMultiplier = 0.2f;
	// Amount of vertical velocity added to a player if they are sliding up a wall while jumping (to combat "friction" of collisions)
	float wallFrictionJumpHelper = .275f;
	// Multiplier to jump force added to a player to keep jumps between standing and running same height
	float runningJumpHelper = 1.331f;
	bool jumpIsOnCooldown = false;
	float jumpCooldown = 0.2f;
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

		if (grounded) HandleGroundMovement(ground);
		else {
			HandleAirMovement();
			return;
		}

		Vector3 moveDirection = new Vector2();
		if (input.UpHeld) {
			moveDirection += transform.forward;
		}
		if (input.DownHeld) {
			moveDirection -= transform.forward;
		}
		if (input.RightHeld) {
			moveDirection += transform.right;
		}
		if (input.LeftHeld) {
			moveDirection -= transform.right;
		}
		// If no keys are pressed, decelerate to a stop
		if (!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))) {
			Vector2 horizontalVelocity = new Vector2(thisRigidbody.velocity.x, thisRigidbody.velocity.z);
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 0.15f);
			thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		// If at least one direction is pressed, move the desired direction
		else {
			Move(moveDirection.normalized);
		}

		// Handle jumping
		if (grounded && input.SpaceHeld && !jumpIsOnCooldown) {
			StartCoroutine(Jump());
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

		Vector3 moveDirection = new Vector2();
		if (input.UpHeld) {
			moveDirection += forward;
		}
		if (input.DownHeld) {
			moveDirection -= forward;
		}
		if (input.RightHeld) {
			moveDirection += right;
		}
		if (input.LeftHeld) {
			moveDirection -= right;
		}

		Physics.gravity = -up * Physics.gravity.magnitude;

		// DEBUG:
		Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
		Debug.DrawRay(transform.position, moveDirection.normalized*3, Color.blue, 0.1f);

		// If no keys are pressed, decelerate to a stop
		if (!(input.UpHeld || input.DownHeld || input.RightHeld || input.LeftHeld)) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 0.15f);
			thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
		}
		else {
			float adjustedMovespeed = (ground.collider.tag == "Staircase") ? walkSpeed : movespeed;
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, moveDirection.normalized * adjustedMovespeed, 0.2f);
		}
	}

	/// <summary>
	/// Handles player movement when the player is in the air.
	/// Movement is perpendicular to Vector3.up.
	/// </summary>
	void HandleAirMovement() {
		Vector3 moveDirection = new Vector3();
		if (input.UpHeld) {
			moveDirection += transform.forward;
		}
		if (input.DownHeld) {
			moveDirection -= transform.forward;
		}
		if (input.RightHeld) {
			moveDirection += transform.right;
		}
		if (input.LeftHeld) {
			moveDirection -= transform.right;
		}

		Physics.gravity = Vector3.down * Physics.gravity.magnitude;

		// DEBUG:
		Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

		// Handle mid-air collision with obstacles
		moveDirection.Normalize();
		moveDirection = AirCollisionMovementAdjustment(moveDirection * movespeed);

		// If no keys are pressed, decelerate to a horizontal stop
		if (!(input.UpHeld || input.DownHeld || input.RightHeld || input.LeftHeld)) {
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
			Vector3 verticalHelp = Vector3.zero;
			if (jumpIsOnCooldown && thisRigidbody.velocity.y > 0) {
				verticalHelp = Vector3.up * wallFrictionJumpHelper;
			}
			Vector3 newMovementVector = Vector3.ProjectOnPlane(movementVector, obstacle.normal) + verticalHelp;
			return newMovementVector;
		}
	}

	/// <summary>
	/// Deprecated.
	/// </summary>
	/// <param name="direction"></param>
	void Move(Vector3 direction) {
		Vector3 accelForce = direction * acceleration;
		thisRigidbody.AddForce(accelForce, ForceMode.Acceleration);

		Vector2 curHorizontalVelocity = HorizontalVelocity();
		if (curHorizontalVelocity.magnitude > movespeed) {
			Vector2 cappedHorizontalMovespeed = curHorizontalVelocity.normalized * movespeed;
			thisRigidbody.velocity = new Vector3(cappedHorizontalMovespeed.x, thisRigidbody.velocity.y, cappedHorizontalMovespeed.y);
		}

		Vector3 curDirectionSpeed = new Vector3(thisRigidbody.velocity.x, 0, thisRigidbody.velocity.z);
		float facingSameDirection = Vector3.Dot(curDirectionSpeed.normalized, transform.forward);
		// Walking backwards is slower than forwards
		if (facingSameDirection < 0) {
			Vector3 curVel = thisRigidbody.velocity;
			float multiplier = Mathf.Lerp(1, backwardsSpeed, -facingSameDirection);
			curVel.x *= multiplier;
			curVel.z *= multiplier;
			thisRigidbody.velocity = curVel;
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

		float multiplier = Mathf.Lerp(1, runningJumpHelper, Mathf.InverseLerp(0, movespeed, HorizontalVelocity().magnitude));
		Vector3 jumpVector = -Physics.gravity.normalized * jumpForce * multiplier;
		thisRigidbody.AddForce(jumpVector, ForceMode.Impulse);
		float startHeight = transform.position.y;
		yield return new WaitForSeconds(0.5f);
		Coroutine p = StartCoroutine(PrintMaxHeight(startHeight));
		yield return new WaitWhile(() => !grounded);
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
		RaycastHit[] allHit = Physics.SphereCastAll(transform.position, thisCollider.radius - 0.02f, -transform.up, (transform.localScale.y * thisCollider.height) / 2f + .02f);
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
