using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class PlayerLook : Singleton<PlayerLook> {
	public bool DEBUG = false;
	DebugLogger debug;
    Transform playerTransform;
    public Transform cameraTransform;
	Vector3 cameraInitialLocalPos;
	[Range(0.01f,1)]
	public float generalSensitivity = 0.5f;
	[Range(0.01f,1)]
	public float sensitivityX = 0.5f;
	[Range(0.01f,1)]
	public float sensitivityY = 0.5f;
    public float rotationY = 0F;
	private float yClamp = 85;

	private const int lookAmountMultiplier = 14;

	public float outsideMultiplier = 1f;

	public ViewLockObject viewLockedObject;

	/// <summary>
	/// Returns the rotationY normalized to the range (-1, 1)
	/// </summary>
	public float normalizedY {
		get {
			return rotationY / yClamp;
		}
	}

    // Use this for initialization
    void Start () {
		debug = new DebugLogger(gameObject, DEBUG);

        playerTransform = gameObject.transform;
        cameraTransform = playerTransform.GetChild(0);
		cameraInitialLocalPos = cameraTransform.localPosition;

		//print(cameraTransform.rotation.x + ", " + cameraTransform.rotation.y + ", " + cameraTransform.rotation.z + ", " + cameraTransform.rotation.w);
		//print(cameraTransform.position.x + ", " + cameraTransform.position.y + ", " + cameraTransform.position.z);

		if (!Application.isEditor) {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	void Update() {
		if (viewLockedObject == null) {
			Look(PlayerButtonInput.instance.RightStick);

			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)) {
				Cursor.lockState = (Cursor.lockState != CursorLockMode.Locked) ? CursorLockMode.Locked : CursorLockMode.None;
				Cursor.visible = !Cursor.visible;
			}
		}
		else {
			MoveCursor(PlayerButtonInput.instance.RightStick);
		}
	}

	private void Look(Vector2 lookDirection) {
		LookHorizontal(lookDirection.x * lookAmountMultiplier * generalSensitivity * sensitivityX * outsideMultiplier);
		rotationY += lookDirection.y * lookAmountMultiplier * generalSensitivity * sensitivityY * outsideMultiplier;
		rotationY = Mathf.Clamp(rotationY, -yClamp, yClamp);
		LookVertical(rotationY);
	}

    private void LookVertical(float rotation) {
        cameraTransform.localEulerAngles = new Vector3(-rotation, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
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

	void MoveCursor(Vector2 direction) {
		Vector2 movement = direction * Time.deltaTime;
		movement.y *= (float)EpitaphScreen.currentWidth / EpitaphScreen.currentHeight;
		movement += Reticle.instance.thisTransformPos;
		Reticle.instance.MoveReticle(movement);
	}

	public void SetViewLock(ViewLockObject lockObject, ViewLockInfo lockInfo) {
		if (viewLockedObject == null) {
			StartCoroutine(LockView(lockObject, lockInfo));
		}
	}

	public void UnlockView() {
		if (viewLockedObject != null) {
			StartCoroutine(UnlockViewCoroutine());
		}
	}

	bool inLockViewCoroutine = false;
	Quaternion rotationBeforeViewLock;
	IEnumerator LockView(ViewLockObject lockObject, ViewLockInfo lockInfo) {
		debug.Log("Locking view for " + lockObject.gameObject.name);
		viewLockedObject = lockObject;
		PlayerMovement.instance.StopMovement();
		Interact.instance.enabled = false;
		inLockViewCoroutine = true;

		Vector3 startPos = cameraTransform.position;
		Quaternion startRot = cameraTransform.rotation;
		Vector3 endPos = lockObject.transform.TransformPoint(lockInfo.camPosition);
		Quaternion endRot = lockObject.transform.rotation * Quaternion.Euler(lockInfo.camRotationEuler);
		rotationBeforeViewLock = startRot;

		float timeElapsed = 0;
		while (timeElapsed < lockObject.viewLockTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / lockObject.viewLockTime;

			//Quaternion desiredCameraRotationInSourceSpace = Quaternion.Inverse(lockInfo.transform.rotation) * lockInfo.desiredCamRotation;
			cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
			cameraTransform.rotation = Quaternion.Lerp(startRot, endRot, t*t);

			yield return null;
		}

		cameraTransform.position = endPos;
		cameraTransform.rotation = endRot;

		inLockViewCoroutine = false;
		lockObject.focusIsLocked = true;
		Interact.instance.enabled = true;
		debug.Log("Finished locking view for " + lockObject.gameObject.name);
	}

	bool inUnlockViewCoroutine = false;
	IEnumerator UnlockViewCoroutine() {
		inUnlockViewCoroutine = true;
		Interact.instance.enabled = false;
		debug.Log("Unlocking view");

		Vector3 startPos = cameraTransform.position;
		Quaternion startRot = cameraTransform.rotation;
		Quaternion endRot = rotationBeforeViewLock;

		Vector2 reticleStartPos = Reticle.instance.thisTransformPos;
		Vector2 reticleEndPos = Vector2.one / 2f;

		float timeElapsed = 0;
		while (timeElapsed < viewLockedObject.viewUnlockTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / viewLockedObject.viewUnlockTime;

			cameraTransform.position = Vector3.Lerp(startPos, playerTransform.TransformPoint(cameraInitialLocalPos), t);
			cameraTransform.rotation = Quaternion.Lerp(startRot, endRot, t*t);

			Reticle.instance.MoveReticle(Vector2.Lerp(reticleStartPos, reticleEndPos, t));

			yield return null;
		}

		Reticle.instance.MoveReticle(reticleEndPos);

		viewLockedObject.hitbox.enabled = true;
		viewLockedObject = null;
		inUnlockViewCoroutine = false;
		PlayerMovement.instance.ResumeMovement();
		Interact.instance.enabled = true;
		debug.Log("Finished unlocking view");
	}
}
