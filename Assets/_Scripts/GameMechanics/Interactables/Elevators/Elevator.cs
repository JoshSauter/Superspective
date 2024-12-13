using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

/// <summary>
/// Contains all the basic functionality for an elevator, including moving the Player and other objects when they are sitting on the elevator.
/// You must create a Collider object that represents the area just above the elevator surface to determine when objects are on the elevator.
/// Instead of re-implementing this functionality for every elevator, just have a has-of relationship with this script.
/// </summary>
[RequireComponent(typeof(UniqueId))]
public class Elevator : SaveableObject<Elevator, Elevator.ElevatorSave> {
    public enum ElevatorState {
        Idle,
        Moving
    }
    public StateMachine<ElevatorState> state;
    
    [Header("Set up references:")]
    public TriggerOverlapZone triggerZone;


    [Header("Config:")]
    public int direction;
    public float speed = 4f;
    public float acceleration = 1f;
    public float changeDirectionAcceleration = 1f; // Acceleration used when velocity direction does not match desired direction
    
    [Header("Animation curve should be normalized in the 0-1 range. X-axis is normalized distance, Y-axis is speed multiplier.")]
    public AnimationCurve speedMultiplierCurve = AnimationCurve.Constant(0f, 1f, 1f);
    public float SpeedMultiplier => speedMultiplierCurve.Evaluate(DistanceTraveled / TotalHeight);
    
    public float minHeight;
    public float maxHeight;

    public float curVelocity;
    private float DesiredVelocity => (0.05f + SpeedMultiplier) * speed * direction;
    
    public bool IsAtTop => CurHeight >= maxHeight;
    public bool IsAtBottom => CurHeight <= minHeight;

    public float DistanceTraveled => state == ElevatorState.Idle ? 0 : (direction > 0 ? CurHeight - minHeight : maxHeight - CurHeight);
    public float DistanceRemaining => TotalHeight - DistanceTraveled;
    private float TotalHeight => maxHeight - minHeight;
    
    public float CurHeight {
        get => transform.localPosition.y;
        set {
            if (CurHeight.IsApproximately(value)) return;
            
            Vector3 positionBefore = transform.localPosition;
            transform.localPosition = transform.localPosition.WithY(Mathf.Clamp(value, minHeight, maxHeight));

            Vector3 worldOffset = transform.TransformVector(transform.localPosition - positionBefore);

            if (triggerZone) {
                foreach (Collider otherObject in triggerZone.objectsInZone) {
                    Rigidbody maybeRigidbody = triggerZone.rigidbodiesInZone[otherObject];
                    if (maybeRigidbody) {
                        maybeRigidbody.MovePosition(maybeRigidbody.position + worldOffset);
                    }
                    else {
                        otherObject.transform.position += worldOffset;
                    }
                }

                if (triggerZone.playerInZone) {
                    Player.instance.transform.position += worldOffset;
                    Player.instance.cameraFollow.RecalculateWorldPositionLastFrame();
                }
            }
        }
    }

    protected override void Awake() {
        base.Awake();

        if (triggerZone == null) {
            Debug.LogWarning($"Element {name} is missing a TriggerOverlapZone reference. Player movement will not be affected by this elevator.");
        }

        state = this.StateMachine(ElevatorState.Idle, true);
        
        // Elevator should go to Idle state when it reaches the top or bottom
        state.AddStateTransition(ElevatorState.Moving, ElevatorState.Idle, () => (direction > 0 && IsAtTop) || (direction < 0 && IsAtBottom));
        
        // When the elevator is idle, it should move to the closest of the two heights (unless those aren't nearby)
        state.AddTrigger(ElevatorState.Idle, () => {
            float minOrMaxHeight = CurHeight.CloserOfTwo(minHeight, maxHeight);

            if (Mathf.Abs(CurHeight - minOrMaxHeight) < 0.2f) {
                CurHeight = minOrMaxHeight;
            }
        });
        
        // Move the elevator up or down
        state.WithUpdate(ElevatorState.Moving, _ => {
            curVelocity = Mathf.Lerp(curVelocity, DesiredVelocity, (Mathf.Approximately(Mathf.Sign(curVelocity), Mathf.Sign(direction)) ? acceleration : changeDirectionAcceleration) * Time.deltaTime);
            
            debug.Log($"Velocity: {curVelocity}\nSpeedMultiplier: {SpeedMultiplier}");
            CurHeight += curVelocity * Time.deltaTime;
        });
    }

#region Saving
		[Serializable]
		public class ElevatorSave : SerializableSaveObject<Elevator> {
            private StateMachine<ElevatorState>.StateMachineSave stateSave;
            
			public ElevatorSave(Elevator script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(Elevator script) {
                script.state.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
