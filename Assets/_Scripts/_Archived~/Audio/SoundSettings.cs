using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Audio {
	public enum SoundType {
		EffectAtLocation,
		EffectOnGameObject,
		Music
	}

	public enum SoundSelectionMethod {
		InOrder,			// Will select (index+1) % #sounds as next index
		Random,				// Will select random index as next index
		SelectedExternally  // Will not modify the index after playing the sound
	}

	public class SoundSettings : MonoBehaviour {
		public string soundName;
		public SoundType type;
		[ShowIf("isEffectAtLocation")]
		public Vector3 locationOfSound;
		[ShowIf("isEffectOnGameObject")]
		public AudioSource audioSourceOnGameObject;
		public AudioClip[] sounds = new AudioClip[1];
		public int soundIndex;
		[ShowIf("hasMultipleSounds")]
		public SoundSelectionMethod soundSelectionMethod = SoundSelectionMethod.SelectedExternally;
		[HideIf("isEffectOnGameObject")]
		public bool playOnAwake = false;
		[HideIf("isEffectOnGameObject")]
		public bool loop = false;
		[HideIf("isEffectOnGameObject")][Range(0, 256)]
		public int priority = 128;
		[HideIf("isEffectOnGameObject")][Range(0f, 1f)]
		public float volume = 1f;
		[Range(-3f, 3f)]
		public float pitch = 1f;
		[Range(0f, 1f)]
		public float randomizePitch = 0f;
		[HideIf("isEffectOnGameObject")][Range(-1f, 1f)]
		public float stereoPan = 0f;
		[HideIf("isEffectOnGameObject")][Range(0f, 1f)]
		public float spatialBlend = 0f;
		[HideIf("isEffectOnGameObject")][Range(0f, 5f)]
		public float dopplerLevel = 1f;
		[HideIf("isEffectOnGameObject")][Range(0, 360)]
		public int spread = 0;
		[HideIf("isEffectOnGameObject")]
		public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
		[HideIf("isEffectOnGameObject")]
		public float minDistance = 1f;
		[HideIf("isEffectOnGameObject")]
		public float maxDistance = 500f;

		// NaughtyAttributes methods
		bool isEffectAtLocation => type == SoundType.EffectAtLocation;
		bool isEffectOnGameObject => type == SoundType.EffectOnGameObject;
		bool hasMultipleSounds => sounds != null && sounds.Length > 1;

		[Button("Play sound")]
		public void PlaySound(bool forcePlay = false) {
			if (soundName == "") {
				Debug.LogError("Please give the sound a name first.");
				return;
			}
			if (isEffectOnGameObject && audioSourceOnGameObject == null) {
				audioSourceOnGameObject = gameObject.AddComponent<AudioSource>();
			}
			SoundManager.instance.Play(this, forcePlay);
			// Select next sound
			if (hasMultipleSounds) {
				switch (soundSelectionMethod) {
					case SoundSelectionMethod.Random:
						soundIndex = UnityEngine.Random.Range(0, sounds.Length - 1);
						break;
					case SoundSelectionMethod.InOrder:
						soundIndex = (soundIndex + 1) % sounds.Length;
						break;
					case SoundSelectionMethod.SelectedExternally:
						break;
				}
			}
		}

		[Button("Stop sound")]
		public void StopSound() {
			if (soundName == "") {
				Debug.LogError("Please give the sound a name first.");
				return;
			}
			SoundManager.instance.Stop(this);
		}

		private void OnEnable() {
			if (type == SoundType.EffectOnGameObject && audioSourceOnGameObject == null) {
				audioSourceOnGameObject = gameObject.AddComponent<AudioSource>();
			}
		}

		private void Start() {
			if (type == SoundType.EffectOnGameObject && audioSourceOnGameObject == null) {
				audioSourceOnGameObject = gameObject.AddComponent<AudioSource>();
			}

			if (isEffectOnGameObject && audioSourceOnGameObject != null) {
				FillSettingsFromAudioSource(audioSourceOnGameObject);
			}

			if (playOnAwake) {
				PlaySound();
			}
		}

		private void OnDisable() {
			if (audioSourceOnGameObject != null) {
				Destroy(audioSourceOnGameObject);
				audioSourceOnGameObject = null;
			}
		}

		private void Update() {
			if (isEffectOnGameObject && audioSourceOnGameObject != null) {
				FillSettingsFromAudioSource(audioSourceOnGameObject);
			}
		}

		private void FillSettingsFromAudioSource(AudioSource source) {
			playOnAwake = source.playOnAwake;
			loop = source.loop;
			priority = source.priority;
			volume = source.volume;
			stereoPan = source.panStereo;
			spatialBlend = source.spatialBlend;
			dopplerLevel = source.dopplerLevel;
			spread = (int)source.spread;
			volumeRolloff = source.rolloffMode;
			minDistance = source.minDistance;
			maxDistance = source.maxDistance;
		}

		public string PrettyPrint() {
			return $"Name: {soundName}\nType: {type}\nLocationOfSound: {locationOfSound:F3}\nSounds: {String.Join(", ", sounds.Select(s => s.name))}\nSoundIndex: {soundIndex}\nSoundSelectionMethod: {soundSelectionMethod}\nPlayOnAwake: {playOnAwake}\nLoop: {loop}\nPriority: {priority}\nVolume: {volume}\nPitch: {pitch}\nRandomizePitch: {randomizePitch}\nStereoPan: {stereoPan}\nSpatialBlend: {spatialBlend}\nDopplarLevel: {dopplerLevel}\nSpread: {spread}\nVolumeRolloff: {volumeRolloff}\nMinDistance: {minDistance}\nMaxDistance: {maxDistance}";
		}
	}
}