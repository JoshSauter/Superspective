using System;
using System.Collections;
using System.Collections.Generic;
using PoweredObjects;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class MiniatureMazeNode : SaveableObject<MiniatureMazeNode, MiniatureMazeNode.MiniatureMazeNodeSave> {
    
    public enum State {
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
		[Serializable]
		public class MiniatureMazeNodeSave : SerializableSaveObject<MiniatureMazeNode> {
            private StateMachine<State>.StateMachineSave stateSave;
            
			public MiniatureMazeNodeSave(MiniatureMazeNode script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(MiniatureMazeNode script) {
                script.state.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
