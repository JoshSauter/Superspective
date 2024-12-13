using System;
using Audio;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class ChasmElevator : SaveableObject<ChasmElevator, ChasmElevator.ChasmElevatorSave>, CustomAudioJob {
    public Collider playerUnderElevatorHitbox;
    public ChasmElevatorHandle handle;
    public Elevator elevator;
    public Transform[] crossBeams;

    private int Direction {
	    get {
		    var effectiveHandleState = handle.handleState.State;
		    if (effectiveHandleState == ChasmElevatorHandle.HandleState.HeldByPlayer) {
			    effectiveHandleState = handle.handleState.PrevState;
		    }

		    return effectiveHandleState == ChasmElevatorHandle.HandleState.Down ? -1 : 1;
	    }
    }

    private const float CROSS_BEAMS_MIN_SIZE = 0.74f;
    private const float SEND_BACK_DOWN_DELAY = 4.25f;
    private const float ELEVATOR_SPEED = 4.5f;
    private const float ELEVATOR_STARTUP_TIME = 1.5f;
    private const float CAMERA_SHAKE_INTENSITY = 2.5f;

    private bool HandleUp => handle.handleState.State == ChasmElevatorHandle.HandleState.Up;
    private bool HandleDown => handle.handleState.State == ChasmElevatorHandle.HandleState.Down;

    private bool PlayerUnderElevator => Direction < 0 && playerUnderElevatorHitbox.bounds.Contains(Player.instance.transform.position);
    private bool ShouldBeMoving => !PlayerUnderElevator && ((HandleUp && !elevator.IsAtTop) || (HandleDown && !elevator.IsAtBottom));

    // If set to true, will explicitly add the state transition for sending the elevator back down when it reaches the top
    // If set to false, will only add the state transition when ManuallySendElevatorBackDownForCutscene is called
    public bool automaticallySendDownWhenAtTop = false;
    private bool ShouldAutomaticallySendBackDownNow => elevator.IsAtTop && elevator.state == Elevator.ElevatorState.Idle && elevator.state.Time > SEND_BACK_DOWN_DELAY && !elevator.triggerZone.playerInZone;

    protected override void Start() {
        base.Awake();

        elevator.speed = ELEVATOR_SPEED;
        InitializeStateMachine();
    }

    /// <summary>
    /// Adds the state transition for sending the elevator back down when it reaches the top, which should trigger it immediately as well
    /// This function gets called when the player walks into the elevator trigger zone before reaching the level proper
    /// </summary>
    public void ManuallySendElevatorBackDownForCutscene() {
	    handle.handleState.AddStateTransition(ChasmElevatorHandle.HandleState.Up, ChasmElevatorHandle.HandleState.Down, () => ShouldAutomaticallySendBackDownNow);
    }

    private void InitializeStateMachine() {
        // Tell the elevator to move when the handle is up and the elevator is not at the top, or vice versa
        elevator.state.AddStateTransition(Elevator.ElevatorState.Idle, Elevator.ElevatorState.Moving, () => ShouldBeMoving);
        
        // Handle the player sneaking under the elevator
        elevator.state.AddStateTransition(Elevator.ElevatorState.Moving, Elevator.ElevatorState.Idle, () => PlayerUnderElevator);
        
        // Apply camera shake when we start moving
        elevator.state.AddTrigger(Elevator.ElevatorState.Moving, () => {
	        AudioManager.instance.PlayWithUpdate(AudioName.MachineClick, ID, this);
	        AudioManager.instance.PlayWithUpdate(AudioName.ElectricalHum, ID, this);
	        CameraShake.instance.Shake(() => transform.position, CAMERA_SHAKE_INTENSITY, ELEVATOR_STARTUP_TIME);
        });
        
        elevator.state.AddTrigger(Elevator.ElevatorState.Idle, () => {
	        AudioManager.instance.PlayWithUpdate(AudioName.MachineOff, ID, this);
        });
        
        // Keep the elevator moving in the correct direction
        elevator.state.WithUpdate(Elevator.ElevatorState.Moving, _ => {
	        elevator.direction = Direction;

	        foreach (Transform crossBeam in crossBeams) {
		        crossBeam.localScale = crossBeam.localScale.WithZ(Mathf.Lerp(CROSS_BEAMS_MIN_SIZE, 1f, elevator.SpeedMultiplier));
	        }
        });

        if (automaticallySendDownWhenAtTop) {
	        ManuallySendElevatorBackDownForCutscene();
        }
    }

    public void UpdateAudioJob(AudioManager.AudioJob job) {
	    job.audio.transform.position = transform.position;
	    if (job.audioName == AudioName.ElectricalHum) {
		    job.audio.volume = Mathf.InverseLerp(0, elevator.speed, Mathf.Abs(elevator.curVelocity));
	    }
    }
    
#region Saving
		[Serializable]
		public class ChasmElevatorSave : SerializableSaveObject<ChasmElevator> {
            
			public ChasmElevatorSave(ChasmElevator script) : base(script) {
			}

			public override void LoadSave(ChasmElevator script) {
			}
		}
#endregion
}
