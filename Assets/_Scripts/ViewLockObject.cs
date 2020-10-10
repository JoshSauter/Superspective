using Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ViewLockInfo {
	public Vector3 camPosition;
	public Vector3 camRotationEuler;
}

[RequireComponent(typeof(Collider))]
public class ViewLockObject : MonoBehaviour {
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

	SoundEffect enterViewLockSfx;

	void Awake() {
		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

		InitAudio();
	}

	void InitAudio() {
		if (enterViewLockSfx == null) {
			enterViewLockSfx = gameObject.AddComponent<SoundEffectOnGameObject>();
			enterViewLockSfx.pitch = 0.5f;
			enterViewLockSfx.audioSource.clip = Resources.Load<AudioClip>("Audio/Sounds/Objects/ViewLockObject");
		}
	}

    void Start() {
		hitbox = GetComponent<Collider>();
		hitbox.isTrigger = true;

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
			enterViewLockSfx.Play(true);
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
}
