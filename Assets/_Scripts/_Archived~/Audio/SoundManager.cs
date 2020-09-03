using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;

namespace Audio {
	[ExecuteInEditMode]
	public class SoundManager : Singleton<SoundManager> {
		public bool DEBUG = false;
		public SoundEffectAtLocation effectAtLocationPrefab;
		DebugLogger debug;
		Dictionary<string, AudioSource> musicAudioSources;
		Dictionary<SoundSettings, AudioSource> audioPlaying;

		[ShowNativeProperty]
		public int numSoundsPlaying => audioPlaying != null ? audioPlaying.Count : 0;

		private void OnEnable() {
			debug = new DebugLogger(gameObject, () => DEBUG);
			if (musicAudioSources == null) {
				musicAudioSources = new Dictionary<string, AudioSource>();
			}

			if (audioPlaying == null) {
				audioPlaying = new Dictionary<SoundSettings, AudioSource>();
			}
		}

		private void OnDisable() {
			foreach (var musicAudioSource in musicAudioSources.Values) {
				Destroy(musicAudioSource);
			}
			musicAudioSources = null;

			foreach (var audio in audioPlaying.Values) {
				if (audio != null) {
					audio.Stop();
				}
			}
			audioPlaying = null;
		}

		//public AudioSource GetAvailableAudioSource() {
		//	if (availableAudioSources.Count > 0) {
		//		return availableAudioSources.Dequeue();
		//	}
		//	else {
		//		return gameObject.AddComponent<AudioSource>();
		//	}
		//}

		public void Play(SoundSettings soundSettings, bool forcePlay = false) {
			AudioSource audioSource = AudioSourceFromSettings(soundSettings);

			FillAudioSourceFromSettings(ref audioSource, soundSettings);

			if (!audioSource.isPlaying || forcePlay) {
				debug.Log($"Playing {soundSettings.soundName} (forced:{forcePlay})");
				StartCoroutine(PlayAudio(audioSource, soundSettings));
			}
		}

		public void Stop(SoundSettings soundSettings) {
			if (audioPlaying.ContainsKey(soundSettings)) {
				audioPlaying[soundSettings].Stop();
				audioPlaying.Remove(soundSettings);
			}
			else {
				debug.LogError($"Trying to stop sound {soundSettings.soundName}, but it's not playing!");
			}
		}

		private AudioSource AudioSourceFromSettings(SoundSettings soundSettings) {
			switch (soundSettings.type) {
				case SoundType.EffectAtLocation:
					return SimplePool.instance.Spawn(effectAtLocationPrefab, soundSettings.locationOfSound, new Quaternion()).audioSource;
				case SoundType.EffectOnGameObject:
					return soundSettings.audioSourceOnGameObject;
				case SoundType.Music:
					if (!musicAudioSources.ContainsKey(soundSettings.soundName)) {
						musicAudioSources[soundSettings.soundName] = gameObject.AddComponent<AudioSource>();
					}
					return musicAudioSources[soundSettings.soundName];
				default:
					Debug.LogError("Unhandled case wtf");
					return null;
			}
		}

		private void FillAudioSourceFromSettings(ref AudioSource source, SoundSettings settings) {
			source.clip = settings.sounds[settings.soundIndex];
			source.playOnAwake = settings.playOnAwake;
			source.loop = settings.loop;
			source.priority = settings.priority;
			source.volume = settings.volume;
			source.pitch = settings.pitch;
			source.panStereo = settings.stereoPan;
			source.spatialBlend = settings.spatialBlend;
			source.dopplerLevel = settings.dopplerLevel;
			source.spread = settings.spread;
			source.rolloffMode = settings.volumeRolloff;
			source.minDistance = settings.minDistance;
			source.maxDistance = settings.maxDistance;
		}

		IEnumerator PlayAudio(AudioSource audioSource, SoundSettings settings) {
			debug.Log($"Playing audio: {settings.PrettyPrint()}");
			float randomPitch = settings.pitch + Random.Range(-settings.randomizePitch, settings.randomizePitch);
			audioSource.pitch = randomPitch;
			audioSource.Play();

			audioPlaying[settings] = audioSource;

			yield return new WaitUntil(() => audioSource.isPlaying);
			yield return new WaitUntil(() => audioSource == null || !audioSource.isPlaying);

			if (audioPlaying.ContainsKey(settings)) {
				audioPlaying.Remove(settings);
			}

			debug.Log($"Finished playing audio: {settings.soundName}");
		}
	}
}