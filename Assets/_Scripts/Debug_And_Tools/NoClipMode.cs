using Saving;
using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
// AKA GodMode
public class NoClipMode : SingletonSaveableObject<NoClipMode, NoClipMode.NoClipSave> {
	Transform playerCamera;
	PlayerMovement playerMovement;
	Rigidbody playerRigidbody;
	Collider playerCollider;
	PlayerButtonInput input;

	private bool allowGodModeInNonDevBuild = true;
	public bool noClipOn = false;
	float desiredSpeed;
	float speed;
	public float slowMoveSpeed = 15f;
	public float moveSpeed = 45;
	public float sprintSpeed = 125;
	public float middleMouseVerticalSpeed = 4;
	
	
	string label = "";
	GUIStyle style = new GUIStyle();
	void OnGUI() {
		if (!enabled || !noClipOn) return;
		// Show the player's position if you're in god mode/no clip mode
		GUI.Label(new Rect(5, 145, 100, 25), $"{playerCamera.position:F0}", style);
	}

	protected override void Awake() {
		base.Awake();
		
		playerMovement = GetComponent<PlayerMovement>();
		playerRigidbody = GetComponent<Rigidbody>();
		playerCollider = GetComponent<Collider>();
	}
	
	protected override void Start() {
		base.Start();
		
		playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
		input = PlayerButtonInput.instance;
		speed = moveSpeed;
	}

	void Update() {
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.G) ||
		    (allowGodModeInNonDevBuild && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.G))) {
			ToggleNoClip();
		}

		if (noClipOn) {
			if (DebugInput.GetKeyDown(KeyCode.LeftShift) || (allowGodModeInNonDevBuild && Input.GetKeyDown(KeyCode.LeftShift))) {
				desiredSpeed = (desiredSpeed == sprintSpeed) ? moveSpeed : sprintSpeed;
			}
			else if (DebugInput.GetKeyDown(KeyCode.LeftControl) || (allowGodModeInNonDevBuild && Input.GetKeyDown(KeyCode.LeftControl))) {
				desiredSpeed = (desiredSpeed == slowMoveSpeed) ? moveSpeed : slowMoveSpeed;
			}
			
			
			float middleMouseScroll = Input.mouseScrollDelta.y;
			Vector3 verticalScroll = transform.up * (middleMouseScroll * middleMouseVerticalSpeed);
			transform.position += verticalScroll;
		}
	}

    void FixedUpdate() {
		if (noClipOn) {
			Vector2 moveInput = input.LeftStick;

			Vector3 moveDirection = playerCamera.forward * moveInput.y + playerCamera.right * moveInput.x;

			speed = Mathf.Lerp(speed, desiredSpeed, Time.fixedDeltaTime * 6f);

			transform.position += moveDirection * (Time.fixedDeltaTime * speed * Player.instance.Scale);
		}
    }

	void ToggleNoClip() {
		desiredSpeed = moveSpeed;
		noClipOn = !noClipOn;
		debug.Log(noClipOn ? "Enabling noclip" : "Disabling noclip");
		playerMovement.enabled = !noClipOn;
		playerRigidbody.isKinematic = noClipOn;
		playerCollider.isTrigger = noClipOn;
		playerMovement.groundMovement.grounded.IsGrounded = false;
	}

	#region Saving
	// There's only one player so we don't need a UniqueId here
	public override string ID => "NoClipMode";

	[Serializable]
	public class NoClipSave : SerializableSaveObject<NoClipMode> {
		bool noClipOn;

		public NoClipSave(NoClipMode noClip) : base(noClip) {
			this.noClipOn = noClip.noClipOn;
		}

		public override void LoadSave(NoClipMode noClip) {
			noClip.noClipOn = !this.noClipOn;
			noClip.ToggleNoClip();
		}
	}
	#endregion
}
