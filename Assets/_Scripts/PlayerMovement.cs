using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using Audio;
using Saving;
using System;
using SerializableClasses;
using NaughtyAttributes;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : Singleton<PlayerMovement>, SaveableObject {
	public bool DEBUG = false;
	public bool autoRun = false;
	DebugLogger debug;

	private float scale { get { return transform.localScale.y; } }
	public Vector3 curVelocity {
		get { return thisRigidbody.velocity; }
	}
	const float accelerationLerpSpeed = 15f;
	const float airspeedControlFactor = 0.4f;
	const float decelerationLerpSpeed = 12f;
	const float backwardsSpeed = 1f;
	private const float _walkSpeed = 9f;
	public float walkSpeed { get { return _walkSpeed * scale; } }
	private const float _runSpeed = 14f;
	public float runSpeed { get { return _runSpeed * scale; } }
	private const float desiredMovespeedLerpSpeed = 10;

	// Jump Settings
	public enum JumpState {
		JumpReady,
		Jumping,
		JumpOnCooldown
	}
	JumpState jumpState = JumpState.JumpReady;
	float timeSpentJumping = 0.0f;
	private const float _jumpForce = 936;
	public float jumpForce { get { return _jumpForce * scale; } }
	public const float windResistanceMultiplier = 0.4f;

	float jumpCooldownRemaining = 0.0f;				// Prevents player from jumping again while > 0
	const float jumpCooldown = 0.2f;				// Time after landing before jumping is available again
	bool underMinJumpTime = false;					// Used to delay otherwise immediate checks for isGrounded right after jumping
	const float minJumpTime = 0.5f;					// as long as underMinJumpTime

	float movespeed;
	public Rigidbody thisRigidbody;
	private PlayerButtonInput input;

	// Staircase handling characteristics
	const float _maxStepHeight = 0.6f;
	float maxStepHeight { get { return _maxStepHeight * scale; } }
	// How far do we move into the step before raycasting down?
	const float _stepOverbiteMagnitude = 0.15f;
	float stepOverbiteMagnitude { get { return _stepOverbiteMagnitude * scale; } }

	CapsuleCollider thisCollider;
	MeshRenderer thisRenderer;

	bool stopped = false;

	List<ContactPoint> allContactThisFrame = new List<ContactPoint>();
	public Vector3 bottomOfPlayer { get { return transform.position - transform.up * 2.5f; } }

	#region IsGrounded characteristics
	public struct GroundedState {
		public bool isGrounded {
			get { return ground != null; }
			// Only allow setting isGrounded to false (clear the state)
			set {
				if (!value) {
					contact = default(ContactPoint);
					framesWaitedAfterLeavingGround = 0;
				}
			}
		}
		public Collider ground => contact.otherCollider;
		public bool standingOnHeldObject => instance.IsStandingOnHeldObject(contact);

		public ContactPoint contact;
		public float framesWaitedAfterLeavingGround;
	}
	public GroundedState grounded = default;
	private const int framesToWaitAfterLeavingGround = 3;
	// Dot(face normal, transform.up) must be greater than this value to be considered "ground"
	public const float isGroundThreshold = 0.6f;
	public const float isGroundedSpherecastDistance = 0.5f;
	#endregion

	public SoundEffect jumpSound;

	#region events
	public delegate void PlayerMovementAction();
	public PlayerMovementAction OnJump;
	public PlayerMovementAction OnJumpLanding;
	public PlayerMovementAction OnStaircaseStepUp;
	#endregion

	// Inspector-only:
	[ShowNativeProperty]
	bool isGrounded => grounded.isGrounded;
	[ShowNativeProperty]
	bool standingOnHeldObject => grounded.standingOnHeldObject;
	[ShowNativeProperty]
	string ground => grounded.ground?.gameObject.name ?? "";
	

	private void Awake() {
		input = PlayerButtonInput.instance;
		debug = new DebugLogger(this, () => DEBUG);
		thisRigidbody = GetComponent<Rigidbody>();
		thisCollider = GetComponent<CapsuleCollider>();
		thisRenderer = GetComponentInChildren<MeshRenderer>();
	}

	// Use this for initialization
	IEnumerator Start() {
		movespeed = walkSpeed;

		thisRigidbody.isKinematic = true;
		yield return new WaitUntil(() => !LevelManager.instance.isCurrentlyLoadingScenes);
		yield return new WaitForSeconds(1f);
		thisRigidbody.isKinematic = false;
	}

	private void Update() {
		if (Input.GetKeyDown(KeyCode.Tilde)) {
			autoRun = !autoRun;
		}

		if (input.ShiftHeld || autoRun) {
			movespeed = Mathf.Lerp(movespeed, runSpeed, desiredMovespeedLerpSpeed * Time.deltaTime);
		}
		else {
			movespeed = Mathf.Lerp(movespeed, walkSpeed, desiredMovespeedLerpSpeed * Time.deltaTime);
		}
	}

	void FixedUpdate() {
		UpdateGroundedState();

		if (stopped || grounded.standingOnHeldObject) return;

		UpdateJumping();

		Vector3 desiredVelocity = thisRigidbody.velocity;
		if (grounded.isGrounded) {
			desiredVelocity = CalculateGroundMovement(grounded.contact);
		}
		else {
			desiredVelocity = CalculateAirMovement();
		}

		// Prevent player from floating around on cubes they're holding...
		if (grounded.standingOnHeldObject) {
			desiredVelocity += 4*Physics.gravity * Time.fixedDeltaTime;
		}

		float movingBackward = Vector2.Dot(new Vector2(desiredVelocity.x, desiredVelocity.z), new Vector2(transform.forward.x, transform.forward.z));
		if (movingBackward < -0.5f) {
			float slowdownAmount = Mathf.InverseLerp(-.5f, -1, movingBackward);
			desiredVelocity.x *= Mathf.Lerp(1, backwardsSpeed, slowdownAmount);
			desiredVelocity.z *= Mathf.Lerp(1, backwardsSpeed, slowdownAmount);
		}

		if (!input.LeftStickHeld && !input.SpaceHeld && grounded.ground != null && grounded.ground.CompareTag("Staircase")) {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeAll;
		}
		else {
			thisRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		}

		StepFound stepFound = DetectStep(desiredVelocity, grounded.contact, grounded.isGrounded);
		if (stepFound != null) {
			transform.Translate(stepFound.stepOffset, Space.World);
			Player.instance.cameraFollow.SetLerpSpeed(CameraFollow.desiredLerpSpeed);
			if (Vector3.Dot(transform.up, stepFound.stepOffset) > 0) {
				OnStaircaseStepUp?.Invoke();
			}
		}
		thisRigidbody.useGravity = !grounded.isGrounded;

		thisRigidbody.velocity = desiredVelocity;

		// Apply wind resistance
		Vector3 projectedVertVelocity = ProjectedVerticalVelocity();
		if (!grounded.isGrounded && Vector3.Dot(Physics.gravity.normalized, projectedVertVelocity.normalized) > 0) {
			thisRigidbody.AddForce(transform.up * projectedVertVelocity.magnitude * thisRigidbody.mass * windResistanceMultiplier);
		}

		allContactThisFrame.Clear();
	}

	private void OnCollisionStay(Collision collision) {
		allContactThisFrame.AddRange(collision.contacts);
	}

	/// <summary>
	/// Calculates player movement when the player is on (or close enough) to the ground.
	/// Movement is perpendicular to the ground's normal vector.
	/// </summary>
	/// <param name="ground">RaycastHit info for the walkable object that passes the IsGrounded test</param>
	/// <returns>Desired Velocity according to current input</returns>
	Vector3 CalculateGroundMovement(ContactPoint ground) {
		Vector3 up = ground.normal;
		Vector3 right = Vector3.Cross(Vector3.Cross(up, transform.right), up);
		Vector3 forward = Vector3.Cross(Vector3.Cross(up, transform.forward), up);

		Vector3 moveDirection = forward * input.LeftStick.y + right * input.LeftStick.x;
		if (autoRun) {
			moveDirection = forward;
		}

		// DEBUG:
		if (DEBUG) {
			//Debug.DrawRay(ground.point, ground.normal * 10, Color.red, 0.2f);
			Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.blue, 0.1f);
		}

		// If no keys are pressed, decelerate to a stop
		if (!input.LeftStickHeld && !autoRun) {
			Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
			Vector3 desiredHorizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, decelerationLerpSpeed * Time.fixedDeltaTime);
			return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
		}
		else {
			float adjustedMovespeed = (ground.otherCollider.CompareTag("Staircase")) ? walkSpeed : movespeed;
			return Vector3.Lerp(thisRigidbody.velocity, moveDirection * adjustedMovespeed, accelerationLerpSpeed * Time.fixedDeltaTime);
		}
	}

	/// <summary>
	/// Handles player movement when the player is in the air.
	/// Movement is perpendicular to Vector3.up.
	/// </summary>
	/// <returns>Desired Velocity according to current input</returns>
	Vector3 CalculateAirMovement() {
		Vector3 moveDirection = input.LeftStick.y * transform.forward + input.LeftStick.x * transform.right;
		if (autoRun) {
			moveDirection = transform.forward;
		}

		// DEBUG:
		Debug.DrawRay(transform.position, moveDirection.normalized * 3, Color.green, 0.1f);

		// Handle mid-air collision with obstacles
		moveDirection = AirCollisionMovementAdjustment(moveDirection * movespeed);

		// If no keys are pressed, decelerate to a horizontal stop
		if (!input.LeftStickHeld && !autoRun) {
			Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
			Vector3 desiredHorizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, decelerationLerpSpeed * Time.fixedDeltaTime);
			return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
		}
		else {
			Vector3 horizontalVelocity = ProjectedHorizontalVelocity();
			Vector3 desiredHorizontalVelocity = Vector3.Lerp(horizontalVelocity, moveDirection, airspeedControlFactor * accelerationLerpSpeed * Time.fixedDeltaTime);
			return desiredHorizontalVelocity + (thisRigidbody.velocity - horizontalVelocity);
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
		
		if (obstacle.collider == null || obstacle.collider.isTrigger || (obstacle.collider.gameObject.GetComponent<PickupObject>()?.isHeld ?? false)) {
			return movementVector;
		}
		else {
			Vector3 newMovementVector = Vector3.ProjectOnPlane(movementVector, obstacle.normal);
			if (Vector3.Dot(ProjectedVerticalVelocity(), newMovementVector) > 0) {
				debug.LogWarning("movementVector:" + movementVector + "\nnewMovementVector:" + newMovementVector);
			}
			return newMovementVector;
		}
	}

	IEnumerator PrintMaxHeight(Vector3 startPosition) {
		float maxHeight = 0;
		yield return new WaitForSeconds(minJumpTime/2f);
		while (!grounded.isGrounded) {
			float height = Vector3.Dot(transform.up, transform.position - startPosition);
			if (height > maxHeight) {
				maxHeight = height;
			}
			yield return new WaitForFixedUpdate();
		}
		debug.Log("Highest jump height: " + maxHeight);
	}

	/// <summary>
	/// Removes any current y-direction movement on the player, applies a one time impulse force to the player upwards,
	/// then waits jumpCooldown seconds to be ready again.
	/// </summary>
	void Jump() {
		OnJump?.Invoke();
		jumpSound.Play();

		timeSpentJumping = 0.0f;
		underMinJumpTime = true;
		grounded.isGrounded = false;
		
		Vector3 jumpVector = -Physics.gravity.normalized * jumpForce;
		thisRigidbody.AddForce(jumpVector, ForceMode.Impulse);
		StartCoroutine(PrintMaxHeight(transform.position));

		jumpState = JumpState.Jumping;
	}

	void UpdateJumping() {
		switch (jumpState) {
			case JumpState.JumpReady:
				if (input.SpaceHeld && grounded.isGrounded && !grounded.standingOnHeldObject) {
					Jump();
				}
				return;
			case JumpState.Jumping:
				timeSpentJumping += Time.fixedDeltaTime;
				underMinJumpTime = timeSpentJumping < minJumpTime;
				if (underMinJumpTime) return;
				else if (grounded.isGrounded) {
					jumpCooldownRemaining = jumpCooldown;
					OnJumpLanding?.Invoke();
					jumpState = JumpState.JumpOnCooldown;
				}
				return;
			case JumpState.JumpOnCooldown:
				jumpCooldownRemaining = Mathf.Max(jumpCooldownRemaining - Time.fixedDeltaTime, 0.0f);
				if (jumpCooldownRemaining == 0.0f) {
					jumpState = JumpState.JumpReady;
				}
				return;
		}
	}

	public Vector3 ProjectedHorizontalVelocity() {
		return Vector3.ProjectOnPlane(thisRigidbody.velocity, transform.up);
	}

	public Vector3 ProjectedVerticalVelocity() {
		return thisRigidbody.velocity - ProjectedHorizontalVelocity();
	}

	class StepFound {
		public ContactPoint contact;
		public Vector3 stepOffset;

		public StepFound(ContactPoint contact, Vector3 stepOffset) {
			this.contact = contact;
			this.stepOffset = stepOffset;
		}
	}

	StepFound DetectStep(Vector3 desiredVelocity, ContactPoint ground, bool isGrounded) {
		// If player is not moving, don't do any raycasts, just return
		if (desiredVelocity.magnitude < 0.1f) {
			return null;
		}

		foreach (ContactPoint contact in allContactThisFrame) {
			bool isBelowMaxStepHeight = Mathf.Abs((Vector3.Dot(contact.point, transform.up) - Vector3.Dot(ground.point, transform.up))) < maxStepHeight;
			// Basically all this nonsense is to get the contact surface's normal rather than the "contact normal" which is different
			RaycastHit hitInfo = default(RaycastHit);
			bool rayHit = false;
			if (isBelowMaxStepHeight) {
				Vector3 rayLowStartPos = bottomOfPlayer + transform.up * 0.01f;
				Vector3 bottomOfPlayerToContactPoint = contact.point - bottomOfPlayer;
				Vector3 rayDirection = Vector3.ProjectOnPlane(bottomOfPlayerToContactPoint, transform.up).normalized;
				if (rayDirection.magnitude > 0) {
					Debug.DrawRay(rayLowStartPos, rayDirection * thisCollider.radius * 2, Color.blue);
					rayHit = contact.otherCollider.Raycast(new Ray(rayLowStartPos, rayDirection), out hitInfo, thisCollider.radius * 2);
				}
			}
			bool isWallNormal = rayHit && Mathf.Abs(Vector3.Dot(hitInfo.normal, transform.up)) < 0.1f;
			bool isInDirectionOfMovement = rayHit && Vector3.Dot(-hitInfo.normal, desiredVelocity.normalized) > 0f;
			//if (ground.otherCollider == null || contact.otherCollider.gameObject != ground.otherCollider.gameObject) {
			//	float t = Vector3.Dot(-hitInfo.normal, desiredVelocity.normalized);
			//	if (Mathf.Abs(t) > 0.1f) {
			//		Debug.LogWarning(t);
			//	}
			//}

			StepFound step;
			if (isBelowMaxStepHeight && isWallNormal && isInDirectionOfMovement && GetStepInfo(out step, contact, ground, isGrounded)) {
				return step;
			}
		}

		return null;
	}

	bool GetStepInfo(out StepFound step, ContactPoint contact, ContactPoint ground, bool isGrounded) {
		step = null;
		RaycastHit stepTest;

		Vector3 stepOverbite = Vector3.ProjectOnPlane(-contact.normal.normalized, transform.up).normalized * stepOverbiteMagnitude;

		// Start the raycast position directly above the contact point with the step
		Debug.DrawRay(contact.point, transform.up * maxStepHeight * 0.8f, Color.blue, 10);
		Vector3 raycastStartPos = contact.point + transform.up * maxStepHeight * 0.8f;
		// Move the raycast inwards towards the stair (we will be raycasting down at the stair)
		Debug.DrawRay(raycastStartPos, stepOverbite, Color.red, 10);
		raycastStartPos += stepOverbite;
		Vector3 direction = -transform.up;

		Debug.DrawRay(raycastStartPos, direction * maxStepHeight, Color.green, 10);
		bool stepFound = contact.otherCollider.Raycast(new Ray(raycastStartPos, direction), out stepTest, maxStepHeight);
		if (stepFound) {
			Vector3 groundPointOrBottomOfPlayer = isGrounded ? ground.point : bottomOfPlayer;
			float stepHeight = Vector3.Dot(transform.up, stepTest.point - groundPointOrBottomOfPlayer);

			Vector3 stepOffset = stepOverbite + transform.up * (stepHeight + 0.02f);
			Debug.DrawRay(contact.point, stepOffset, Color.black, 10);
			step = new StepFound(contact, stepOffset);
			debug.Log($"Step: {contact}\n{stepOffset:F3}\nstepHeight:{stepHeight}");
		}

		return stepFound;
	}

	public void UpdateGroundedState() {
		ContactPoint groundContactPoint = default(ContactPoint);
		float maxGroundTest = isGroundThreshold;    // Amount upwards-facing the most ground-like object is
		foreach (ContactPoint contact in allContactThisFrame) {

			float groundTest = Vector3.Dot(contact.normal, transform.up);
			if (groundTest > maxGroundTest) {
				groundContactPoint = contact;
				maxGroundTest = groundTest;
			}
		}

		// Was a ground object found?
		bool isGroundedNow = (maxGroundTest > isGroundThreshold) && !underMinJumpTime;
		if (isGroundedNow) {
			grounded.framesWaitedAfterLeavingGround = 0;
			grounded.contact = groundContactPoint;
		}
		// If we were grounded last FixedUpdate and not grounded now
		else if (grounded.isGrounded) {
			// Wait a few fixed updates before saying that the player is ungrounded
			if (grounded.framesWaitedAfterLeavingGround >= framesToWaitAfterLeavingGround) {
				grounded.contact = default(ContactPoint);
				grounded.framesWaitedAfterLeavingGround = 0;
			}
			else {
				grounded.framesWaitedAfterLeavingGround++;
			}
		}
		// Not grounded anytime recently
		else {
			grounded.contact = default(ContactPoint);
		}
	}

	bool IsStandingOnHeldObject(ContactPoint contact) {
		PickupObject maybeCube1 = null, maybeCube2 = null;
		if (contact.thisCollider != null) {
			maybeCube1 = contact.thisCollider.GetComponent<PickupObject>();
		}
		if (contact.otherCollider != null) {
			maybeCube2 = contact.otherCollider.GetComponent<PickupObject>();
		}
		bool cube1IsHeld = maybeCube1 != null && maybeCube1.isHeld;
		bool cube2IsHeld = maybeCube2 != null && maybeCube2.isHeld;
		//debug.Log($"Grounded: {grounded.isGrounded}\nCube1IsHeld: {cube1IsHeld}\nCube2IsHeld: {cube2IsHeld}");
		return (cube1IsHeld || cube2IsHeld);
	}

	public void StopMovement() {
		stopped = true;
		thisRigidbody.velocity = Vector3.zero;
	}

	public void ResumeMovement() {
		stopped = false;
	}

	#region Saving
	public bool SkipSave { get; set; }
	// There's only one player so we don't need a UniqueId here
	public string ID => "PlayerMovement";

	[Serializable]
	class PlayerMovementSave {
		bool DEBUG;
		bool autoRun;

		int jumpState;
		float timeSpentJumping;

		float jumpCooldownRemaining;
		bool underMinJumpTime;
		float movespeed;

		SerializableVector3 playerGravityDirection;
		SerializableVector3 thisRigidbodyVelocity;
		bool thisRigidbodyKinematic;
		bool thisRigidbodyUseGravity;
		float thisRigidbodyMass;

		bool stopped;

		public PlayerMovementSave(PlayerMovement playerMovement) {
			this.DEBUG = playerMovement.DEBUG;
			this.autoRun = playerMovement.autoRun;
			this.jumpState = (int)playerMovement.jumpState;
			this.timeSpentJumping = playerMovement.timeSpentJumping;

			this.jumpCooldownRemaining = playerMovement.jumpCooldownRemaining;
			this.underMinJumpTime = playerMovement.underMinJumpTime;
			this.movespeed = playerMovement.movespeed;

			this.playerGravityDirection = Physics.gravity.normalized;
			this.thisRigidbodyVelocity = playerMovement.thisRigidbody.velocity;
			this.thisRigidbodyKinematic = playerMovement.thisRigidbody.isKinematic;
			this.thisRigidbodyUseGravity = playerMovement.thisRigidbody.useGravity;
			this.thisRigidbodyMass = playerMovement.thisRigidbody.mass;

			this.stopped = playerMovement.stopped;
		}

		public void LoadSave(PlayerMovement playerMovement) {
			playerMovement.DEBUG = this.DEBUG;
			playerMovement.autoRun = this.autoRun;
			playerMovement.jumpState = (JumpState)this.jumpState;
			playerMovement.timeSpentJumping = this.timeSpentJumping;

			playerMovement.jumpCooldownRemaining = this.jumpCooldownRemaining;
			playerMovement.underMinJumpTime = this.underMinJumpTime;
			playerMovement.movespeed = this.movespeed;

			// Don't know a better place to restore gravity direction
			Physics.gravity = Physics.gravity.magnitude * (Vector3)this.playerGravityDirection;
			playerMovement.thisRigidbody.velocity = this.thisRigidbodyVelocity;
			playerMovement.thisRigidbody.isKinematic = this.thisRigidbodyKinematic;
			playerMovement.thisRigidbody.useGravity = this.thisRigidbodyUseGravity;
			playerMovement.thisRigidbody.mass = this.thisRigidbodyMass;

			playerMovement.stopped = this.stopped;
		}
	}

	public object GetSaveObject() {
		return new PlayerMovementSave(this); ;
	}

	public void LoadFromSavedObject(object savedObject) {
		PlayerMovementSave save = savedObject as PlayerMovementSave;

		save.LoadSave(this);
	}
	#endregion
}
