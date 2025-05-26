using System;
using System.Collections;
using System.Collections.Generic;
using LevelSpecific.Fork;
using PortalMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class FrontDoorway : SuperspectiveObject<FrontDoorway, FrontDoorway.FrontDoorwaySave> {
	public DimensionObject movingDoorway, staticOpenDoorway;
	public Transform leftDoor, rightDoor;
	public Renderer visibilityMask;

	private const float TIME_TO_MOVE = 5f;
	private const float DISTANCE_TO_MOVE = 5;
	private readonly AnimationCurve doorMoveCurve = AnimationCurve.EaseInOut(0,0,1,DISTANCE_TO_MOVE);
	private BladeEdgeDetection EdgeDetection => MaskBufferRenderTextures.instance.edgeDetection;
	public ToggleEdgeDetectionOnPortal portalEdgeColors;

	public enum State : byte {
        Open,
        Closing,
        Closed
    }
    public StateMachine<State> state;

    protected override void Start() {
        base.Start();

        state = this.StateMachine(State.Open);

        state.AddStateTransition(State.Closing, State.Closed, TIME_TO_MOVE);
    }

    public void Close() {
	    if (state == State.Open) {
		    state.Set(State.Closing);
	    }
    }

    void Update() {
	    bool edgesAreWhite = EdgeDetection.EdgesAreWhite();
	    bool edgesAreBlack = EdgeDetection.EdgesAreBlack();
	    visibilityMask.enabled = state != State.Open && edgesAreBlack && portalEdgeColors.portalEdgesAreWhite;
	    if (edgesAreWhite) {
		    movingDoorway.SwitchVisibilityState(VisibilityState.Invisible, DimensionObject.RefreshMode.All, true);
		    staticOpenDoorway.SwitchVisibilityState(VisibilityState.Visible, DimensionObject.RefreshMode.All, true);
	    }
	    else {
		    movingDoorway.SwitchVisibilityState(VisibilityState.PartiallyInvisible, DimensionObject.RefreshMode.All, true);
		    staticOpenDoorway.SwitchVisibilityState(VisibilityState.PartiallyVisible, DimensionObject.RefreshMode.All, true);
	    }

	    float t;
	    switch (state.State) {
		    case State.Open:
			    t = 0;
			    break;
		    case State.Closing:
			    t = state.Time / TIME_TO_MOVE;
			    break;
		    case State.Closed:
			    t = 1;
			    break;
		    default:
			    throw new ArgumentOutOfRangeException();
	    }
	    leftDoor.transform.localPosition = Vector3.right * doorMoveCurve.Evaluate(t);
	    rightDoor.transform.localPosition = Vector3.left * doorMoveCurve.Evaluate(t);
    }
    
#region Saving

	public override void LoadSave(FrontDoorwaySave save) { }

	[Serializable]
	public class FrontDoorwaySave : SaveObject<FrontDoorway> {
		public FrontDoorwaySave(FrontDoorway script) : base(script) { }
	}
#endregion
}
