using System;
using System.Linq;
using EpitaphUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class DoorOpenClose : SaveableObject<DoorOpenClose, DoorOpenClose.DoorOpenCloseSave> {
    public enum DoorState {
        Closed,
        Opening,
        Open,
        Closing
    }

    const float targetLocalXScale = 0;
    public AnimationCurve doorOpenCurve;
    public AnimationCurve doorCloseCurve;

    public float timeBetweenEachDoorPiece = 0.4f;
    public float timeForEachDoorPieceToOpen = 2f;
    public float timeForEachDoorPieceToClose = 0.5f;
    UniqueId _id;
    DoorState _state = DoorState.Closed;

    Vector3 closedScale;

    Transform[] doorPieces;
    Vector3 openedScale;
    bool playerInTriggerZoneThisFrame;

    // Has to be re-asserted every physics timestep, else will close the door
    bool playerWasInTriggerZoneLastFrame;
    bool queueDoorClose;
    float timeSinceStateChange;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    public DoorState state {
        get => _state;
        set {
            if (_state == value) return;
            switch (value) {
                case DoorState.Closed:
                    OnDoorCloseEnd?.Invoke();
                    break;
                case DoorState.Opening:
                    OnDoorOpenStart?.Invoke();
                    break;
                case DoorState.Open:
                    OnDoorOpenEnd?.Invoke();
                    break;
                case DoorState.Closing:
                    OnDoorCloseStart?.Invoke();
                    break;
            }

            timeSinceStateChange = 0f;
            _state = value;
        }
    }

    protected override void Awake() {
        base.Awake();
        doorPieces = transform.GetComponentsInChildrenOnly<Transform>();
        closedScale = doorPieces[0].localScale;
        openedScale = new Vector3(targetLocalXScale, closedScale.y, closedScale.z);
    }

    // Update is called once per frame
    void Update() {
        if (!DEBUG) return;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R) && state != DoorState.Opening &&
            state != DoorState.Closing)
            ResetDoorPieceScales();

        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.O) && state != DoorState.Opening &&
                 state != DoorState.Closing)
            state = DoorState.Closing;

        else if (Input.GetKeyDown(KeyCode.O) && state != DoorState.Opening && state != DoorState.Closing)
            state = DoorState.Opening;
    }

    void FixedUpdate() {
        if ((state == DoorState.Open || state == DoorState.Opening) && playerWasInTriggerZoneLastFrame &&
            !playerInTriggerZoneThisFrame) queueDoorClose = true;

        if (queueDoorClose && state == DoorState.Open) CloseDoor();

        UpdateDoor();

        // Need to re-assert this every physics timestep, reset state
        playerWasInTriggerZoneLastFrame = playerInTriggerZoneThisFrame;
        playerInTriggerZoneThisFrame = false;
    }

    void OnTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) queueDoorClose = true;
    }

    void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            if (state == DoorState.Closed) OpenDoor();
            playerInTriggerZoneThisFrame = true;
        }
    }

    void UpdateDoor() {
        timeSinceStateChange += Time.fixedDeltaTime;
        switch (state) {
            case DoorState.Closed:
                break;
            case DoorState.Opening: {
                float totalTime = timeBetweenEachDoorPiece * (doorPieces.Length - 1) + timeForEachDoorPieceToOpen;
                if (timeSinceStateChange < totalTime) {
                    for (int i = doorPieces.Length - 1; i >= 0; i--) {
                        float timeIndex = doorPieces.Length - i - 1;
                        float startTime = timeIndex * timeBetweenEachDoorPiece;
                        float endTime = startTime + timeForEachDoorPieceToOpen;
                        float t = Mathf.InverseLerp(startTime, endTime, timeSinceStateChange);

                        doorPieces[i].localScale = Vector3.LerpUnclamped(
                            closedScale,
                            openedScale,
                            doorOpenCurve.Evaluate(t)
                        );
                    }
                }
                else {
                    foreach (Transform piece in doorPieces) {
                        piece.localScale = openedScale;
                    }

                    state = DoorState.Open;
                }

                break;
            }
            case DoorState.Open:
                break;
            case DoorState.Closing: {
                float totalTime = timeBetweenEachDoorPiece * (doorPieces.Length - 1) + timeForEachDoorPieceToClose;
                if (timeSinceStateChange < totalTime) {
                    for (int i = doorPieces.Length - 1; i >= 0; i--) {
                        float timeIndex = doorPieces.Length - i - 1;
                        float startTime = timeIndex * timeBetweenEachDoorPiece;
                        float endTime = startTime + timeForEachDoorPieceToClose;
                        float t = Mathf.InverseLerp(startTime, endTime, timeSinceStateChange);

                        doorPieces[i].localScale = Vector3.LerpUnclamped(
                            openedScale,
                            closedScale,
                            doorCloseCurve.Evaluate(t)
                        );
                    }
                }
                else {
                    for (int i = 0; i < doorPieces.Length; i++) {
                        doorPieces[i].localScale = closedScale;
                    }

                    state = DoorState.Closed;
                }

                break;
            }
        }
    }

    void ResetDoorPieceScales() {
        for (int i = 0; i < doorPieces.Length; i++) {
            doorPieces[i].localScale = closedScale;
        }
    }

    public void OpenDoor() {
        if (state == DoorState.Closed) state = DoorState.Opening;
    }

    public void CloseDoor() {
        if (state == DoorState.Open) {
            queueDoorClose = false;
            state = DoorState.Closing;
        }
    }

