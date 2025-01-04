using System.Collections;
using System.Collections.Generic;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

namespace Audio {
	[RequireComponent(typeof(Rigidbody))]
    public class CubeImpactSound : SuperspectiveObject, AudioJobOnGameObject {
	    
		float minSpeed = 5f;
		float maxSpeed = 25f;

		float curVolume = 0f;
		float minVolume = .15f;
		float maxVolume = 4.5f;

		float minPitch = 0.6f;
		float maxPitch = 1.2f;

		float timeSinceLastSound = 0f;
		float cooldown = 0.05f;

		private AudioManager.AudioJob audioJob;

		void OnCollisionEnter(Collision collision) {
			if (!GameManager.instance.gameHasLoaded) return;
			float impactSpeed = Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, maxSpeed);
			float impactLerpSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, impactSpeed);
			//Debug.Log(impactSpeed);
			float nextVolume = Mathf.Lerp(minVolume, maxVolume, impactLerpSpeed);

			bool shouldPlay = timeSinceLastSound > cooldown || nextVolume > curVolume;

			void AudioSettingsOverride(AudioManager.AudioJob audio) {
				audio.audio.volume = nextVolume;
				audio.basePitch = Mathf.Lerp(minPitch, maxPitch, impactLerpSpeed);
			}

			PrintDebugCollisionInfo(collision, impactSpeed);
			audioJob = AudioManager.instance.PlayOnGameObject(AudioName.CubeImpact, id.uniqueId, this, shouldPlay, AudioSettingsOverride);
			timeSinceLastSound = 0f;
			curVolume = nextVolume;
		}

		void PrintDebugCollisionInfo(Collision collision, float impactSpeed) {
			string gameObjectName = gameObject.FullPath();
			string gameObjectLayer = LayerMask.LayerToName(gameObject.layer);
			string otherObjectName = collision.collider.gameObject.FullPath();
			string otherObjectLayer = LayerMask.LayerToName(collision.collider.gameObject.layer);
			string speed = $"{impactSpeed:F1}";
			debug.LogWithContext($"({gameObjectName}, {gameObjectLayer}) collided with ({otherObjectName}, {otherObjectLayer}) at speed {impactSpeed}", collision.collider.gameObject);
		}

		void Update() {
			timeSinceLastSound += Time.deltaTime;
		}

		protected override void OnDisable() {
			base.OnDisable();

			audioJob?.Stop();
		}

		public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;
    }
}
