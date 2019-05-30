using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ViewLockObject : MonoBehaviour, InteractableObject {
	public Vector3 desiredCamPosition;
	[SerializeField]
	Vector3 desiredCamRotationEuler;
	[HideInInspector]
	public Quaternion desiredCamRotation;
	public float viewLockTime = 0.75f;
	public float viewUnlockTime = 0.5f;

	public bool focusIsLocked = false;
	public Collider hitbox;

    void Start() {
		hitbox = GetComponent<Collider>();
		hitbox.isTrigger = true;

		desiredCamRotation = Quaternion.Euler(desiredCamRotationEuler);
    }

	public void OnLeftMouseButtonDown() {
		hitbox.enabled = false;
		PlayerLook.instance.SetViewLock(this);
	}

	public void OnLeftMouseButton() { }
	public void OnLeftMouseButtonUp() { }
	public void OnLeftMouseButtonFocusLost() { }

	void Update() {
		if (focusIsLocked && PlayerButtonInput.instance.LeftStickHeld) {
			focusIsLocked = false;
			PlayerLook.instance.UnlockView();
		} 
	}
}
