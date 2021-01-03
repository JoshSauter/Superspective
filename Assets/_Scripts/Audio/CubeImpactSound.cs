using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
	[RequireComponent(typeof(Rigidbody))]
    public class CubeImpactSound : MonoBehaviour {
		UniqueId _id;
		UniqueId id {
			get {
				if (_id == null) {
					_id = GetComponent<UniqueId>();
				}
				return _id;
			}
		}

		float minSpeed = 5f;
		float maxSpeed = 25f;

		float curVolume = 0f;
		float minVolume = .15f;
		float maxVolume = 4.5f;

		float minPitch = 0.6f;
		float maxPitch = 1.2f;

		float speedToSwitchSound = 15f;
		float timeSinceLastSound = 0f;
		float cooldown = 0.05f;

		private void OnCollisionEnter(Collision collision) {
			float impactSpeed = Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, maxSpeed);
			float impactLerpSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, impactSpeed);
			//Debug.Log(impactSpeed);
			float nextVolume = Mathf.Lerp(minVolume, maxVolume, impactLerpSpeed);

			bool shouldPlay = timeSinceLastSound > cooldown || nextVolume > curVolume;

			void AudioSettingsOverride(AudioSource audio) {
				audio.volume = nextVolume;
				audio.pitch = Mathf.Lerp(minPitch, maxPitch, impactLerpSpeed);
			}

			AudioManager.instance.PlayOnGameObject(AudioName.CubeImpact, id.uniqueId, gameObject, shouldPlay, AudioSettingsOverride);
			timeSinceLastSound = 0f;
			curVolume = nextVolume;
		}

		private void Update() {
			timeSinceLastSound += Time.deltaTime;
		}
	}
}