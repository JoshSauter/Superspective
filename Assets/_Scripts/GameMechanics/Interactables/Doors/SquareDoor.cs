using System;
using System.Collections;
using DissolveObjects;
using PoweredObjects;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class SquareDoor : SuperspectiveObject<SquareDoor, SquareDoor.SquareDoorSave> {
    public SuperspectiveReference<PoweredObject, PoweredObject.PoweredObjectSave> poweredFrom;

    public DissolveObject centerDissolve;
    public Transform topLeft, topRight, bottomRight, bottomLeft;
    
    public enum State {
        Closed,
        CenterDissolving,
        DoorsOpening,
        Open,
        DoorsClosing,
        CenterReforming
    }
    public bool ClosingOrClosed => state.State is State.Closed or State.DoorsClosing or State.CenterReforming;
    public bool OpeningOrOpen => !ClosingOrClosed;
    public State startingState;
    public StateMachine<State> state;

    public float centerDissolveTime = 1.25f;
    public float doorOpenTime = .75f;
    public AnimationCurve doorOpenCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public float doorCloseTime = .25f;
    public AnimationCurve doorCloseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float centerReformTime = 1.25f;

    private const float DOOR_OPEN_DISTANCE = 4;

    protected override void Awake() {
        base.Awake();

        InitializeStateMachine();
    }

    private void InitializeStateMachine() {
        state = this.StateMachine(startingState);
        
        state.AddStateTransition(State.CenterDissolving, State.DoorsOpening, centerDissolveTime);
        state.AddStateTransition(State.DoorsOpening, State.Open, doorOpenTime);
        state.AddStateTransition(State.DoorsClosing, State.CenterReforming, doorCloseTime);
        state.AddStateTransition(State.CenterReforming, State.Closed, centerReformTime);
        
        state.AddTrigger(State.CenterDissolving, () => centerDissolve.Dematerialize());
        state.AddTrigger(State.CenterReforming, () => centerDissolve.Materialize());
        
        state.AddTrigger(State.Open, () => SetDoors(DOOR_OPEN_DISTANCE));
        state.AddTrigger(State.CenterReforming, () => SetDoors(0));
        
        state.WithUpdate(State.DoorsOpening, HandleDoorsMoving);
        state.WithUpdate(State.DoorsClosing, HandleDoorsMoving);
    }

    private void HandleDoorsMoving(float time) {
        float t;
        t = OpeningOrOpen ? doorOpenCurve.Evaluate(time / doorOpenTime) : doorCloseCurve.Evaluate(1 - time / doorCloseTime);
        float distance = DOOR_OPEN_DISTANCE * t;

        SetDoors(distance);
    }

    private void SetDoors(float distance) {
        topLeft.localPosition = new Vector3(-distance, distance, 0);
        topRight.localPosition = new Vector3(distance, distance, 0);
        bottomRight.localPosition = new Vector3(distance, -distance, 0);
        bottomLeft.localPosition = new Vector3(-distance, -distance, 0);
    }

    private IEnumerator InitializePoweredFrom() {
        yield return new WaitUntil(this.IsInActiveScene);

        PoweredObject poweredObject = poweredFrom.GetOrNull();
        if (poweredObject != null) {
            poweredObject.OnPowerFinish += OpenDoor;
            poweredObject.OnDepowerBegin += CloseDoor;
        }
    }

    protected override void Start() {
        base.Start();
        
        StartCoroutine(InitializePoweredFrom());
    }
    
    public void OpenDoor() {
        if (OpeningOrOpen) return;

        float lerpValue;
        switch (state.State) {
            case State.Closed:
                state.Set(State.CenterDissolving);
                break;
            case State.DoorsClosing:
                float newTime = GetEquivalentTimeSwitchingCurves(
                    state.Time,
                    doorCloseTime,
                    doorCloseCurve,
                    doorOpenTime,
                    doorOpenCurve
                );
                state.Set(State.DoorsClosing, newTime);
                break;
            case State.CenterReforming:
                lerpValue = state.Time / centerReformTime;
                state.Set(State.CenterDissolving, (1-lerpValue) * centerDissolveTime);
                break;
            case State.CenterDissolving:
            case State.Open:
            case State.DoorsOpening:
                throw new ArgumentOutOfRangeException($"Cannot open door from state {state.State}");
        }
    }

    public void CloseDoor() {
        if (ClosingOrClosed) return;

        float lerpValue;
        switch (state.State) {
            case State.Open:
                state.Set(State.DoorsClosing);
                break;
            case State.DoorsOpening:
                float newTime = GetEquivalentTimeSwitchingCurves(
                    state.Time,
                    doorOpenTime,
                    doorOpenCurve,
                    doorCloseTime,
                    doorCloseCurve
                );
                state.Set(State.DoorsClosing, newTime);
                break;
            case State.CenterDissolving:
                lerpValue = state.Time / centerDissolveTime;
                state.Set(State.CenterReforming, (1-lerpValue) * centerReformTime);
                break;
            case State.CenterReforming:
            case State.Closed:
            case State.DoorsClosing:
                throw new ArgumentOutOfRangeException($"Cannot close door from state {state.State}");
        }
    }
    
    // Helper function to get equivalent time on a new curve
    float GetEquivalentTimeSwitchingCurves(
        float elapsedTime,
        float fromDuration,
        AnimationCurve fromCurve,
        float toDuration,
        AnimationCurve toCurve) {
        float t = elapsedTime / fromDuration;               // time space -> t space
        float y = fromCurve.Evaluate(t);                    // t space -> curve space
        float reversedY = 1f - y;                           // same visual progress, reversed
        float tNew = toCurve.InverseEvaluate(reversedY, 0, 1); // find corresponding point in new curve
        return tNew * toDuration;                           // scale to target duration
    }

    
#region Saving
		[Serializable]
		public class SquareDoorSave : SaveObject<SquareDoor> {
            public StateMachine<State>.StateMachineSave stateSave;
            
			public SquareDoorSave(SquareDoor script) : base(script) {
                this.stateSave = script.state.ToSave();
			}
		}

        public override void LoadSave(SquareDoorSave save) {
            state.LoadFromSave(save.stateSave);
        }
#endregion
}
