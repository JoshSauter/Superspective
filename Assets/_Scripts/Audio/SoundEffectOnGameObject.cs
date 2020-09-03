using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
	public class SoundEffectOnGameObject : SoundEffect {

		[SerializeField]
		private AudioSource _audioSource;
		public override AudioSource audioSource {
			get {
				if (_audioSource == null) {
					_audioSource = gameObject.AddComponent<AudioSource>();
				}
				return _audioSource;
			}
		}

		private bool _initialized = false;

		void Init() {
			if (_initialized) return;

			if (_audioSource == null) {
				_audioSource = gameObject.AddComponent<AudioSource>();
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

		public override void Play(bool shouldForcePlay = false) {
			audioSource.pitch = GetPitch();
			if (!audioSource.isPlaying || shouldForcePlay) {
				audioSource.Play();
			}
		}

		public override void PlayOneShot() {
			if (audioSource.loop) {
				Debug.LogError("Use Play() instead of PlayOneShot() for looping sounds", gameObject);
			}
			audioSource.pitch = GetPitch();
			audioSource.PlayOneShot(audioSource.clip);
		}

		public override void Stop() {
			audioSource.Stop();
		}
	}
}