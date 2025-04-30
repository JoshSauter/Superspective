using System;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class CurrentValueShutter : SuperspectiveObject<CurrentValueShutter, CurrentValueShutter.CurrentValueShutterSave> {
    private const float TIME_TO_OPEN = .75f;
    private const float TIME_TO_SHUT = 1.75f;

    private Vector3 startingPosition;
    //private readonly Vector3 distanceToOpen = new Vector3(0, 3, 5.5f);
    private readonly List<Vector3> distancesToOpen = new List<Vector3>() {
        new Vector3(0, 3, 0),
        new Vector3(0, 3, 5.5f),
        new Vector3(0, 0, 5.5f)
    };

    private Vector3 distanceToOpen;
    
    public Transform topLeft, topRight, botLeft, botRight;

    public enum State : byte {
        Open,
        Moving,
        Shut
    }
    public StateMachine<State> state;
    public bool isSetToOpen;
    private float lerpTime = 0; // 0 <-> 1

    protected override void Awake() {
        base.Awake();

        state = this.StateMachine(State.Open);
        
        // All 4 share the same pivot so this is the same for all of them
        startingPosition = topLeft.localPosition;
        distanceToOpen = distancesToOpen.RandomElementFrom();

        InitStateMachine();
    }

    void InitStateMachine() {
        state.AddStateTransition(State.Moving, () => isSetToOpen ? State.Open : State.Shut, () => (isSetToOpen ? lerpTime >= 1 : lerpTime <= 0));
        
        state.AddTrigger(s => s is State.Open or State.Shut, () => distanceToOpen = distancesToOpen.RandomElementFrom());
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        switch (state.State) {
            case State.Open:
                lerpTime = 1;
                break;
            case State.Moving:
                lerpTime += (isSetToOpen ? 1 : -1) * Time.deltaTime / (isSetToOpen ? TIME_TO_OPEN : TIME_TO_SHUT);
                break;
            case State.Shut:
                lerpTime = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        SetShutterDoors(Easing.EaseInOut(lerpTime));
    }

    void SetShutterDoors(float t) {
        topLeft.localPosition = startingPosition + t*distanceToOpen;
        topRight.localPosition = startingPosition + t*distanceToOpen.ScaledBy(1, 1, -1);
        botLeft.localPosition = startingPosition + t*distanceToOpen.ScaledBy(1, -1, 1);
        botRight.localPosition = startingPosition + t*distanceToOpen.ScaledBy(1, -1, -1);
    }
    
#region Saving

    public override void LoadSave(CurrentValueShutterSave save) {
        state.LoadFromSave(save.stateSave);
        lerpTime = save.lerpTime;
        isSetToOpen = save.isSetToOpen;
    }

    [Serializable]
	public class CurrentValueShutterSave : SaveObject<CurrentValueShutter> {
        public StateMachineSave<State> stateSave;
        public float lerpTime;
        public bool isSetToOpen;
        
		public CurrentValueShutterSave(CurrentValueShutter script) : base(script) {
            this.stateSave = script.state.ToSave();
            this.lerpTime = script.lerpTime;
            this.isSetToOpen = script.isSetToOpen;
		}
	}
#endregion
}
