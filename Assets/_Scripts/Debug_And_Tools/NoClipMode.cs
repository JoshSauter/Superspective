using Saving;
using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NoClipMode : SaveableObject<NoClipMode, NoClipMode.NoClipSave> {
	Transform playerCamera;
	PlayerMovement playerMovement;
	Rigidbody playerRigidbody;
	Collider playerCollider;
	PlayerButtonInput input;

	public bool noClipOn = false;
	public float slowMoveSpeed = 15f;
	public float moveSpeed = 45;
	public float sprintSpeed = 125;
	public float middleMouseVerticalSpeed = 10;

	protected override void Start() {
		base.Start();
		playerMovement = GetComponent<PlayerMovement>();
		playerRigidbody = GetComponent<Rigidbody>();
		playerCollider = GetComponent<Collider>();
		playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
		input = PlayerButtonInput.instance;
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.G)) {
			ToggleNoClip();
		}

		if (noClipOn) {
			Vector2 moveInput = input.LeftStick;

			Vector3 moveDirection = playerCamera.forward * moveInput.y + playerCamera.right * moveInput.x;
			float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : Input.GetKey(KeyCode.LeftControl) ? slowMoveSpeed : moveSpeed;

			float middleMouseScroll = Input.mouseScrollDelta.y;
			Vector3 verticalScroll = transform.up * (middleMouseScroll * middleMouseVerticalSpeed);

			transform.position += (verticalScroll + moveDirection) * (Time.deltaTime * speed);
		}
    }

	void ToggleNoClip() {
		noClipOn = !noClipOn;
		Debug.Log(noClipOn ? "Enabling noclip" : "Disabling noclip");
		playerMovement.enabled = !noClipOn;
		playerRigidbody.isKinematic = noClipOn;
		playerCollider.isTrigger = noClipOn;
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
