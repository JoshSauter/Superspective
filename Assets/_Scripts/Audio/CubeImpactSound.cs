using System.Collections;
using System.Collections.Generic;
using Saving;
using UnityEngine;

namespace Audio {
	[RequireComponent(typeof(Rigidbody))]
    public class CubeImpactSound : SaveableObject, AudioJobOnGameObject {
		float minSpeed = 5f;
		float maxSpeed = 25f;

		float curVolume = 0f;
		float minVolume = .15f;
		float maxVolume = 4.5f;

		float minPitch = 0.6f;
		float maxPitch = 1.2f;

		float timeSinceLastSound = 0f;
		float cooldown = 0.05f;

		void OnCollisionEnter(Collision collision) {
			float impactSpeed = Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, maxSpeed);
			float impactLerpSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, impactSpeed);
			//Debug.Log(impactSpeed);
			float nextVolume = Mathf.Lerp(minVolume, maxVolume, impactLerpSpeed);

			bool shouldPlay = timeSinceLastSound > cooldown || nextVolume > curVolume;

			void AudioSettingsOverride(AudioSource audio) {
				audio.volume = nextVolume;
				audio.pitch = Mathf.Lerp(minPitch, maxPitch, impactLerpSpeed);
			}

			debug.LogWarning($"{gameObject.name} collided with {collision.collider.gameObject.name} at speed {impactSpeed:F1}");
			AudioManager.instance.PlayOnGameObject(AudioName.CubeImpact, id.uniqueId, this, shouldPlay, AudioSettingsOverride);
			timeSinceLastSound = 0f;
			curVolume = nextVolume;
		}

		void Update() {
			timeSinceLastSound += Time.deltaTime;
		}

		public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;
    }
}