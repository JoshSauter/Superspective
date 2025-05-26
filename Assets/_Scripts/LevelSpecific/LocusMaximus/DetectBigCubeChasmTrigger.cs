using System;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class DetectBigCubeChasmTrigger : SuperspectiveObject<DetectBigCubeChasmTrigger, DetectBigCubeChasmTrigger.DetectBigCubeChasmTriggerSave> {

    public TriggerOverlapZone triggerZone;
    public Collider chasmPlayerFloor;
    
    public enum State {
        Untriggered,
        Triggered
    }
    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();
        
        
        state = this.StateMachine(State.Untriggered);
    }

    protected override void Start() {
        base.Start();

        state.OnStateChangeSimple += UpdatePlayerChasmFloor;
        triggerZone.OnColliderAdded += HandleTriggerZoneColliderAdded;
    }

    private void HandleTriggerZoneColliderAdded(Collider other) {
        if (other.TaggedAsPlayer()) return;

        if (ChasmCubeAdjustTrigger.TryFindLargeCubeInZone(triggerZone, out PickupObject _)) {
            state.Set(State.Triggered);
        }
    }

    protected override void OnDisable() {
        base.OnDisable();
        
        state.OnStateChangeSimple -= UpdatePlayerChasmFloor;
    }

    private void UpdatePlayerChasmFloor() {
        chasmPlayerFloor.enabled = state == State.Triggered;
    }
    
#region Saving
		[Serializable]
		public class DetectBigCubeChasmTriggerSave : SaveObject<DetectBigCubeChasmTrigger> {
			public DetectBigCubeChasmTriggerSave(DetectBigCubeChasmTrigger script) : base(script) {
			}
		}

        public override void LoadSave(DetectBigCubeChasmTriggerSave save) {
            UpdatePlayerChasmFloor();
        }
#endregion
}
