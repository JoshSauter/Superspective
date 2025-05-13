using System;
using NovaMenuUI;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerLook : SingletonSuperspectiveObject<PlayerLook, PlayerLook.PlayerLookSave> {
    public delegate void ViewLockAction();

    public enum ViewLockState : byte {
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

    public float generalSensitivity => 1.5f * Settings.Gameplay.GeneralSensitivity / 100f;

    public float sensitivityX => 2f * Settings.Gameplay.XSensitivity / 100f;

    public float sensitivityY => 2f * Settings.Gameplay.YSensitivity / 100f;

    private float rotationY;
    [ShowInInspector]
    public float RotationY {
        get => rotationY;
        set {
            float clampedValue = Mathf.Clamp(value, -yClamp, yClamp);
            rotationY = clampedValue;
            LookVertical(rotationY);
        }
    }
    // For bypassing the yClamp
    public void ForceSetRotationY(float value) {
        rotationY = value;
        LookVertical(rotationY);
    }

    public float yClamp = 85;

    public float outsideMultiplier = 1f;
    ViewLockState _state;

    Vector3 endPos;
    Quaternion endRot;
    Transform playerTransform;
    Vector2 reticleEndPos;
    Vector2 reticleStartPos;
    Vector3 startPos;
    Quaternion startRot;
    public float timeSinceStateChange;
    float viewLockTime;
    float viewUnlockTime;
    bool cursorIsStationary;

    public ViewLockState state {
        get => _state;
        set {
            timeSinceStateChange = 0f;
            _state = value;
        }
    }

    private bool _frozenOverride;

    public bool Frozen {
        get => _frozenOverride || NovaPauseMenu.instance.PauseMenuIsOpen || ((int)EndOfPlaytestMessage.instance.state > (int)EndOfPlaytestMessage.State.BackgroundFadingIn) || GameManager.instance.justResumed;
        set => _frozenOverride = value;
    } 

    /// <summary>
    ///     Returns the rotationY normalized to the range (-1, 1)
    /// </summary>
    public float NormalizedY => rotationY / yClamp;

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

    void Update() {
        if (Frozen || !GameManager.instance.gameHasLoaded || GameManager.instance.IsCurrentlyPaused) return;

        if (state != ViewLockState.ViewUnlocked && GameManager.instance.IsCurrentlyLoading) return;

        timeSinceStateChange += Time.deltaTime;

        switch (state) {
            case ViewLockState.ViewUnlocked:
                UpdateUnlockedView();
                break;
            case ViewLockState.ViewLocked:
                UpdateLockedView();
                break;
            case ViewLockState.ViewUnlocking:
                UpdateUnlockingView();
                break;
            case ViewLockState.ViewLocking:
                UpdateLockingView();
                break;
        }

        if (state == ViewLockState.ViewLocked) {
            if ((PlayerButtonInput.instance.ExitLockedViewPressed || PlayerButtonInput.instance.LeftStickHeld) && !GameManager.instance.IsCurrentlyLoading) {
                UnlockView();
            }
        }
        
        Vector3 normalizedAngles = cameraContainerTransform.localEulerAngles;
        normalizedAngles.x = (normalizedAngles.x > 180) ? normalizedAngles.x - 360 : normalizedAngles.x;
        normalizedAngles.y = (normalizedAngles.y > 180) ? normalizedAngles.y - 360 : normalizedAngles.y;
        normalizedAngles.z = (normalizedAngles.z > 180) ? normalizedAngles.z - 360 : normalizedAngles.z;
        if (Mathf.Approximately(Mathf.Abs(normalizedAngles.y), 180)) {
            Debug.LogError($"My life just got flip-turned upside down!");
        }
    }

    void OnApplicationFocus(bool focus) {
        SetCursorLockState(focus);
    }

    void SetCursorLockState(bool focus) {
#if !UNITY_EDITOR
        CursorLockMode lockedState = NovaPauseMenu.instance.PauseMenuIsOpen ? CursorLockMode.Confined : CursorLockMode.Locked;
		Cursor.lockState = focus ? lockedState : CursorLockMode.None;
		Cursor.visible = !focus || NovaPauseMenu.instance.PauseMenuIsOpen;
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
        if (cursorIsStationary) {
            if (moveDirection.magnitude > 0.5f) {
                UnlockView();
            }
        }
        else {
            MoveCursor(moveDirection);
        }
    }

    void InitializeUnlockingView() {
        Interact.instance.enabled = false;
        debug.Log("Unlocking view");
        OnViewLockExitBegin?.Invoke();

        startPos = cameraContainerTransform.position;
        startRot = cameraContainerTransform.rotation;

        cursorIsStationary = false;

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

                Player.instance.cameraFollow.enabled = true;
                PlayerMovement.instance.pauseSnapToGround = false;
                Interact.instance.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                OnViewLockExitFinish?.Invoke();

                debug.Log("Finished unlocking view");

                state = ViewLockState.ViewUnlocked;
            }
        }
    }

    void InitializeLockingView(ViewLockObject lockObject, ViewLockInfo lockInfo) {
        debug.Log("Locking view for " + lockObject.gameObject.name);
        PlayerMovement.instance.StopMovement();
        PlayerMovement.instance.pauseSnapToGround = true;
        Player.instance.cameraFollow.enabled = false;
        SuperspectiveScreen.instance.playerCamera.transform.localPosition = Vector3.zero;
        Interact.instance.enabled = false;
        OnViewLockEnterBegin?.Invoke();

        startPos = cameraContainerTransform.position;
        startRot = cameraContainerTransform.rotation;
        endPos = lockObject.transform.TransformPoint(lockInfo.camPosition);
        endRot = lockObject.transform.rotation * Quaternion.Euler(lockInfo.camRotationEuler);
        rotationBeforeViewLock = startRot;

        viewLockTime = lockObject.viewLockTime;
        viewUnlockTime = lockObject.viewUnlockTime;
        cursorIsStationary = lockObject.cursorIsStationaryOnLock;

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
            if (!cursorIsStationary) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.lockState = CursorLockMode.Confined;
            }

            // Debug line to look at differences for teleport pictures
            //yield return new WaitForSeconds(3f);

            OnViewLockEnterFinish?.Invoke();
            debug.Log("Finished locking view");

            state = ViewLockState.ViewLocked;
        }
    }

    void Look(Vector2 lookDirection) {
        LookHorizontal(lookDirection.x * lookAmountMultiplier * generalSensitivity * sensitivityX * outsideMultiplier);
        float diffY = lookDirection.y * lookAmountMultiplier * generalSensitivity * sensitivityY * outsideMultiplier;
        
        debug.Log($"Diff Y: {diffY:F3}. Rotation Y Before: {rotationY:F3}");
        // If we've been set to above the yClamp by something else, only allow movement back towards the clamp window
        if (Mathf.Abs(rotationY) > yClamp)
            rotationY = Mathf.Sign(rotationY) * Mathf.Min(Mathf.Abs(rotationY + diffY), Mathf.Abs(rotationY));
        else {
            rotationY += diffY;
            rotationY = Mathf.Clamp(rotationY, -yClamp, yClamp);
        }
        debug.Log($"Rotation Y After: {rotationY:F3}");

        LookVertical(rotationY);
    }

    void LookVertical(float rotation) {
        float currentY = cameraContainerTransform.localEulerAngles.y;
        float currentZ = cameraContainerTransform.localEulerAngles.z;

        Quaternion newRotation = Quaternion.Euler(-rotation, currentY, currentZ);
        cameraContainerTransform.localRotation = newRotation;
    }

    void LookHorizontal(float rotation) {
        playerTransform.Rotate(new Vector3(0, rotation, 0));
    }

    // TODO: Make this work with a controller too, not just a mouse pointer
    void MoveCursor(Vector2 direction) {
        Reticle.instance.MoveReticle(
            new Vector2(
                Input.mousePosition.x / SuperspectiveScreen.currentWidth,
                Input.mousePosition.y / SuperspectiveScreen.currentHeight
            )
        );
    }

    public void SetViewLock(ViewLockObject lockObject, ViewLockInfo lockInfo) {
        if (state == ViewLockState.ViewUnlocked) {
            state = ViewLockState.ViewLocking;
            InitializeLockingView(lockObject, lockInfo);
        }
    }

    public void UnlockView() {
        if (state == ViewLockState.ViewLocked) {
            state = ViewLockState.ViewUnlocking;
            InitializeUnlockingView();
        }
    }


#region Saving

    public override void LoadSave(PlayerLookSave save) {
        cameraContainerTransform.localPosition = save.cameraLocalPosition;
        cameraContainerTransform.localRotation = save.cameraLocalRotation;
            
        Cursor.lockState = save.cursorLockState;
        Cursor.visible = save.cursorVisible;

        Frozen = save.frozenOverride;
    }

    [Serializable]
    public class PlayerLookSave : SaveObject<PlayerLook> {
        public SerializableQuaternion cameraLocalRotation;
        public SerializableVector3 cameraLocalPosition;
        public CursorLockMode cursorLockState;
        public bool cursorVisible;
        public bool frozenOverride;

        public PlayerLookSave(PlayerLook playerLook) : base(playerLook) {
            cameraLocalPosition = playerLook.cameraContainerTransform.localPosition;
            cameraLocalRotation = playerLook.cameraContainerTransform.localRotation;

            if (NovaPauseMenu.instance.PauseMenuIsOpen) {
                cursorLockState = NovaPauseMenu.instance.cachedLockMode;
                cursorVisible = false;
            }
            else {
                cursorLockState = Cursor.lockState;
                cursorVisible = Cursor.visible;
            }

            frozenOverride = playerLook._frozenOverride;
        }
    }
#endregion
}