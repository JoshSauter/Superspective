﻿using System;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

public class PlayerLook : SingletonSaveableObject<PlayerLook, PlayerLook.PlayerLookSave> {
    public delegate void ViewLockAction();

    public enum ViewLockState {
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

    public float rotationY;
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

    public bool frozen => NovaPauseMenu.instance.PauseMenuIsOpen || ((int)EndOfPlaytestMessage.instance.state > (int)EndOfPlaytestMessage.State.BackgroundFadingIn);

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

    void Update() {
        if (frozen || !GameManager.instance.gameHasLoaded) return;

        if (state == ViewLockState.ViewLocked && GameManager.instance.IsCurrentlyLoading) return;

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

        if (state == ViewLockState.ViewLocked && PlayerButtonInput.instance.LeftStickHeld) UnlockView();
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
    // There's only one PlayerLook so we don't need a UniqueId here
    public override string ID => "PlayerLook";

    [Serializable]
    public class PlayerLookSave : SerializableSaveObject<PlayerLook> {
        SerializableVector3 cameraLocalPosition;
        SerializableQuaternion cameraLocalRotation;
        bool cursorVisible;
        SerializableVector3 endPos;
        SerializableQuaternion endRot;

        int lockState;
        float outsideMultiplier;
        SerializableVector2 reticleEndPos;

        SerializableVector2 reticleStartPos;

        // Previously Coroutine local variables
        SerializableQuaternion rotationBeforeViewLock;
        float rotationY;
        SerializableVector3 startPos;
        SerializableQuaternion startRot;

        int state;
        float timeSinceStateChange;
        float viewLockTime;
        float viewUnlockTime;
        float yClamp;

        public PlayerLookSave(PlayerLook playerLook) : base(playerLook) {
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

            rotationY = playerLook.rotationY;
            yClamp = playerLook.yClamp;
            outsideMultiplier = playerLook.outsideMultiplier;

            if (NovaPauseMenu.instance.PauseMenuIsOpen) {
                lockState = (int) NovaPauseMenu.instance.cachedLockMode;
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

            playerLook.state = (ViewLockState) state;
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

            playerLook.rotationY = rotationY;
            playerLook.yClamp = yClamp;
            playerLook.outsideMultiplier = outsideMultiplier;
            
            Cursor.lockState = (CursorLockMode) lockState;
            Cursor.visible = cursorVisible;
        }
    }
#endregion
}