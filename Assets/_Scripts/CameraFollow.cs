using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Player camera is already a child of the player, but we want it to act like it's lerping its position towards the player instead
public class CameraFollow : MonoBehaviour {
	bool shouldFollow = true;
	float lerpSpeed = 15;
	public Vector3 relativeStartPosition;
	public Vector3 relativePositionLastFrame;	// Used in restoring position of camera after jump-cut movement of player
	public Vector3 worldPositionLastFrame;

	private void Start() {
		relativeStartPosition = transform.localPosition;
		worldPositionLastFrame = transform.position;
		TeleportEnter.OnAnyTeleportSimple += ResetPosition;
		Player.instance.look.OnViewLockBegin += HandleViewLockBegin;
		Player.instance.look.OnViewUnlockEnd += HandleViewUnlockEnd;
	}
	void LateUpdate() {
		if (!shouldFollow) return;

		Vector3 destination = transform.parent.TransformPoint(relativeStartPosition);
		transform.position = Vector3.Lerp(worldPositionLastFrame, destination, lerpSpeed * Time.deltaTime);
		Debug.DrawRay(worldPositionLastFrame, transform.position - worldPositionLastFrame, Color.magenta);
		worldPositionLastFrame = transform.position;
		relativePositionLastFrame = transform.localPosition;
    }

	public void ResetPosition() {
		worldPositionLastFrame = transform.parent.TransformPoint(relativePositionLastFrame);
	}

	void HandleViewLockBegin() {
		shouldFollow = false;
	}

	void HandleViewUnlockEnd() {
		shouldFollow = true;
		ResetPosition();
	}
}
