using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
	public abstract class SoundEffect : MonoBehaviour {
		[Range(-2f, 2f)]
		public float pitch = 1f;
		[Range(0f, 1f)]
		public float randomizePitch;

		[ShowNativeProperty]
		public string soundName {
			get {
				if (audioSource != null && audioSource.clip != null) {
					return audioSource.clip.name;
				}
				else return "";
			}
		}
		public abstract AudioSource audioSource { get; }

		[Button("Play Sound")]
		public abstract void Play(bool shouldForcePlay = false, float playAt = 0.0f);

		public abstract void PlayOneShot();

		[Button("Stop Sound")]
		public abstract void Stop();

		protected float GetPitch() {
			return pitch + Random.Range(-randomizePitch, randomizePitch);
		}
	}
}