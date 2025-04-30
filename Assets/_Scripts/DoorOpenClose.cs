using System;
using System.Linq;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using StateUtils;
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
    float AnimationTime => timeBetweenEachDoorPiece * (doorPieces.Length - 1) + timeForEachDoorPieceToOpen;
    Vector3 closedScale;

    Transform[] doorPieces;
    Vector3 openedScale;
    private TriggerOverlapZone trigger;

    public bool useZInstead = false;

    public StateMachine<DoorState> state;

    protected override void Awake() {
        base.Awake();
        trigger = this.GetOrAddComponent<TriggerOverlapZone>();
        
        doorPieces = transform.GetComponentsInChildrenOnly<Transform>();
        closedScale = doorPieces[0].localScale;
        openedScale = useZInstead ? closedScale.WithZ(targetLocalXScale) : closedScale.WithX(targetLocalXScale);

        InitializeStateMachine();
    }

    private void InitializeStateMachine() {
        state = this.StateMachine(DoorState.Closed);

        float totalTime = AnimationTime;
        state.AddStateTransition(DoorState.Closed, DoorState.Opening, () => trigger.playerInZone);
        state.AddStateTransition(DoorState.Opening, DoorState.Open, totalTime);
        state.AddStateTransition(DoorState.Open, DoorState.Closing, () => !trigger.playerInZone);
        state.AddStateTransition(DoorState.Closing, DoorState.Closed, totalTime);
        
        state.WithUpdate(DoorState.Closing, time => {
            for (int i = 0; i < doorPieces.Length; i++) {
                float timeIndex = i;
                float startTime = timeIndex * timeBetweenEachDoorPiece;
                float endTime = startTime + timeForEachDoorPieceToOpen;
                float t = Mathf.InverseLerp(startTime, endTime, time);

                doorPieces[i].localScale = Vector3.LerpUnclamped(
                    openedScale,
                    closedScale,
                    doorOpenCurve.Evaluate(t)
                );
            }
        });
        
        state.WithUpdate(DoorState.Opening, time => {
            for (int i = doorPieces.Length - 1; i >= 0; i--) {
                float timeIndex = doorPieces.Length - i - 1;
                float startTime = timeIndex * timeBetweenEachDoorPiece;
                float endTime = startTime + timeForEachDoorPieceToOpen;
                float t = Mathf.InverseLerp(startTime, endTime, time);

                doorPieces[i].localScale = Vector3.LerpUnclamped(
                    closedScale,
                    openedScale,
                    doorOpenCurve.Evaluate(t)
                );
            }
        });

        state.OnStateChangeSimple += () => {
            switch (state.State) {
                case DoorState.Closed:
                    foreach (Transform piece in doorPieces) {
                        piece.localScale = closedScale;
                    }
                    OnDoorCloseEnd?.Invoke();
                    break;
                case DoorState.Opening:
                    OnDoorOpenStart?.Invoke();
                    break;
                case DoorState.Open:
                    foreach (Transform piece in doorPieces) {
                        piece.localScale = openedScale;
                    }
                    OnDoorOpenEnd?.Invoke();
                    break;
                case DoorState.Closing:
                    OnDoorCloseStart?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public void OpenDoor() {
        state.Set(DoorState.Opening);
    }

    public void CloseDoor() {
        state.Set(DoorState.Closing);
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
        for (int i = 0; i < save.doorPieceScales.Length; i++) {
            doorPieces[i].localScale = save.doorPieceScales[i];
        }
    }

    public override bool SkipSave {
        get => !gameObject.activeInHierarchy;
        set { }
    }

    [Serializable]
    public class DoorOpenCloseSave : SaveObject<DoorOpenClose> {
        public SerializableVector3[] doorPieceScales;

        public DoorOpenCloseSave(DoorOpenClose door) : base(door) {
            doorPieceScales = door.doorPieces.Select<Transform, SerializableVector3>(d => d.localScale).ToArray();
        }
    }
#endregion
}