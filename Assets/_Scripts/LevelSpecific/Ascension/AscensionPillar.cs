using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;

[RequireComponent(typeof(UniqueId))]
public class AscensionPillar : SuperspectiveObject<AscensionPillar, AscensionPillar.AscensionPillarSave> {
    public DimensionPillar prevPillar;
    public PillarDimensionObject pillarDimensionObj;
    
    public enum State : byte {
        NotActive,
        NotYetVisible,
        Visible
    }
    public StateMachine<State> state;
    public State startingState = State.NotActive;

    protected override void Awake() {
        base.Awake();
        
        InitializeStateMachine();
    }

    private void InitializeStateMachine() {
        state = this.StateMachine(startingState);
        
        state.AddStateTransition(State.NotActive, State.NotYetVisible, () => prevPillar.enabled);
        pillarDimensionObj.OnStateChangeSimple += () => {
            if (pillarDimensionObj.EffectiveVisibilityState == VisibilityState.Visible) {
                state.Set(State.Visible);
                GetComponent<DimensionPillar>().enabled = true;
            }
        };
        
        state.AddTrigger(State.Visible, () => prevPillar.enabled = false);
        
    }
    
#region Saving

    public override void LoadSave(AscensionPillarSave save) {
    }

    [Serializable]
	public class AscensionPillarSave : SaveObject<AscensionPillar> {
		public AscensionPillarSave(AscensionPillar script) : base(script) { }
	}
#endregion
}
