using System;
using LevelManagement;
using PortalMechanics;
using UnityEngine;
using Saving;
using StateUtils;

[RequireComponent(typeof(UniqueId))]
// TODO: Implement the idea proposed in the Idea gameobject, this is a placeholder for now
public class RoseRoomExit : SuperspectiveObject<RoseRoomExit, RoseRoomExit.RoseRoomExitSave> {
    
    public enum State {
        Exterior,
        InteriorCurved
    }
    public StateMachine<State> state;

    public GameObject exteriorRoot;
    public GameObject interiorCurvedRoot;
    private GameObject[] roots;

    public Portal interiorReflectorPortal;

    protected override void Start() {
        base.Start();

        InitializeStateMachine();

        interiorReflectorPortal.OnPortalTeleportPlayerSimple += () => {
            if (LevelManager.instance.ActiveScene == Levels.RoseRoomExit) {
                LevelManager.instance.SwitchActiveScene(Levels.RoseRoomExit2);
            }
            else if (LevelManager.instance.ActiveScene == Levels.RoseRoomExit2) {
                LevelManager.instance.SwitchActiveScene(Levels.RoseRoomExit);
            }
        };
    }

    private void InitializeStateMachine() {
        state = this.StateMachine(State.Exterior);
        
        roots = new GameObject[] { exteriorRoot, interiorCurvedRoot };
        state.OnStateChangeSimple += UpdateState;
    }

    public void SetAsExterior() {
        state.Set(State.Exterior);
    }
    
    public void SetAsInteriorCurved() {
        state.Set(State.InteriorCurved);
    }

    private void UpdateState() {
        for (int i = 0; i < roots.Length; i++) {
            roots[i].SetActive(i == (int)state.State);
        }
    }
    
#region Saving
		[Serializable]
		public class RoseRoomExitSave : SaveObject<RoseRoomExit> {
			public RoseRoomExitSave(RoseRoomExit script) : base(script) { }
		}

        public override void LoadSave(RoseRoomExitSave save) {
            UpdateState();
        }
#endregion
}