#region events
    public delegate void DoorAction();

    public event DoorAction OnDoorOpenStart;
    public event DoorAction OnDoorCloseStart;
    public event DoorAction OnDoorOpenEnd;
    public event DoorAction OnDoorCloseEnd;
#endregion

#region Saving
    public override bool SkipSave {
        get => !gameObject.activeInHierarchy;
        set { }
    }

    public override string ID => $"DoorOpenClose_{id.uniqueId}";

    [Serializable]
    public class DoorOpenCloseSave : SerializableSaveObject<DoorOpenClose> {
        public float timeBetweenEachDoorPiece;
        public float timeForEachDoorPieceToOpen;
        public float timeForEachDoorPieceToClose;

        SerializableVector3 closedScale;
        SerializableAnimationCurve doorCloseCurve;
        SerializableAnimationCurve doorOpenCurve;

        SerializableVector3[] doorPieceScales;
        SerializableVector3 openedScale;
        bool playerInTriggerZoneThisFrame;

        bool playerWasInTriggerZoneLastFrame;
        bool queueDoorClose;
        DoorState state;
        float timeSinceStateChange;

        public DoorOpenCloseSave(DoorOpenClose door) {
            doorOpenCurve = door.doorOpenCurve;
            doorCloseCurve = door.doorCloseCurve;

            doorPieceScales = door.doorPieces.Select<Transform, SerializableVector3>(d => d.localScale).ToArray();
            state = door.state;
            timeSinceStateChange = door.timeSinceStateChange;
            queueDoorClose = door.queueDoorClose;

            closedScale = door.closedScale;
            openedScale = door.openedScale;

            playerWasInTriggerZoneLastFrame = door.playerWasInTriggerZoneLastFrame;
            playerInTriggerZoneThisFrame = door.playerInTriggerZoneThisFrame;

            timeBetweenEachDoorPiece = door.timeBetweenEachDoorPiece;
            timeForEachDoorPieceToOpen = door.timeForEachDoorPieceToOpen;
            timeForEachDoorPieceToClose = door.timeForEachDoorPieceToClose;
        }

        public override void LoadSave(DoorOpenClose door) {
            door.doorOpenCurve = doorOpenCurve;
            door.doorCloseCurve = doorCloseCurve;

            for (int i = 0; i < doorPieceScales.Length; i++) {
                door.doorPieces[i].localScale = doorPieceScales[i];
            }

            door._state = state;
            door.timeSinceStateChange = timeSinceStateChange;
            door.queueDoorClose = queueDoorClose;

            door.closedScale = closedScale;
            door.openedScale = openedScale;

            door.playerWasInTriggerZoneLastFrame = playerWasInTriggerZoneLastFrame;
            door.playerInTriggerZoneThisFrame = playerInTriggerZoneThisFrame;

            door.timeBetweenEachDoorPiece = timeBetweenEachDoorPiece;
            door.timeForEachDoorPieceToOpen = timeForEachDoorPieceToOpen;
            door.timeForEachDoorPieceToClose = timeForEachDoorPieceToClose;
        }
    }
#endregion
}