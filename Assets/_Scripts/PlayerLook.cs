using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using UnityEditor;
using Saving;
using System;
using SerializableClasses;

public class PlayerLook : Singleton<PlayerLook>, SaveableObject {
	public bool DEBUG = false;
	public enum State {
		ViewUnlocked,
		ViewLocking,
		ViewLocked,
		ViewUnlocking
	}
	private State _state;
	public State state {
		get {
			return _state;
		}
		set {
			timeSinceStateChange = 0f;
			_state = value;
		}
	}
	float timeSinceStateChange = 0f;
	// Previously Coroutine local variables
	public Quaternion rotationBeforeViewLock;
	Vector3 startPos;
	Quaternion startRot;
	Vector3 endPos;
	Quaternion endRot;
	Vector2 reticleStartPos;
	Vector2 reticleEndPos;
	float viewLockTime;
	float viewUnlockTime;

	DebugLogger debug;
	Transform playerTransform;
    public Transform cameraContainerTransform;
	public Vector3 cameraInitialLocalPos;
	[Range(0.01f,1)]
	public float generalSensitivity = 0.3f;
	[Range(0.01f,1)]
	public float sensitivityX = 0.5f;
	[Range(0.01f,1)]
	public float sensitivityY = 0.5f;
    public float rotationY = 0F;
	private float yClamp = 85;

	private const int lookAmountMultiplier = 14;

	public float outsideMultiplier = 1f;

	public delegate void ViewLockAction();
	public event ViewLockAction OnViewLockEnterBegin;
	public event ViewLockAction OnViewLockEnterFinish;
	public event ViewLockAction OnViewLockExitBegin;
	public event ViewLockAction OnViewLockExitFinish;

	public bool frozen => MainCanvas.instance.tempMenu.menuIsOpen;

	/// <summary>
	/// Returns the rotationY normalized to the range (-1, 1)
	/// </summary>
	public float normalizedY {
		get {
			return rotationY / yClamp;
		}
	}

