using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PoweredObjects;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId), typeof(BetterTrigger))]
public class WeightedPressurePlate : SaveableObject<WeightedPressurePlate, WeightedPressurePlate.WeightedPressurePlateSave>, BetterTriggers {
    public PowerTrail powerTrail;
    public float targetWeight = 30;
    [SerializeField]
    private float _currentWeight;

    public float currentWeight {
        get => _currentWeight;
        set {
            _currentWeight = value;
            if (_currentWeight == targetWeight) {
                state.Set(State.OnTarget);
            }
            else if (_currentWeight == 0) {
                state.Set(State.NoWeight);
            }
            else if (_currentWeight < targetWeight) {
                state.Set(State.UnderTarget);
            }
            else {
                state.Set(State.OverTarget);
            }
        }
    }
    public enum State {
        NoWeight,
        UnderTarget,
        OnTarget,
        OverTarget
    }

    public StateMachine<State> state;

    protected override void Awake() {
        base.Awake();
        state = this.StateMachine(State.NoWeight);
    }

    protected override void Start() {
        base.Start();
        InitializeStateMachine();
    }

    void InitializeStateMachine() {
        state.AddTrigger(State.NoWeight, () => {
            powerTrail.pwr.PowerIsOn = false;
            powerTrail.targetFillAmount = 1f;
        });
        state.AddTrigger(State.UnderTarget, () => {
            powerTrail.pwr.PowerIsOn = true;
            powerTrail.targetFillAmount = currentWeight / targetWeight;
        });
        state.AddTrigger(State.OnTarget, () => {
            powerTrail.pwr.PowerIsOn = true;
            powerTrail.targetFillAmount = 1f;
        });
        state.AddTrigger(State.OverTarget, () => {
            powerTrail.pwr.PowerIsOn = false;
            powerTrail.targetFillAmount = 1f;
        });
    }

    bool ColliderIsHeld(Collider c) {
        if (Player.instance.IsHoldingSomething) {
            return Player.instance.heldObject.thisCollider == c;
        }

        return false;
    }

    public void OnBetterTriggerEnter(Collider c) {
        if (!ColliderIsHeld(c)) {
            float weight = c.GetComponentsInChildren<Rigidbody>().Sum(rb => rb.mass);
            currentWeight += weight;
        }

        if (c.TaggedAsPlayer()) {
            if (Player.instance.IsHoldingSomething) {
                currentWeight += Player.instance.heldObject.thisRigidbody.mass;
            }
        }
    }

    public void OnBetterTriggerExit(Collider c) {
        if (!ColliderIsHeld(c)) {
            float weight = c.GetComponentsInChildren<Rigidbody>().Sum(rb => rb.mass);
            currentWeight -= weight;
        }
        
        if (c.TaggedAsPlayer()) {
            if (Player.instance.IsHoldingSomething) {
                currentWeight -= Player.instance.heldObject.thisRigidbody.mass;
            }
        }
    }

    public void OnBetterTriggerStay(Collider c) { }

#region Saving
		[Serializable]
		public class WeightedPressurePlateSave : SerializableSaveObject<WeightedPressurePlate> {
            private float targetWeight;
            private StateMachine<State>.StateMachineSave stateSave;
            
			public WeightedPressurePlateSave(WeightedPressurePlate script) : base(script) {
                this.targetWeight = script.targetWeight;
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(WeightedPressurePlate script) {
                script.state.LoadFromSave(this.stateSave);
                script.targetWeight = this.targetWeight;
            }
		}
#endregion
}
