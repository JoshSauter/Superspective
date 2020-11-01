using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using PortalMechanics;
using NaughtyAttributes;
using Saving;
using System;
using SerializableClasses;

// Player camera is already a child of the player, but we want it to act like it's lerping its position towards the player instead
public class CameraFollow : MonoBehaviour, SaveableObject {
	bool shouldFollow = true;
	[SerializeField]
	[ReadOnly]
	float currentLerpSpeed = 450f;				// Can be set by external scripts to slow the camera's lerp speed for a short time
	public const float desiredLerpSpeed = 30f;	// currentLerpSpeed will approach this value after not being changed for a while
	public Vector3 relativeStartPosition;
	public Vector3 relativePositionLastFrame;	// Used in restoring position of camera after jump-cut movement of player
	public Vector3 worldPositionLastFrame;

	float timeSinceCurrentLerpSpeedWasModified = 0f;

	Headbob headbob;

	public delegate void CameraFollowUpdate(Vector3 offset, Vector3 positionDiffFromLastFrame);
	public event CameraFollowUpdate OnCameraFollowUpdate;

	// DEBUG:
	//float maxFollowDistance = 0f;

	public void SetLerpSpeed(float lerpSpeed) {
		currentLerpSpeed = lerpSpeed;
		timeSinceCurrentLerpSpeedWasModified = 0f;
	}

	private void Awake() {
		headbob = Player.instance.GetComponent<Headbob>();
		relativeStartPosition = transform.localPosition;
		worldPositionLastFrame = transform.position;
	}

	private void Start() {
		TeleportEnter.OnAnyTeleportSimple += RecalculateWorldPositionLastFrame;
		Portal.OnAnyPortalTeleportSimple += (obj) => { if (obj.TaggedAsPlayer()) RecalculateWorldPositionLastFrame(); };
		Player.instance.look.OnViewLockEnterBegin += HandleViewLockBegin;
		Player.instance.look.OnViewLockExitFinish += HandleViewUnlockEnd;
	}

	void FixedUpdate() {
		if (!shouldFollow) return;

		Vector3 destination = transform.parent.TransformPoint(relativeStartPosition);

		//float distanceBetweenCamAndPlayerBefore = Vector3.Distance(worldPositionLastFrame, destination);
		Vector3 nextPosition = Vector3.Lerp(worldPositionLastFrame, destination, currentLerpSpeed * Time.fixedDeltaTime);
		Vector3 offset = nextPosition - transform.position;
		Vector3 positionDiffFromLastFrame = nextPosition - worldPositionLastFrame;
		OnCameraFollowUpdate?.Invoke(offset, positionDiffFromLastFrame);
		transform.position = nextPosition;
		//float distanceBetweenCamAndPlayer = Vector3.Distance(transform.position, destination);

		//if (distanceBetweenCamAndPlayer > maxFollowDistance) {
		//	maxFollowDistance = distanceBetweenCamAndPlayer;
		//	Debug.LogWarning("New max follow distance: " + maxFollowDistance);
		//}

		Debug.DrawRay(worldPositionLastFrame, transform.position - worldPositionLastFrame, Color.magenta);

		worldPositionLastFrame = transform.position;
		relativePositionLastFrame = transform.localPosition;

		transform.position -= headbob.curBobAmount * -Player.instance.transform.up;

		if (timeSinceCurrentLerpSpeedWasModified > 0.5f) {
			currentLerpSpeed = Mathf.Lerp(currentLerpSpeed, desiredLerpSpeed, 0.25f * Time.fixedDeltaTime);
		}

		timeSinceCurrentLerpSpeedWasModified += Time.fixedDeltaTime;
    }

	// Restore the relative offset of worldPositionLastFrame after a jump-cut movement of the player
	public void RecalculateWorldPositionLastFrame() {
		worldPositionLastFrame = transform.parent.TransformPoint(relativePositionLastFrame);
	}

	void HandleViewLockBegin() {
		shouldFollow = false;
	}

	void HandleViewUnlockEnd() {
		shouldFollow = true;
		RecalculateWorldPositionLastFrame();
	}

	#region Saving
	public bool SkipSave { get; set; }
	// There's only one player so we don't need a UniqueId here
	public string ID => "CameraFollow";

	[Serializable]
	class CameraFollowSave {
		bool shouldFollow;
		float currentLerpSpeed;
		SerializableVector3 relativeStartPosition;
		SerializableVector3 relativePositionLastFrame;
		SerializableVector3 worldPositionLastFrame;

		float timeSinceCurrentLerpSpeedWasModified;

		public CameraFollowSave(CameraFollow cam) {
			this.shouldFollow = cam.shouldFollow;
			this.currentLerpSpeed = cam.currentLerpSpeed;
			this.relativeStartPosition = cam.relativeStartPosition;
			this.relativePositionLastFrame = cam.relativePositionLastFrame;
			this.worldPositionLastFrame = cam.worldPositionLastFrame;
			this.timeSinceCurrentLerpSpeedWasModified = cam.timeSinceCurrentLerpSpeedWasModified;
		}

		public void LoadSave(CameraFollow cam) {
			cam.shouldFollow = this.shouldFollow;
			cam.currentLerpSpeed = this.currentLerpSpeed;
			cam.relativeStartPosition = this.relativeStartPosition;
			cam.relativePositionLastFrame = this.relativePositionLastFrame;
			cam.worldPositionLastFrame = this.worldPositionLastFrame;
			cam.timeSinceCurrentLerpSpeedWasModified = this.timeSinceCurrentLerpSpeedWasModified;
		}
	}

	public object GetSaveObject() {
		return new CameraFollowSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		CameraFollowSave save = savedObject as CameraFollowSave;

		save.LoadSave(this);
	}
	#endregion
}
