using System;
using Audio;
using Saving;
using UnityEngine;

[Serializable]
public struct ViewLockInfo {
    public Vector3 camPosition;
    public Vector3 camRotationEuler;
}

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(Collider))]
public class ViewLockObject : SaveableObject<ViewLockObject, ViewLockObject.ViewLockObjectSave>, AudioJobOnGameObject {
    public delegate void ViewLockEvent();

    public ViewLockInfo[] viewLockOptions;
    public bool cursorIsStationaryOnLock = false;
    public float viewLockTime = 0.75f;
    public float viewUnlockTime = 0.25f;

    public Collider hitbox;

    PlayerLook.ViewLockState _state;
    public InteractableObject interactableObject;
    public ViewLockEvent OnViewLockEnterBegin;
    public ViewLockEvent OnViewLockEnterFinish;
    public ViewLockEvent OnViewLockExitBegin;
    public ViewLockEvent OnViewLockExitFinish;
    Transform playerCamera;

    public PlayerLook.ViewLockState state {
        get => _state;
        set {
            if (state == value) return;

            _state = value;
            switch (value) {
                case PlayerLook.ViewLockState.ViewLocking:
                    OnViewLockEnterBegin?.Invoke();
                    break;
                case PlayerLook.ViewLockState.ViewLocked:
                    OnViewLockEnterFinish?.Invoke();
                    break;
                case PlayerLook.ViewLockState.ViewUnlocking:
                    OnViewLockExitBegin?.Invoke();
                    break;
                case PlayerLook.ViewLockState.ViewUnlocked:
                    OnViewLockExitFinish?.Invoke();

                    hitbox.enabled = true;
                    break;
            }
        }
    }

    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    bool isLockedOnThisObject => state != PlayerLook.ViewLockState.ViewUnlocked;

    protected override void Awake() {
        base.Awake();
        hitbox = GetComponent<Collider>();
        hitbox.isTrigger = true;

        interactableObject = GetComponent<InteractableObject>();
        if (interactableObject == null) interactableObject = gameObject.AddComponent<InteractableObject>();
        interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
    }

    protected override void Start() {
        base.Start();
        playerCamera = SuperspectiveScreen.instance.playerCamera.transform;
    }

    void Update() {
        if (isLockedOnThisObject) state = PlayerLook.instance.state;
    }

    public void OnLeftMouseButtonDown() {
        if (PlayerLook.instance.state == PlayerLook.ViewLockState.ViewUnlocked) {
            hitbox.enabled = false;
            AudioManager.instance.Play(AudioName.ViewLockObject, ID, this);
            PlayerLook.instance.SetViewLock(this, ClosestViewLock(playerCamera.position, playerCamera.rotation));
            state = PlayerLook.ViewLockState.ViewLocking;
        }
    }

    ViewLockInfo ClosestViewLock(Vector3 pos, Quaternion rot) {
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

            if (angleBetween < minRotationAngle) minRotationAngle = angleBetween;
        }

        return viewLockOptions[indexOfWinner];
    }

#region Saving
    public override bool SkipSave {
        get => hitbox == null;
        set { }
    }

    [Serializable]
    public class ViewLockObjectSave : SerializableSaveObject<ViewLockObject> {
        bool colliderEnabled;
        int state;

        public ViewLockObjectSave(ViewLockObject viewLockObject) : base(viewLockObject) {
            state = (int) viewLockObject.state;
            colliderEnabled = viewLockObject.hitbox.enabled;
        }

        public override void LoadSave(ViewLockObject viewLockObject) {
            viewLockObject._state = (PlayerLook.ViewLockState) state;
            viewLockObject.hitbox.enabled = colliderEnabled;
        }
    }
#endregion
}