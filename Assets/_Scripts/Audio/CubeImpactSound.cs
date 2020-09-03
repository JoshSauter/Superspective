using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
	[RequireComponent(typeof(Rigidbody))]
    public class CubeImpactSound : MonoBehaviour {
        public SoundEffect cubeImpactSounds;

		float minSpeed = 5f;
		float maxSpeed = 25f;

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
			float volume = Mathf.Lerp(minVolume, maxVolume, impactLerpSpeed);
			float prevVolume = cubeImpactSounds.audioSource.volume;
			cubeImpactSounds.audioSource.volume = volume;
			cubeImpactSounds.pitch = Mathf.Lerp(minPitch, maxPitch, impactLerpSpeed);

			bool shouldPlay = timeSinceLastSound > cooldown || volume > prevVolume;
			cubeImpactSounds.Play(shouldPlay);
			timeSinceLastSound = 0f;
		}

		private void Update() {
			timeSinceLastSound += Time.deltaTime;
		}
	}
}