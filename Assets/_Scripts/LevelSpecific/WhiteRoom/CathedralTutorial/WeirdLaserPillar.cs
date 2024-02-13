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
public class WeirdLaserPillar : SaveableObject<WeirdLaserPillar, WeirdLaserPillar.WeirdLaserPillarSave> {
	public PowerTrail powerTrailToReactTo;
	ParticleSystem ps;

	public enum State {
        Off,
        On
    }
    public StateMachine<State> state;

    private const float powerPropagationSpeed = 9.5f;
    private float initialDelay => Mathf.Max(0f, (Vector3.Distance(transform.position, powerTrailToReactTo.transform.position) - 17.5f) / powerPropagationSpeed);
    private const float airWhooshCrescendoTime = 2f;
    private const float laserLoopDelay = 1.215f;
    private const float cameraShakeTime = 2f;
    private const float cameraShakeStartIntensity = 3.75f;
    private const float cameraShakeStartIntensityMultiplierMin = .05f;

    private float cameraShakeMultiplier => Mathf.Lerp(cameraShakeStartIntensityMultiplierMin, 1,
	    1-Mathf.InverseLerp(5f, 40f, distanceFromPlayer));

    private float distanceFromPlayer => Vector3.Distance(Player.instance.PlayerCam.transform.position, transform.position);

    protected override void Start() {
        base.Start();

        state = this.StateMachine(State.Off);

        ps = GetComponentInChildren<ParticleSystem>();

        powerTrailToReactTo.pwr.OnPowerBegin += () => state.Set(State.On);
        
        state.AddTrigger(State.On, initialDelay, () => AudioManager.instance.PlayAtLocation(AudioName.AirWhoosh, ID, transform.position));
        state.AddTrigger(State.On, initialDelay + airWhooshCrescendoTime, () => {
	        // Debug.Log($"Multiple: {cameraShakeMultiplier}, Distance: {distanceFromPlayer}");
	        CameraShake.instance.Shake(cameraShakeTime, cameraShakeStartIntensity*cameraShakeMultiplier, 0f);
	        AudioManager.instance.PlayAtLocation(AudioName.LaserLoopStart, ID, transform.position);
	        ps.Play();
        });
        state.AddTrigger(State.On, initialDelay + airWhooshCrescendoTime + laserLoopDelay, () => AudioManager.instance.PlayAtLocation(AudioName.LaserLoop, ID, transform.position));
    }

    private void Update() {
	    switch (state.state) {
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
		[Serializable]
		public class WeirdLaserPillarSave : SerializableSaveObject<WeirdLaserPillar> {
            private StateMachine<State>.StateMachineSave stateSave;
            private SerializableParticleSystem particleSystemSave;
            
			public WeirdLaserPillarSave(WeirdLaserPillar script) : base(script) {
                this.stateSave = script.state.ToSave();
                this.particleSystemSave = script.ps;
			}

			public override void LoadSave(WeirdLaserPillar script) {
                script.state.LoadFromSave(this.stateSave);
                this.particleSystemSave.ApplyToParticleSystem(script.ps);
			}
		}
#endregion
}