	private void Awake() {
		playerTransform = gameObject.transform;
		cameraContainerTransform = playerTransform.GetChild(0);
		cameraInitialLocalPos = cameraContainerTransform.localPosition;

		if (!Application.isEditor
#if UNITY_EDITOR
			|| GameWindow.instance.maximizeOnPlay
#endif
			) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	// Use this for initialization
	void Start () {
		debug = new DebugLogger(this, () => DEBUG);

		//print(cameraTransform.rotation.x + ", " + cameraTransform.rotation.y + ", " + cameraTransform.rotation.z + ", " + cameraTransform.rotation.w);
		//print(cameraTransform.position.x + ", " + cameraTransform.position.y + ", " + cameraTransform.position.z);
	}

	void Update() {
		if (frozen) return;

		timeSinceStateChange += Time.deltaTime;

		switch (state) {
			case State.ViewUnlocked:
				UpdateUnlockedView();
				break;
			case State.ViewLocked:
				UpdateLockedView();
				break;
			case State.ViewUnlocking:
				UpdateUnlockingView();
				break;
			case State.ViewLocking:
				UpdateLockingView();
				break;
		}

		if (state == State.ViewLocked && PlayerButtonInput.instance.LeftStickHeld) {
			UnlockView();
		}
	}


	void UpdateUnlockedView() {
		Look(PlayerButtonInput.instance.RightStick);

		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)) {
			Cursor.lockState = (Cursor.lockState != CursorLockMode.Locked) ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !Cursor.visible;
		}
	}

	void UpdateLockedView() {
		Vector2 moveDirection = Vector2.Scale(PlayerButtonInput.instance.RightStick, new Vector2(sensitivityX, sensitivityY)) * generalSensitivity * lookAmountMultiplier;
		MoveCursor(moveDirection);
	}

	void InitializeUnlockingView() {
		Interact.instance.enabled = false;
		debug.Log("Unlocking view");
		OnViewLockExitBegin?.Invoke();

		startPos = cameraContainerTransform.position;
		startRot = cameraContainerTransform.rotation;

		reticleStartPos = Reticle.instance.thisTransformPos;
		reticleEndPos = Vector2.one / 2f;

		PlayerMovement.instance.thisRigidbody.isKinematic = false;
	}

	void UpdateUnlockingView() {
		// Skip one frame
		if (timeSinceStateChange > Time.deltaTime) {
			float timeElapsed = Mathf.Max(0, timeSinceStateChange - 2 * Time.deltaTime);
			if (timeElapsed < viewUnlockTime) {
				float t = timeElapsed / viewUnlockTime;

				cameraContainerTransform.position = Vector3.Lerp(startPos, playerTransform.TransformPoint(cameraInitialLocalPos), t);
				cameraContainerTransform.rotation = Quaternion.Lerp(startRot, rotationBeforeViewLock, t * t);

				Reticle.instance.MoveReticle(Vector2.Lerp(reticleStartPos, reticleEndPos, t));
			}
			else {
				Reticle.instance.MoveReticle(reticleEndPos);

				cameraContainerTransform.position = playerTransform.TransformPoint(cameraInitialLocalPos);
				cameraContainerTransform.rotation = rotationBeforeViewLock;

				PlayerMovement.instance.ResumeMovement();
				Player.instance.cameraFollow.enabled = true;
				Interact.instance.enabled = true;
				Cursor.lockState = CursorLockMode.Locked;
				OnViewLockExitFinish?.Invoke();

				debug.Log("Finished unlocking view");

				state = State.ViewUnlocked;
			}
		}
	}

	void InitializeLockingView(ViewLockObject lockObject, ViewLockInfo lockInfo) {
		debug.Log("Locking view for " + lockObject.gameObject.name);
		PlayerMovement.instance.StopMovement();
		Player.instance.cameraFollow.enabled = false;
		EpitaphScreen.instance.playerCamera.transform.localPosition = Vector3.zero;
		Interact.instance.enabled = false;
		OnViewLockEnterBegin?.Invoke();

		startPos = cameraContainerTransform.position;
		startRot = cameraContainerTransform.rotation;
		endPos = lockObject.transform.TransformPoint(lockInfo.camPosition);
		endRot = lockObject.transform.rotation * Quaternion.Euler(lockInfo.camRotationEuler);
		rotationBeforeViewLock = startRot;

		viewLockTime = lockObject.viewLockTime;
		viewUnlockTime = lockObject.viewUnlockTime;

		PlayerMovement.instance.thisRigidbody.isKinematic = true;
	}

	void UpdateLockingView() {
		if (timeSinceStateChange < viewLockTime) {
			float t = timeSinceStateChange / viewLockTime;

			cameraContainerTransform.position = Vector3.Lerp(startPos, endPos, t);
			cameraContainerTransform.rotation = Quaternion.Lerp(startRot, endRot, t * t);
		}
		else {
			debug.Log($"EndPos: {endPos:F3}");
			cameraContainerTransform.position = endPos;
			cameraContainerTransform.rotation = endRot;

			Interact.instance.enabled = true;
			//Going directly from Locked to Confined does not work
			Cursor.lockState = CursorLockMode.None;
			Cursor.lockState = CursorLockMode.Confined;

			// Debug line to look at differences for teleport pictures
			//yield return new WaitForSeconds(3f);

			OnViewLockEnterFinish?.Invoke();
			debug.Log("Finished locking view");

			state = State.ViewLocked;
		}
	}

	private void Look(Vector2 lookDirection) {
		LookHorizontal(lookDirection.x * lookAmountMultiplier * generalSensitivity * sensitivityX * outsideMultiplier);
		float diffY = lookDirection.y * lookAmountMultiplier * generalSensitivity * sensitivityY * outsideMultiplier;
		// If we've been set to above the yClamp by something else, only allow movement back towards the clamp window
		if (Mathf.Abs(rotationY) > yClamp) {
			rotationY = Mathf.Sign(rotationY) * Mathf.Min(Mathf.Abs(rotationY + diffY), Mathf.Abs(rotationY));
		}
		else {
			rotationY += diffY;
			rotationY = Mathf.Clamp(rotationY, -yClamp, yClamp);
		}

		LookVertical(rotationY);
	}

    private void LookVertical(float rotation) {
        cameraContainerTransform.localEulerAngles = new Vector3(-rotation, cameraContainerTransform.localEulerAngles.y, cameraContainerTransform.localEulerAngles.z);
    }

    private void LookHorizontal(float rotation) {
        playerTransform.Rotate(new Vector3(0, rotation, 0));
    }

	private void OnApplicationFocus(bool focus) {
#if !UNITY_EDITOR
		Cursor.lockState = focus ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !focus;
#endif
	}

	// TODO: Make this work with a controller too, not just a mouse pointer
	void MoveCursor(Vector2 direction) {
		Reticle.instance.MoveReticle(new Vector2(Input.mousePosition.x / EpitaphScreen.currentWidth, Input.mousePosition.y / EpitaphScreen.currentHeight));
	}

	public void SetViewLock(ViewLockObject lockObject, ViewLockInfo lockInfo) {
		if (state == State.ViewUnlocked) {
			state = State.ViewLocking;
			InitializeLockingView(lockObject, lockInfo);
		}
	}

	public void UnlockView() {
		if (state == State.ViewLocked) {
			state = State.ViewUnlocking;
			InitializeUnlockingView();
		}
	}


	#region Saving
	public bool SkipSave { get; set; }
	// There's only one PlayerLook so we don't need a UniqueId here
	public string ID => "PlayerLook";

	[Serializable]
	class PlayerLookSave {
		SerializableVector3 cameraLocalPosition;
		SerializableQuaternion cameraLocalRotation;

		int state;
		float timeSinceStateChange;
		// Previously Coroutine local variables
		SerializableQuaternion rotationBeforeViewLock;
		SerializableVector3 startPos;
		SerializableQuaternion startRot;
		SerializableVector3 endPos;
		SerializableQuaternion endRot;
		SerializableVector2 reticleStartPos;
		SerializableVector2 reticleEndPos;
		float viewLockTime;
		float viewUnlockTime;

		float generalSensitivity;
		float sensitivityX;
		float sensitivityY;
		float rotationY;
		float yClamp;
		float outsideMultiplier;

		int lockState;
		bool cursorVisible;

		public PlayerLookSave(PlayerLook playerLook) {
			this.cameraLocalPosition = playerLook.cameraContainerTransform.localPosition;
			this.cameraLocalRotation = playerLook.cameraContainerTransform.localRotation;

			this.state = (int)playerLook.state;
			this.timeSinceStateChange = playerLook.timeSinceStateChange;
			this.rotationBeforeViewLock = playerLook.rotationBeforeViewLock;
			this.startPos = playerLook.startPos;
			this.startRot = playerLook.startRot;
			this.endPos = playerLook.endPos;
			this.endRot = playerLook.endRot;
			this.reticleStartPos = playerLook.reticleStartPos;
			this.reticleEndPos = playerLook.reticleEndPos;
			this.viewLockTime = playerLook.viewLockTime;
			this.viewUnlockTime = playerLook.viewUnlockTime;

			this.generalSensitivity = playerLook.generalSensitivity;
			this.sensitivityX = playerLook.sensitivityX;
			this.sensitivityY = playerLook.sensitivityY;
			this.rotationY = playerLook.rotationY;
			this.yClamp = playerLook.yClamp;
			this.outsideMultiplier = playerLook.outsideMultiplier;

			this.lockState = (int)Cursor.lockState;
			this.cursorVisible = Cursor.visible;
		}

		public void LoadSave(PlayerLook playerLook) {
			playerLook.cameraContainerTransform.localPosition = this.cameraLocalPosition;
			playerLook.cameraContainerTransform.localRotation = this.cameraLocalRotation;

			playerLook.state = (State)this.state;
			playerLook.timeSinceStateChange = this.timeSinceStateChange;
			playerLook.rotationBeforeViewLock = this.rotationBeforeViewLock;
			playerLook.startPos = this.startPos;
			playerLook.startRot = this.startRot;
			playerLook.endPos = this.endPos;
			playerLook.endRot = this.endRot;
			playerLook.reticleStartPos = this.reticleStartPos;
			playerLook.reticleEndPos = this.reticleEndPos;
			playerLook.viewLockTime = this.viewLockTime;
			playerLook.viewUnlockTime = this.viewUnlockTime;

			playerLook.generalSensitivity = this.generalSensitivity;
			playerLook.sensitivityX = this.sensitivityX;
			playerLook.sensitivityY = this.sensitivityY;
			playerLook.rotationY = this.rotationY;
			playerLook.yClamp = this.yClamp;
			playerLook.outsideMultiplier = this.outsideMultiplier;

			Cursor.lockState = (CursorLockMode)this.lockState;
			Cursor.visible = this.cursorVisible;
		}
	}

	public object GetSaveObject() {
		return new PlayerLookSave(this); ;
	}

	public void LoadFromSavedObject(object savedObject) {
		PlayerLookSave save = savedObject as PlayerLookSave;

		save.LoadSave(this);
	}
	#endregion
}
