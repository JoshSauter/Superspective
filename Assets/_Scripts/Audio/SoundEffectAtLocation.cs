using EpitaphUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
    public class SoundEffectAtLocation : SoundEffect {
		private AudioSource locationGameObjectPrefab;

		[SerializeField]
		private AudioSource _audioSourceOnGameObject;
		[SerializeField]
		private AudioSource _audioSourcePlayingAtLocation;
		public override AudioSource audioSource => _audioSourceOnGameObject;
		public Vector3 location;

		private bool _initialized = false;

		void Init() {
			if (_initialized) return;

			if (_audioSourceOnGameObject == null) {
				_audioSourceOnGameObject = gameObject.AddComponent<AudioSource>();
			}
			if (locationGameObjectPrefab == null) {
				locationGameObjectPrefab = Resources.Load<AudioSource>("Prefabs/Audio/SoundEffectAtLocation");
			}

			_initialized = true;
		}

		private void Awake() {
			if (!_initialized) {
				Init();
			}
		}

		private void OnValidate() {
			if (!_initialized) {
				Init();
			}
		}

		private void Update() {
			UpdateAudioSourceSettings();
		}

		void UpdateAudioSourceSettings() {
			if (_audioSourceOnGameObject == null || _audioSourcePlayingAtLocation == null) {
				return;
			}

			_audioSourcePlayingAtLocation.clip = _audioSourceOnGameObject.clip;
			CopyAudioSourceSettings(from: ref _audioSourceOnGameObject, to: ref _audioSourcePlayingAtLocation);
			_audioSourcePlayingAtLocation.transform.position = location;
		}

		public override void PlayOneShot() {
			if (audioSource.loop) {
				Debug.LogError("Use Play() instead of PlayOneShot() for looping sounds", gameObject);
			}
			_audioSourcePlayingAtLocation.PlayOneShot(_audioSourcePlayingAtLocation.clip);
			StartCoroutine(PlaySound(_audioSourcePlayingAtLocation));
		}

		private void PlaySound() {
			_audioSourcePlayingAtLocation.Play();
			StartCoroutine(PlaySound(_audioSourcePlayingAtLocation));
		}

		public override void Play(bool shouldForcePlay = false) {
			if (_audioSourcePlayingAtLocation == null) {
				_audioSourcePlayingAtLocation = SimplePool.instance.Spawn(locationGameObjectPrefab, location, new Quaternion());
			}

			if (!_audioSourcePlayingAtLocation.isPlaying || shouldForcePlay) {
				UpdateAudioSourceSettings();
				_audioSourcePlayingAtLocation.pitch = GetPitch();
				PlaySound();
			}
		}

		public override void Stop() {
			if (_audioSourcePlayingAtLocation != null) {
				_audioSourcePlayingAtLocation.Stop();
			}
		}

		IEnumerator PlaySound(AudioSource source) {
			yield return new WaitUntil(() => source.isPlaying);
			yield return new WaitUntil(() => source == null || !source.isPlaying);

			_audioSourcePlayingAtLocation = null;
			SimplePool.instance.Despawn(source);
		}

		void CopyAudioSourceSettings(ref AudioSource from, ref AudioSource to) {
			to.clip = from.clip;
			to.playOnAwake = from.playOnAwake;
			to.loop = from.loop;
			to.priority = from.priority;
			to.volume = from.volume;
			to.pitch = from.pitch;
			to.panStereo = from.panStereo;
			to.spatialBlend = from.spatialBlend;
			to.reverbZoneMix = from.reverbZoneMix;
			to.dopplerLevel = from.dopplerLevel;
			to.spread = from.spread;
			to.rolloffMode = from.rolloffMode;
			to.minDistance = from.minDistance;
			to.maxDistance = from.maxDistance;
		}
	}
}
