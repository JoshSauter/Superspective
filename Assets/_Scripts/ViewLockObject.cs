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
public class ViewLockObject : SaveableObject<ViewLockObject, ViewLockObject.ViewLockObjectSave> {
    public delegate void ViewLockEvent();

    public ViewLockInfo[] viewLockOptions;
    public float viewLockTime = 0.75f;
    public float viewUnlockTime = 0.25f;

    public Collider hitbox;
    UniqueId _id;

    PlayerLook.State _state;
    InteractableObject interactableObject;
    public ViewLockEvent OnViewLockEnterBegin;
    public ViewLockEvent OnViewLockEnterFinish;
    public ViewLockEvent OnViewLockExitBegin;
    public ViewLockEvent OnViewLockExitFinish;
    Transform playerCamera;

    public UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    PlayerLook.State state {
        get => _state;
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
        playerCamera = EpitaphScreen.instance.playerCamera.transform;
    }

    void Update() {
        if (isLockedOnThisObject) state = PlayerLook.instance.state;
    }

    public void OnLeftMouseButtonDown() {
        if (PlayerLook.instance.state == PlayerLook.State.ViewUnlocked) {
            hitbox.enabled = false;
            AudioManager.instance.PlayOnGameObject(AudioName.ViewLockObject, ID, gameObject, true);
            PlayerLook.instance.SetViewLock(this, ClosestViewLock(playerCamera.position, playerCamera.rotation));
            state = PlayerLook.State.ViewLocking;
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

    public override string ID => $"ViewLockObject_{id.uniqueId}";

    [Serializable]
    public class ViewLockObjectSave : SerializableSaveObject<ViewLockObject> {
        bool colliderEnabled;
        int state;

        public ViewLockObjectSave(ViewLockObject viewLockObject) {
            state = (int) viewLockObject.state;
            colliderEnabled = viewLockObject.hitbox.enabled;
        }

        public override void LoadSave(ViewLockObject viewLockObject) {
            viewLockObject._state = (PlayerLook.State) state;
            viewLockObject.hitbox.enabled = colliderEnabled;
        }
    }
#endregion
}