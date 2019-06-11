﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ViewLockInfo {
	public Vector3 camPosition;
	public Vector3 camRotationEuler;
}

[RequireComponent(typeof(Collider))]
public class ViewLockObject : MonoBehaviour, InteractableObject {
	public ViewLockInfo[] viewLockOptions;
	public float viewLockTime = 0.75f;
	public float viewUnlockTime = 0.25f;

	public bool focusIsLocked = false;
	public Collider hitbox;
	Transform playerCamera;

    void Start() {
		hitbox = GetComponent<Collider>();
		hitbox.isTrigger = true;

		playerCamera = EpitaphScreen.instance.playerCamera.transform;
    }

	public void OnLeftMouseButtonDown() {
		hitbox.enabled = false;
		PlayerLook.instance.SetViewLock(this, ClosestViewLock(playerCamera.position, playerCamera.rotation));
		PlayerMovement.instance.thisRigidbody.isKinematic = true;
	}

	public void OnLeftMouseButton() { }
	public void OnLeftMouseButtonUp() { }
	public void OnLeftMouseButtonFocusLost() { }

	void Update() {
		if (focusIsLocked && PlayerButtonInput.instance.LeftStickHeld) {
			focusIsLocked = false;
			PlayerLook.instance.UnlockView();
			PlayerMovement.instance.thisRigidbody.isKinematic = false;
		} 
	}

	private ViewLockInfo ClosestViewLock(Vector3 pos, Quaternion rot) {
		int indexOfWinner = -1;
		float minRotationAngle = float.MaxValue;
		float minPositionDistance = float.MaxValue;

		for (int i = 0; i < viewLockOptions.Length; i++) {
			ViewLockInfo viewLock = viewLockOptions[i];
			Vector3 viewLockWorldPos = transform.TransformPoint(viewLock.camPosition);
			Quaternion viewLockWorldRot = transform.rotation * Quaternion.Euler(viewLock.camRotationEuler);
			float distance = (pos - viewLockWorldPos).magnitude;
			float angleBetween = Quaternion.Angle(viewLockWorldRot, rot);
			if (distance < minPositionDistance) {
				minPositionDistance = distance;
				indexOfWinner = i;
			}
			if (angleBetween < minRotationAngle) {
				minRotationAngle = angleBetween;
			}
		}

		return viewLockOptions[indexOfWinner];
	}
}