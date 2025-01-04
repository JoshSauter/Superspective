using System;
using System.Collections.Generic;
using Audio;
using NaughtyAttributes;
using Saving;
using UnityEngine;

[Serializable]
public struct ViewLockInfo {
    public Vector3 camPosition;
    public Vector3 camRotationEuler;
}

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(Collider))]
public class ViewLockObject : SuperspectiveObject<ViewLockObject, ViewLockObject.ViewLockObjectSave>, AudioJobOnGameObject {
    public delegate void ViewLockEvent();

    [OnValueChanged(nameof(SetDragNDropTransform))]
    public Transform dragNDropCameraTransform; // Drag a desired Transform into this to set the position & rotation from it
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
    
    private void SetDragNDropTransform() {
        Transform t = dragNDropCameraTransform;
        if (t == null) return;
        
        List<ViewLockInfo> optionList = new List<ViewLockInfo>(viewLockOptions);
        optionList.Add(new ViewLockInfo() {
            camPosition = t.localPosition,
            camRotationEuler = t.localRotation.eulerAngles
        });
        viewLockOptions = optionList.ToArray();

        dragNDropCameraTransform = null;
    }

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

    public override void LoadSave(ViewLockObjectSave save) {
        _state = save.state;
        hitbox.enabled = save.colliderEnabled;
    }

    [Serializable]
    public class ViewLockObjectSave : SaveObject<ViewLockObject> {
        public PlayerLook.ViewLockState state;
        public bool colliderEnabled;

        public ViewLockObjectSave(ViewLockObject viewLockObject) : base(viewLockObject) {
            state = viewLockObject.state;
            colliderEnabled = viewLockObject.hitbox.enabled;
        }
    }
#endregion
}