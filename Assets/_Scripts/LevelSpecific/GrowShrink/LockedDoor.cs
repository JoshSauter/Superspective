using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using NaughtyAttributes;
using PoweredObjects;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class LockedDoor : SaveableObject<LockedDoor, LockedDoor.LockedDoorSave> {
    public enum TriggerCondition {
        PoweredObject,
        PowerTrailDistance
    }
    public TriggerCondition triggerCondition = TriggerCondition.PoweredObject;
    public bool TriggeredByPowerObj => triggerCondition == TriggerCondition.PoweredObject;
    public bool TriggeredByPowerTrailDistance => triggerCondition == TriggerCondition.PowerTrailDistance;
    
    public enum State {
        Closed,
        Opening,
        Open,
        Closing
    }

    public bool ClosingOrClosed => state.State is State.Closing or State.Closed;
    public bool OpeningOrOpen => state.State is State.Opening or State.Open;
    public StateMachine<State> state;
    public Collider invisibleCollider;
    public Transform doorLeft, doorRight;

    // Config
    private const float cameraShakeIntensity = 5f;
    private const float cameraLongShakeIntensity = 1.25f;
    private const float cameraShakeDuration = .25f;
    private const float portalMovingSoundDelay = .35f;
    public float maxOffset = 3f; // Max distance for doors to open (total opening width is this value * 2)
    private float OpenCloseSpeed => maxOffset / OPEN_CLOSE_TIME;
    private const float OPEN_CLOSE_TIME = 3.75f;

    [ShowIf(nameof(TriggeredByPowerObj))]
    public SerializableReference<PoweredObject, PoweredObject.PoweredObjectSave> poweredFrom;
    [ShowIf(nameof(TriggeredByPowerTrailDistance))]
    public SerializableReference<PowerTrail, PowerTrail.PowerTrailSave> powerTrail;
    [ShowIf(nameof(TriggeredByPowerTrailDistance))]
    public float powerTrailDistance;

    protected override void Awake() {
        base.Awake();

        state = this.StateMachine(State.Closed);
    }

    protected override void Init() {
        base.Init();

        switch (triggerCondition) {
            case TriggerCondition.PoweredObject:
                PoweredObject powerFrom = poweredFrom.GetOrNull();
                if (powerFrom) {
                    powerFrom.OnPowerFinish += OpenDoor;
                    powerFrom.OnDepowerBegin += CloseDoor;
                }
                break;
            case TriggerCondition.PowerTrailDistance:
                PowerTrail pwrTrail = powerTrail.GetOrNull();
                if (pwrTrail) {
                    pwrTrail.OnPowerTrailUpdate += (prevDistance, newDistance) => {
                        if (prevDistance < powerTrailDistance && newDistance >= powerTrailDistance) {
                            OpenDoor();
                        }
                        else if (prevDistance >= powerTrailDistance && newDistance < powerTrailDistance) {
                            CloseDoor();
                        }
                    };
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        InitializeStateMachine();
    }

    void InitializeStateMachine() {
        state.AddStateTransition(State.Opening, State.Open, () => DoorOffset >= maxOffset);
        state.AddStateTransition(State.Closing, State.Closed, () => DoorOffset <= 0);
        
        state.AddTrigger(State.Closed, () => {
            if (state.PrevState != State.Closing) return;
            
            CameraShake.instance.Shake(transform.position, cameraShakeIntensity, cameraShakeDuration);
            AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingEnd, ID, transform.position);
        });
        
        state.AddTrigger(State.Open, () => {
            if (state.PrevState != State.Opening) return;
            
            CameraShake.instance.Shake(transform.position, cameraShakeIntensity, cameraShakeDuration);
            AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingEnd, ID, transform.position);
        });
        
        state.AddTrigger(State.Closing, () => {
            if (state.PrevState != State.Open) return;
            
            CameraShake.instance.Shake(transform.position, cameraShakeIntensity, cameraShakeDuration);
            AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
        });
        
        state.AddTrigger(State.Opening, () => {
            if (state.PrevState != State.Closed) return;
            
            CameraShake.instance.Shake(transform.position, cameraShakeIntensity, cameraShakeDuration);
            AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMovingStart, ID, transform.position);
        });
        state.AddTrigger((enumValue) => enumValue is State.Closing or State.Opening, portalMovingSoundDelay, () => {
            CameraShake.CameraShakeEvent shake = new CameraShake.CameraShakeEvent() {
                duration = OPEN_CLOSE_TIME - portalMovingSoundDelay,
                intensity = cameraLongShakeIntensity,
                intensityCurve = AnimationCurve.Constant(0, 1, 1),
                spatial = 1,
                locationProvider = () => transform.position
            };
            CameraShake.instance.Shake(shake);
            AudioManager.instance.PlayAtLocation(AudioName.PortalDoorMoving, ID, transform.position);
        });
        
        state.OnStateChangeSimple += () => invisibleCollider.enabled = ClosingOrClosed;
    }

    public void OpenDoor() {
        if (OpeningOrOpen) return;
        
        state.Set(State.Opening);
    }

    public void CloseDoor() {
        if (ClosingOrClosed) return;
        
        state.Set(State.Closing);
    }

    public float DoorOffset {
        get => doorRight.localPosition.x;
        set {
            doorLeft.localPosition = doorLeft.localPosition.WithX(Mathf.Clamp(-value, -maxOffset, 0));
            doorRight.localPosition = doorRight.localPosition.WithX(Mathf.Clamp(value, 0, maxOffset));
        }
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        switch (state.State) {
            case State.Closed:
                DoorOffset = 0;
                break;
            case State.Opening:
                DoorOffset += Time.deltaTime * OpenCloseSpeed;
                break;
            case State.Open:
                DoorOffset = maxOffset;
                break;
            case State.Closing:
                DoorOffset -= Time.deltaTime * OpenCloseSpeed;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
#region Saving
		[Serializable]
		public class LockedDoorSave : SerializableSaveObject<LockedDoor> {
            private StateMachine<State>.StateMachineSave stateSave;
            private float curOffset;
            
			public LockedDoorSave(LockedDoor script) : base(script) {
                this.stateSave = script.state.ToSave();
                this.curOffset = script.DoorOffset;
            }

			public override void LoadSave(LockedDoor script) {
                script.state.LoadFromSave(this.stateSave);
                script.DoorOffset = curOffset;
            }
		}
#endregion
}
