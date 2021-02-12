using System;
using EpitaphUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

public class PlayerLook : SingletonSaveableObject<PlayerLook, PlayerLook.PlayerLookSave> {
    public delegate void ViewLockAction();

    public enum State {
        ViewUnlocked,
        ViewLocking,
        ViewLocked,
        ViewUnlocking
    }

    const int lookAmountMultiplier = 14;

    // Previously Coroutine local variables
    public Quaternion rotationBeforeViewLock;
    public Transform cameraContainerTransform;
    public Vector3 cameraInitialLocalPos;

    [Range(0.01f, 1)]
    public float generalSensitivity = 0.3f;

    [Range(0.01f, 1)]
    public float sensitivityX = 0.5f;

    [Range(0.01f, 1)]
    public float sensitivityY = 0.5f;

    public float rotationY;
    public float yClamp = 85;

    public float outsideMultiplier = 1f;
    State _state;

    Vector3 endPos;
    Quaternion endRot;
    Transform playerTransform;
    Vector2 reticleEndPos;
    Vector2 reticleStartPos;
    Vector3 startPos;
    Quaternion startRot;
    float timeSinceStateChange;
    float viewLockTime;
    float viewUnlockTime;

    public State state {
        get => _state;
        set {
            timeSinceStateChange = 0f;
            _state = value;
        }
    }

    public bool frozen => MainCanvas.instance.tempMenu.menuIsOpen;

    /// <summary>
    ///     Returns the rotationY normalized to the range (-1, 1)
    /// </summary>
    public float normalizedY => rotationY / yClamp;

