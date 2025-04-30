using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using SerializableClasses;
using StateUtils;

[RequireComponent(typeof(UniqueId))]
public class WeirdLaserPillar : SuperspectiveObject<WeirdLaserPillar, WeirdLaserPillar.WeirdLaserPillarSave> {
	public PowerTrail powerTrailToReactTo;
	ParticleSystem ps;

	public enum State : byte {
        Off,
        On
    }
    public StateMachine<State> state;

    private const float POWER_PROPAGATION_SPEED = 9.5f;
    private float InitialDelay => Mathf.Max(0f, (Vector3.Distance(transform.position, powerTrailToReactTo.transform.position) - 17.5f) / POWER_PROPAGATION_SPEED);
    private const float AIR_WHOOSH_CRESCENDO_TIME = 2f;
    private const float LASER_LOOP_DELAY = 1.215f;
    private const float CAMERA_SHAKE_TIME = 2f;
    private const float CAMERA_SHAKE_START_INTENSITY = 3.75f;

    protected override void Start() {
        base.Start();

        state = this.StateMachine(State.Off);

        ps = GetComponentInChildren<ParticleSystem>();

        powerTrailToReactTo.pwr.OnPowerBegin += () => state.Set(State.On);
        
        state.AddTrigger(State.On, InitialDelay, () => AudioManager.instance.PlayAtLocation(AudioName.AirWhoosh, ID, transform.position));
        state.AddTrigger(State.On, InitialDelay + AIR_WHOOSH_CRESCENDO_TIME, () => {
	        // Debug.Log($"Multiple: {cameraShakeMultiplier}, Distance: {distanceFromPlayer}");
	        CameraShake.instance.Shake(transform.position, CAMERA_SHAKE_START_INTENSITY, CAMERA_SHAKE_TIME);
	        AudioManager.instance.PlayAtLocation(AudioName.LaserLoopStart, ID, transform.position);
	        ps.Play();
        });
        state.AddTrigger(State.On, InitialDelay + AIR_WHOOSH_CRESCENDO_TIME + LASER_LOOP_DELAY, () => AudioManager.instance.PlayAtLocation(AudioName.LaserLoop, ID, transform.position));
    }

    private void Update() {
	    switch (state.State) {
		    case State.Off:
			    if (ps.isPlaying) {
				    ps.Stop();
			    }
			    break;
		    case State.On:
			    break;
		    default:
			    throw new ArgumentOutOfRangeException();
	    }
    }

#region Saving

	public override void LoadSave(WeirdLaserPillarSave save) {
		state.LoadFromSave(save.stateSave);
		ps.LoadFromSerializable(save.particleSystemSave);
	}

	[Serializable]
	public class WeirdLaserPillarSave : SaveObject<WeirdLaserPillar> {
        public StateMachineSave<State> stateSave;
        public SerializableParticleSystem particleSystemSave;
        
		public WeirdLaserPillarSave(WeirdLaserPillar script) : base(script) {
            this.stateSave = script.state.ToSave();
            this.particleSystemSave = script.ps;
		}
	}
#endregion
}
