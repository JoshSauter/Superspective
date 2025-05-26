using System;
using PoweredObjects;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class MiniatureMazeNode : SuperspectiveObject<MiniatureMazeNode, MiniatureMazeNode.MiniatureMazeNodeSave> {
    
    public enum State : byte {
        Off,
        On
    }
    public StateMachine<State> state;
    public PoweredObject pwr;
    private ColorChangeOnPower colorChangeOnPower;

    protected override void Awake() {
        base.Awake();
        
        pwr = GetComponentInParent<PoweredObject>();
        colorChangeOnPower = GetComponentInParent<ColorChangeOnPower>();
        
        pwr.automaticallyFinishPowering = true;
        pwr.automaticallyFinishDepowering = true;
        pwr.automaticFinishPoweringTime = colorChangeOnPower.timeToChangeColor;
        pwr.automaticFinishDepoweringTime = colorChangeOnPower.timeToChangeColor;
        
        state = this.StateMachine(State.Off);
    }

    protected override void Start() {
        base.Start();
        
        InitializeStateMachine();
    }

    void InitializeStateMachine() {
        state.AddTrigger(State.On, () => pwr.state.Set(PowerState.PartiallyPowered));
        state.AddTrigger(State.Off, () => pwr.state.Set(PowerState.PartiallyDepowered));
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer() && MiniatureMaze.instance.state == MiniatureMaze.State.PlayerInMaze) {
            state.Set(State.On);
        }
    }

#region Saving

    public override void LoadSave(MiniatureMazeNodeSave save) { }

    [Serializable]
	public class MiniatureMazeNodeSave : SaveObject<MiniatureMazeNode> {
		public MiniatureMazeNodeSave(MiniatureMazeNode script) : base(script) { }
	}
#endregion
}