    protected override void Awake() {
        base.Awake();
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
    void Start() {
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

        if (state == State.ViewLocked && PlayerButtonInput.instance.LeftStickHeld) UnlockView();
    }

    void OnApplicationFocus(bool focus) {
#if !UNITY_EDITOR
		Cursor.lockState = focus ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !focus;
#endif
    }

    public event ViewLockAction OnViewLockEnterBegin;
    public event ViewLockAction OnViewLockEnterFinish;
    public event ViewLockAction OnViewLockExitBegin;
    public event ViewLockAction OnViewLockExitFinish;


    void UpdateUnlockedView() {
        Look(PlayerButtonInput.instance.RightStick);

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)) {
            Cursor.lockState = Cursor.lockState != CursorLockMode.Locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Cursor.visible;
        }
    }

    void UpdateLockedView() {
        Vector2 moveDirection =
            Vector2.Scale(PlayerButtonInput.instance.RightStick, new Vector2(sensitivityX, sensitivityY)) * (generalSensitivity * lookAmountMultiplier);
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

                cameraContainerTransform.position = Vector3.Lerp(
                    startPos,
                    playerTransform.TransformPoint(cameraInitialLocalPos),
                    t
                );
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

    void Look(Vector2 lookDirection) {
        LookHorizontal(lookDirection.x * lookAmountMultiplier * generalSensitivity * sensitivityX * outsideMultiplier);
        float diffY = lookDirection.y * lookAmountMultiplier * generalSensitivity * sensitivityY * outsideMultiplier;
        // If we've been set to above the yClamp by something else, only allow movement back towards the clamp window
        if (Mathf.Abs(rotationY) > yClamp)
            rotationY = Mathf.Sign(rotationY) * Mathf.Min(Mathf.Abs(rotationY + diffY), Mathf.Abs(rotationY));
        else {
            rotationY += diffY;
            rotationY = Mathf.Clamp(rotationY, -yClamp, yClamp);
        }

        LookVertical(rotationY);
    }

    void LookVertical(float rotation) {
        cameraContainerTransform.localEulerAngles = new Vector3(
            -rotation,
            cameraContainerTransform.localEulerAngles.y,
            cameraContainerTransform.localEulerAngles.z
        );
    }

    void LookHorizontal(float rotation) {
        playerTransform.Rotate(new Vector3(0, rotation, 0));
    }

    // TODO: Make this work with a controller too, not just a mouse pointer
    void MoveCursor(Vector2 direction) {
        Reticle.instance.MoveReticle(
            new Vector2(
                Input.mousePosition.x / EpitaphScreen.currentWidth,
                Input.mousePosition.y / EpitaphScreen.currentHeight
            )
        );
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
    // There's only one PlayerLook so we don't need a UniqueId here
    public override string ID => "PlayerLook";

    [Serializable]
    public class PlayerLookSave : SerializableSaveObject<PlayerLook> {
        SerializableVector3 cameraLocalPosition;
        SerializableQuaternion cameraLocalRotation;
        bool cursorVisible;
        SerializableVector3 endPos;
        SerializableQuaternion endRot;

        float generalSensitivity;

        int lockState;
        float outsideMultiplier;
        SerializableVector2 reticleEndPos;

        SerializableVector2 reticleStartPos;

        // Previously Coroutine local variables
        SerializableQuaternion rotationBeforeViewLock;
        float rotationY;
        float sensitivityX;
        float sensitivityY;
        SerializableVector3 startPos;
        SerializableQuaternion startRot;

        int state;
        float timeSinceStateChange;
        float viewLockTime;
        float viewUnlockTime;
        float yClamp;

        public PlayerLookSave(PlayerLook playerLook) {
            cameraLocalPosition = playerLook.cameraContainerTransform.localPosition;
            cameraLocalRotation = playerLook.cameraContainerTransform.localRotation;

            state = (int) playerLook.state;
            timeSinceStateChange = playerLook.timeSinceStateChange;
            rotationBeforeViewLock = playerLook.rotationBeforeViewLock;
            startPos = playerLook.startPos;
            startRot = playerLook.startRot;
            endPos = playerLook.endPos;
            endRot = playerLook.endRot;
            reticleStartPos = playerLook.reticleStartPos;
            reticleEndPos = playerLook.reticleEndPos;
            viewLockTime = playerLook.viewLockTime;
            viewUnlockTime = playerLook.viewUnlockTime;

            generalSensitivity = playerLook.generalSensitivity;
            sensitivityX = playerLook.sensitivityX;
            sensitivityY = playerLook.sensitivityY;
            rotationY = playerLook.rotationY;
            yClamp = playerLook.yClamp;
            outsideMultiplier = playerLook.outsideMultiplier;

            if (TempMenu.instance.menuIsOpen) {
                lockState = (int) TempMenu.instance.cachedLockMode;
                cursorVisible = false;
            }
            else {
                lockState = (int) Cursor.lockState;
                cursorVisible = Cursor.visible;
            }
        }

        public override void LoadSave(PlayerLook playerLook) {
            playerLook.cameraContainerTransform.localPosition = cameraLocalPosition;
            playerLook.cameraContainerTransform.localRotation = cameraLocalRotation;

            playerLook.state = (State) state;
            playerLook.timeSinceStateChange = timeSinceStateChange;
            playerLook.rotationBeforeViewLock = rotationBeforeViewLock;
            playerLook.startPos = startPos;
            playerLook.startRot = startRot;
            playerLook.endPos = endPos;
            playerLook.endRot = endRot;
            playerLook.reticleStartPos = reticleStartPos;
            playerLook.reticleEndPos = reticleEndPos;
            playerLook.viewLockTime = viewLockTime;
            playerLook.viewUnlockTime = viewUnlockTime;

            playerLook.generalSensitivity = generalSensitivity;
            playerLook.sensitivityX = sensitivityX;
            playerLook.sensitivityY = sensitivityY;
            playerLook.rotationY = rotationY;
            playerLook.yClamp = yClamp;
            playerLook.outsideMultiplier = outsideMultiplier;
            
            Cursor.lockState = (CursorLockMode) lockState;
            Cursor.visible = cursorVisible;
        }
    }
#endregion
}