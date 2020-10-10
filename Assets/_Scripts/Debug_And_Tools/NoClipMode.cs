using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class NoClipMode : MonoBehaviour, SaveableObject {
	Transform playerCamera;
	PlayerMovement playerMovement;
	Rigidbody playerRigidbody;
	Collider playerCollider;
	PlayerButtonInput input;

	public bool noClipOn = false;
	public float moveSpeed = 15;
	public float sprintSpeed = 75;
	public float middleMouseVerticalSpeed = 10;

    void Start() {
		playerMovement = GetComponent<PlayerMovement>();
		playerRigidbody = GetComponent<Rigidbody>();
		playerCollider = GetComponent<Collider>();
		playerCamera = EpitaphScreen.instance.playerCamera.transform;
		input = PlayerButtonInput.instance;
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G)) {
			ToggleNoClip();
		}

		if (noClipOn) {
			Vector2 moveInput = input.LeftStick;

			Vector3 moveDirection = playerCamera.forward * moveInput.y + playerCamera.right * moveInput.x;
			float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

			float middleMouseScroll = Input.mouseScrollDelta.y;
			Vector3 verticalScroll = middleMouseScroll * transform.up * middleMouseVerticalSpeed;

			transform.position += (verticalScroll + moveDirection) * Time.deltaTime * speed;
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
	public string ID => "NoClipMode";

	[Serializable]
	class NoClipSave {
		bool noClipOn;

		public NoClipSave(NoClipMode noClip) {
			this.noClipOn = noClip.noClipOn;
		}

		public void LoadSave(NoClipMode noClip) {
			noClip.noClipOn = !this.noClipOn;
			noClip.ToggleNoClip();
		}
	}

	public object GetSaveObject() {
		return new NoClipSave(this); ;
	}

	public void LoadFromSavedObject(object savedObject) {
		NoClipSave save = savedObject as NoClipSave;

		save.LoadSave(this);
	}
	#endregion
}
