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
public class FrontDoorway : SaveableObject<FrontDoorway, FrontDoorway.FrontDoorwaySave> {
	public DimensionObject movingDoorway, staticOpenDoorway;
	public Transform leftDoor, rightDoor;
	public Renderer visibilityMask;

	private const float timeToMove = 5f;
	const float distanceToMove = 5;
	private AnimationCurve doorMoveCurve = AnimationCurve.EaseInOut(0,0,1,distanceToMove);
	private BladeEdgeDetection edgeDetection => MaskBufferRenderTextures.instance.edgeDetection;
	public ToggleEdgeDetectionOnPortal portalEdgeColors;

	public enum State {
        Open,
        Closing,
        Closed
    }
    public StateMachine<State> state;

    protected override void Start() {
        base.Start();

        state = this.StateMachine(State.Open);

        state.AddStateTransition(State.Closing, State.Closed, timeToMove);
    }

    public void Close() {
	    if (state == State.Open) {
		    state.Set(State.Closing);
	    }
    }

    void Update() {
	    bool edgesAreWhite = edgeDetection.EdgesAreWhite();
	    bool edgesAreBlack = edgeDetection.EdgesAreBlack();
	    visibilityMask.enabled = state != State.Open && edgesAreBlack && portalEdgeColors.portalEdgesAreWhite;
	    if (edgesAreWhite) {
		    movingDoorway.SwitchVisibilityState(VisibilityState.Invisible, true);
		    staticOpenDoorway.SwitchVisibilityState(VisibilityState.Visible, true);
	    }
	    else {
		    movingDoorway.SwitchVisibilityState(VisibilityState.PartiallyInvisible, true);
		    staticOpenDoorway.SwitchVisibilityState(VisibilityState.PartiallyVisible, true);
	    }

	    float t;
	    switch (state.State) {
		    case State.Open:
			    t = 0;
			    break;
		    case State.Closing:
			    t = state.Time / timeToMove;
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
		[Serializable]
		public class FrontDoorwaySave : SerializableSaveObject<FrontDoorway> {
            private StateMachine<State>.StateMachineSave stateSave;
            
			public FrontDoorwaySave(FrontDoorway script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(FrontDoorway script) {
                script.state.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
