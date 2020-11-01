using NaughtyAttributes;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
	[RequireComponent(typeof(UniqueId))]
	public abstract class SoundEffect : MonoBehaviour, SaveableObject {
		public bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }
		UniqueId _id;
		UniqueId id {
			get {
				if (_id == null) {
					_id = GetComponent<UniqueId>();
				}
				return _id;
			}
		}

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
			return pitch + UnityEngine.Random.Range(-randomizePitch, randomizePitch);
		}

		#region Saving
		public string ID => $"{soundName}_SoundEffect_{id.uniqueId}";

		[Serializable]
		class SoundEffectSave {
			bool isPlaying;
			float normalizedTime;
			float pitch;
			float randomizePitch;

			public SoundEffectSave(SoundEffect sound) {
				if (sound.audioSource != null) {
					this.isPlaying = sound.audioSource.isPlaying;
					this.normalizedTime = sound.audioSource.time / sound.audioSource.clip.length;
				}
				else {
					this.isPlaying = false;
					this.normalizedTime = 0f;
				}
				this.pitch = sound.pitch;
				this.randomizePitch = sound.randomizePitch;
			}

			public void LoadSave(SoundEffect obj) {
				obj.pitch = this.pitch;
				obj.randomizePitch = this.randomizePitch;
				if (this.isPlaying && this.normalizedTime > 0) {
					obj.Play(true, this.normalizedTime);
				}
			}
		}

		public object GetSaveObject() {
			return new SoundEffectSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			SoundEffectSave save = savedObject as SoundEffectSave;

			save.LoadSave(this);
		}
		#endregion
	}
}