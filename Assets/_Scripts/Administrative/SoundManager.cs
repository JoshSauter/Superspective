using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;

public enum SoundType {
	EffectAtLocation,
	EffectOnGameObject,
	Music
}

[System.Serializable]
public class SoundSettings {
	public SoundType type;
	public Vector3 locationOfSound;
	public GameObject gameObjectAttachedTo;
	public AudioClip sound;
	public bool playOnAwake = true;
	public bool loop = false;
	[Range(0, 256)]
	public int priority = 128;
	[Range(0f, 1f)]
	public float volume = 1f;
	[Range(-3f, 3f)]
	public float pitch = 1f;
	[Range(0f, 1f)]
	public float randomizePitch = 0f;
	[Range(-1f, 1f)]
	public float stereoPan = 0f;
	[Range(0f, 1f)]
	public float spatialBlend = 0f;
	[Range(0f, 5f)]
	public float dopplerLevel = 1f;
	[Range(0, 360)]
	public int spread = 0;
	public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
	public float minDistance = 1f;
	public float maxDistance = 500f;
}
[ExecuteInEditMode]
public class SoundManager : Singleton<SoundManager> {
	public bool DEBUG = false;
	DebugLogger debug;
	Dictionary<string, AudioSource> knownAudioSources;

	private void OnEnable() {
		debug = new DebugLogger(gameObject, () => DEBUG);
		knownAudioSources = new Dictionary<string, AudioSource>();
	}

	//public AudioSource GetAvailableAudioSource() {
	//	if (availableAudioSources.Count > 0) {
	//		return availableAudioSources.Dequeue();
	//	}
	//	else {
	//		return gameObject.AddComponent<AudioSource>();
	//	}
	//}

	public void Play(string soundName, SoundSettings soundSettings, bool forcePlay = false) {
		if (knownAudioSources == null) {
			knownAudioSources = new Dictionary<string, AudioSource>();
		}
		AudioSource audioSource;
		if (knownAudioSources.ContainsKey(soundName)) {
			audioSource = knownAudioSources[soundName];
			FillAudioSourceFromSettings(ref audioSource, soundSettings);
		}
		else {
			audioSource = AudioSourceFromSettings(soundSettings);
			knownAudioSources[soundName] = audioSource;
		}
		if (!audioSource.isPlaying || forcePlay) {
			debug.Log($"Playing {soundName} (forced:{forcePlay})");
			StartCoroutine(PlayAudio(soundName, audioSource, soundSettings));
		}
	}

	private AudioSource AudioSourceFromSettings(SoundSettings soundSettings) {
		switch (soundSettings.type) {
			case SoundType.EffectAtLocation:
				Debug.LogError("Should not call AudioSourceFromSettings for EffectAtLocation: " + soundSettings);
				return null;
			case SoundType.EffectOnGameObject: {
				if (soundSettings.gameObjectAttachedTo == null) {
					Debug.LogError("No gameobject set for EffectOnGameObject: " + soundSettings);
					return null;
				}
				AudioSource newAudioSource = soundSettings.gameObjectAttachedTo.GetComponent<AudioSource>();
				if (newAudioSource == null) {
					newAudioSource = soundSettings.gameObjectAttachedTo.AddComponent<AudioSource>();
				}
				FillAudioSourceFromSettings(ref newAudioSource, soundSettings);
				return newAudioSource;
			}
			case SoundType.Music: {
				AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
				FillAudioSourceFromSettings(ref newAudioSource, soundSettings);
				return newAudioSource;
			}
			default:
				Debug.LogError("Unhandled case wtf");
				return null;
		}
	}

	private void FillAudioSourceFromSettings(ref AudioSource source, SoundSettings settings) {
		source.clip = settings.sound;
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

	IEnumerator PlayAudio(string name, AudioSource audioSource, SoundSettings settings) {
		//Debug.Log("Playing audio: " + name);
		float randomPitch = settings.pitch + Random.Range(-settings.randomizePitch, settings.randomizePitch);
		audioSource.pitch = randomPitch;
		audioSource.Play();

		yield return new WaitUntil(() => audioSource.isPlaying);
		yield return new WaitUntil(() => !audioSource.isPlaying);

		//Debug.Log("Finished playing audio: " + name);
	}
}
