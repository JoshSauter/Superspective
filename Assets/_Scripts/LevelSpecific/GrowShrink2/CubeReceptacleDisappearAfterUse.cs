using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PoweredObjects;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

/// <summary>
/// This script is used to make a cube receptacle disappear after the player has used it and no longer needs it.
/// I'm hoping that this will lead to players re-using the cube that powered the receptacle
/// </summary>
[RequireComponent(typeof(UniqueId))]
public class CubeReceptacleDisappearAfterUse : SuperspectiveObject<CubeReceptacleDisappearAfterUse, CubeReceptacleDisappearAfterUse.CubeReceptacleDisappearAfterUseSave> {
    // PowerTrail must be powered
    public PoweredObject powerTrigger;
    // Revealed doorway DimensionObject must be in the correct visibility state
    public DimensionObject dimensionObjectTrigger;
    public CubeReceptacle cubeReceptacle;
    private Renderer _cubeReceptacleRenderer;
    private Renderer cubeReceptacleRenderer => _cubeReceptacleRenderer ??= cubeReceptacle.GetComponent<Renderer>();
    public List<GameObject> otherObjectsToDisappear;
    private List<Renderer> otherObjectsToDisappearRenderers;
    
    public enum State : byte {
        Idle,
        AwaitingPlayerLookAway,
        Gone
    }
    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();
        
        InitializeStateMachine();
        otherObjectsToDisappearRenderers = otherObjectsToDisappear.SelectMany(o => o.GetComponentsInChildren<Renderer>()).ToList();
    }
    
    private bool ShouldReceptacleDisappear() {
        Camera playerCam = Player.instance.PlayerCam;
        return !cubeReceptacleRenderer.IsVisibleFrom(playerCam) &&
               otherObjectsToDisappearRenderers.TrueForAll(r => !r.IsVisibleFrom(playerCam));
    }

    private bool ShouldAwaitPlayerLookAway() {
        return powerTrigger.PowerIsOn && dimensionObjectTrigger.EffectiveVisibilityState == VisibilityState.Visible;
    }

    void InitializeStateMachine() {
        state = this.StateMachine(State.Idle);
        
        state.AddStateTransition(State.Idle, State.AwaitingPlayerLookAway, ShouldAwaitPlayerLookAway);
        state.AddStateTransition(State.AwaitingPlayerLookAway, State.Idle, () => !ShouldAwaitPlayerLookAway());
        state.AddStateTransition(State.AwaitingPlayerLookAway, State.Gone, ShouldReceptacleDisappear);
        
        state.AddTrigger(State.Gone, () => {
            cubeReceptacle.ExpelCube();
            cubeReceptacle.gameObject.SetActive(false);
            otherObjectsToDisappear.ForEach(o => o.gameObject.SetActive(false));
        });
    }
    
#region Saving

    public override void LoadSave(CubeReceptacleDisappearAfterUseSave save) {
        state.LoadFromSave(save.stateSave);
    }

    [Serializable]
	public class CubeReceptacleDisappearAfterUseSave : SaveObject<CubeReceptacleDisappearAfterUse> {
        public StateMachine<State>.StateMachineSave stateSave;
        
		public CubeReceptacleDisappearAfterUseSave(CubeReceptacleDisappearAfterUse script) : base(script) {
            this.stateSave = script.state.ToSave();
		}
	}
#endregion
}
