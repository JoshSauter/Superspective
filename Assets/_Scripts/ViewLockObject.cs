using Audio;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ViewLockInfo {
	public Vector3 camPosition;
	public Vector3 camRotationEuler;
}

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(Collider))]
public class ViewLockObject : MonoBehaviour, SaveableObject {
	UniqueId _id;
	public UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	PlayerLook.State _state;
	PlayerLook.State state {
		get { return _state; }
		set {
			if (state == value) return;

			_state = value;
			switch (value) {
				case PlayerLook.State.ViewLocking:
					OnViewLockEnterBegin?.Invoke();
					break;
				case PlayerLook.State.ViewLocked:
					OnViewLockEnterFinish?.Invoke();
					break;
				case PlayerLook.State.ViewUnlocking:
					OnViewLockExitBegin?.Invoke();
					break;
				case PlayerLook.State.ViewUnlocked:
					OnViewLockExitFinish?.Invoke();

					hitbox.enabled = true;
					break;
			}
		}
	}
	bool isLockedOnThisObject => state != PlayerLook.State.ViewUnlocked;
	InteractableObject interactableObject;
	public ViewLockInfo[] viewLockOptions;
	public float viewLockTime = 0.75f;
	public float viewUnlockTime = 0.25f;

	public Collider hitbox;
	Transform playerCamera;

	public delegate void ViewLockEvent();
	public ViewLockEvent OnViewLockEnterBegin;
	public ViewLockEvent OnViewLockEnterFinish;
	public ViewLockEvent OnViewLockExitBegin;
	public ViewLockEvent OnViewLockExitFinish;

	void Awake() {
		hitbox = GetComponent<Collider>();
		hitbox.isTrigger = true;

		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
	}

    void Start() {
		playerCamera = EpitaphScreen.instance.playerCamera.transform;
    }

	private void Update() {
		if (isLockedOnThisObject) {
			state = PlayerLook.instance.state;
		}
	}

	public void OnLeftMouseButtonDown() {
		if (PlayerLook.instance.state == PlayerLook.State.ViewUnlocked) {
			hitbox.enabled = false;
			AudioManager.instance.PlayOnGameObject(AudioName.ViewLockObject, ID, gameObject, true);
			PlayerLook.instance.SetViewLock(this, ClosestViewLock(playerCamera.position, playerCamera.rotation));
			state = PlayerLook.State.ViewLocking;
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

	#region Saving
	public bool SkipSave { get { return hitbox == null; } set { } }

	public string ID => $"ViewLockObject_{id.uniqueId}";

	[Serializable]
	class ViewLockObjectSave {
		int state;
		bool colliderEnabled;

		public ViewLockObjectSave(ViewLockObject viewLockObject) {
			this.state = (int)viewLockObject.state;
			this.colliderEnabled = viewLockObject.hitbox.enabled;
		}

		public void LoadSave(ViewLockObject viewLockObject) {
			viewLockObject.state = (PlayerLook.State)this.state;
			viewLockObject.hitbox.enabled = this.colliderEnabled;
		}
	}

	public object GetSaveObject() {
		return new ViewLockObjectSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		ViewLockObjectSave save = savedObject as ViewLockObjectSave;

		save.LoadSave(this);
	}
	#endregion
}
