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
	private const float MIN_SPEED = 0.01f;
	private const float BASE_SPEED = 10;
	private const float MAX_SPEED = 300;
	private const float SPEED_MULTIPLIER_DELTA = 1.02f; // Multiplier (or divisor) per frame to the speed
	float speed;
	public float middleMouseVerticalSpeed = 4;
	
	
	string label = "";
	GUIStyle style = new GUIStyle();
	void OnGUI() {
		if (!enabled || !noClipOn) return;
		// Show the player's position if you're in god mode/no clip mode
		GUI.Label(new Rect(5, 145, 100, 25), $"{playerCamera.position:F0}, Speed: {speed:F2} (Ctrl -, Shift +)", style);
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
		speed = BASE_SPEED;
	}

	void Update() {
		if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.G) ||
		    (allowGodModeInNonDevBuild && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.G))) {
			ToggleNoClip();
		}

		if (noClipOn) {
			if (DebugInput.GetKey(KeyCode.LeftShift) || (allowGodModeInNonDevBuild && Input.GetKey(KeyCode.LeftShift))) {
				//speed = Mathf.Clamp(Mathf.Pow(speed, SPEED_MULTIPLIER_DELTA), MIN_SPEED, MAX_SPEED);
				speed = Mathf.Clamp(speed * SPEED_MULTIPLIER_DELTA, MIN_SPEED, MAX_SPEED);
			}
			else if (DebugInput.GetKey(KeyCode.LeftControl) || (allowGodModeInNonDevBuild && Input.GetKey(KeyCode.LeftControl))) {
				//speed = Mathf.Clamp(Mathf.Pow(speed, 1f / SPEED_MULTIPLIER_DELTA), MIN_SPEED, MAX_SPEED);
				speed = Mathf.Clamp(speed / SPEED_MULTIPLIER_DELTA, MIN_SPEED, MAX_SPEED);
			}
			
			
			float middleMouseScroll = Input.mouseScrollDelta.y;
			Vector3 verticalScroll = transform.up * (middleMouseScroll * middleMouseVerticalSpeed) * (speed / BASE_SPEED);
			transform.position += verticalScroll;
		}
	}

    void FixedUpdate() {
		if (noClipOn) {
			Vector2 moveInput = input.LeftStick;

			Vector3 moveDirection = playerCamera.forward * moveInput.y + playerCamera.right * moveInput.x;

			transform.position += moveDirection * (Time.fixedDeltaTime * speed * Player.instance.Scale);
		}
    }

	void ToggleNoClip() {
		speed = BASE_SPEED;
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
