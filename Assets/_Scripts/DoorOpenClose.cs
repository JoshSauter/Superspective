using System;
using System.Linq;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class DoorOpenClose : SuperspectiveObject<DoorOpenClose, DoorOpenClose.DoorOpenCloseSave> {
    public enum DoorState : byte {
        Closed,
        Opening,
        Open,
        Closing
    }

    public float targetLocalXScale = 0;
    public AnimationCurve doorOpenCurve;
    public AnimationCurve doorCloseCurve;

    public float timeBetweenEachDoorPiece = 0.4f;
    public float timeForEachDoorPieceToOpen = 2f;
    public float timeForEachDoorPieceToClose = 0.5f;
    DoorState _state = DoorState.Closed;

    Vector3 closedScale;

    Transform[] doorPieces;
    Vector3 openedScale;
    bool playerInTriggerZoneThisFrame;

    // Has to be re-asserted every physics timestep, else will close the door
    bool playerWasInTriggerZoneLastFrame;
    bool queueDoorClose;
    float timeSinceStateChange;

    public bool useZInstead = false;

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
        openedScale = useZInstead ? closedScale.WithZ(targetLocalXScale) : closedScale.WithX(targetLocalXScale);
    }

    // Update is called once per frame
    void Update() {
        if (!DEBUG) return;

        if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.R) && state != DoorState.Opening &&
            state != DoorState.Closing)
            ResetDoorPieceScales();

        else if (DebugInput.GetKey(KeyCode.LeftShift) && DebugInput.GetKeyDown(KeyCode.O) && state != DoorState.Opening &&
                 state != DoorState.Closing)
            state = DoorState.Closing;

        else if (DebugInput.GetKeyDown(KeyCode.O) && state != DoorState.Opening && state != DoorState.Closing)
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

    public override void LoadSave(DoorOpenCloseSave save) {
        doorOpenCurve = save.doorOpenCurve;
        doorCloseCurve = save.doorCloseCurve;

        for (int i = 0; i < save.doorPieceScales.Length; i++) {
            doorPieces[i].localScale = save.doorPieceScales[i];
        }

        _state = save.state;
        timeSinceStateChange = save.timeSinceStateChange;
        queueDoorClose = save.queueDoorClose;

        closedScale = save.closedScale;
        openedScale = save.openedScale;

        playerWasInTriggerZoneLastFrame = save.playerWasInTriggerZoneLastFrame;
        playerInTriggerZoneThisFrame = save.playerInTriggerZoneThisFrame;

        timeBetweenEachDoorPiece = save.timeBetweenEachDoorPiece;
        timeForEachDoorPieceToOpen = save.timeForEachDoorPieceToOpen;
        timeForEachDoorPieceToClose = save.timeForEachDoorPieceToClose;
    }

    public override bool SkipSave {
        get => !gameObject.activeInHierarchy;
        set { }
    }

    [Serializable]
    public class DoorOpenCloseSave : SaveObject<DoorOpenClose> {
        public SerializableAnimationCurve doorCloseCurve;
        public SerializableAnimationCurve doorOpenCurve;
        public SerializableVector3[] doorPieceScales;
        public SerializableVector3 closedScale;
        public SerializableVector3 openedScale;
        public float timeBetweenEachDoorPiece;
        public float timeForEachDoorPieceToOpen;
        public float timeForEachDoorPieceToClose;
        public float timeSinceStateChange;
        public DoorState state;
        public bool playerInTriggerZoneThisFrame;
        public bool playerWasInTriggerZoneLastFrame;
        public bool queueDoorClose;

        public DoorOpenCloseSave(DoorOpenClose door) : base(door) {
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
    }
#endregion
}