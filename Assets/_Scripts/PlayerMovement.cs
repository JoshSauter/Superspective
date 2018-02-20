using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
	public Vector3 curVelocity {
		get { return thisRigidbody.velocity; }
	}
	float acceleration = 75f;
	float backwardsSpeed = 0.7f;
	public float walkSpeed = 4f;
	public float runSpeed = 7f;
	public float windResistanceMultiplier = 0.2f;
	float movespeed;
	private Rigidbody thisRigidbody;
	private PlayerButtonInput input;

	CapsuleCollider thisCollider;

#region IsGrounded characteristics
	// Dot(face normal, Vector3.up) must be greater than this value to be considered "ground"
	public float isGroundThreshold = 0.6f;
	int layerMask;
#endregion

	private void Awake() {
		input = PlayerButtonInput.instance;
	}

	// Use this for initialization
	void Start() {
		movespeed = walkSpeed;
		thisRigidbody = GetComponent<Rigidbody>();
		thisCollider = GetComponent<CapsuleCollider>();

		layerMask = 1 << LayerMask.NameToLayer("WalkableObject");
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
		bool grounded = IsGrounded(out ground);

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
		// TODO: Make this real
		if (grounded && input.SpaceHeld) {
			thisRigidbody.AddForce(Vector3.up * 4, ForceMode.Impulse);
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
			//thisRigidbody.velocity = Vector3.zero;
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

		// If no keys are pressed, decelerate to a horizontal stop
		if (!(input.UpHeld || input.DownHeld || input.RightHeld || input.LeftHeld)) {
			Vector2 horizontalVelocity = HorizontalVelocity();
			horizontalVelocity = Vector2.Lerp(horizontalVelocity, Vector2.zero, 0.15f);
			thisRigidbody.velocity = new Vector3(horizontalVelocity.x, thisRigidbody.velocity.y, horizontalVelocity.y);
			//thisRigidbody.velocity = 
		}
		else {
			moveDirection = moveDirection.normalized * movespeed;
			Vector3 desiredVelocity = new Vector3(moveDirection.x, thisRigidbody.velocity.y, moveDirection.z);
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, desiredVelocity, 0.075f);
		}
		// Apply wind resistance
		if (thisRigidbody.velocity.y < 0) {
			thisRigidbody.AddForce(Vector3.up * (-thisRigidbody.velocity.y) * windResistanceMultiplier);
		}
	}

	/// <summary>
	/// Deprecated.
	/// </summary>
	/// <param name="direction"></param>
	void Move(Vector3 direction) {
		// Walking backwards is slower than forwards
		// TODO: This needs to operate based on transform.forward
		if (direction.x == 0) {
			direction.x *= backwardsSpeed;
		}
		Vector3 accelForce = direction * acceleration;
		thisRigidbody.AddForce(accelForce, ForceMode.Acceleration);

		Vector2 curHorizontalVelocity = HorizontalVelocity();
		if (curHorizontalVelocity.magnitude > movespeed) {
			Vector2 cappedHorizontalMovespeed = curHorizontalVelocity.normalized * movespeed;
			thisRigidbody.velocity = new Vector3(cappedHorizontalMovespeed.x, thisRigidbody.velocity.y, cappedHorizontalMovespeed.y);
		}

		Vector3 curDirectionSpeed = new Vector3(thisRigidbody.velocity.x, 0, thisRigidbody.velocity.z);
		float facingSameDirection = Vector3.Dot(curDirectionSpeed.normalized, transform.forward);
		if (facingSameDirection < 0) {
			Vector3 curVel = thisRigidbody.velocity;
			float multiplier = Mathf.Lerp(1, backwardsSpeed, -facingSameDirection);
			curVel.x *= multiplier;
			curVel.z *= multiplier;
			thisRigidbody.velocity = curVel;
		}
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
		RaycastHit[] allHit = Physics.SphereCastAll(transform.position, thisCollider.radius, -transform.up, (transform.localScale.y * thisCollider.height) / 2f + .02f, layerMask);
		foreach (RaycastHit curHitInfo in allHit) {
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
