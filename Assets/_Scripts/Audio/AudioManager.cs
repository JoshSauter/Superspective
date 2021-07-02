using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System;
using System.Linq;
using System.Net;
using Saving;
using SerializableClasses;

namespace Audio {
	public class AudioManager : SingletonSaveableObject<AudioManager, AudioManager.AudioManagerSave> {
		[Serializable]
		public class AudioJob {
			public string id;
			public string uniqueIdentifier => id.Split('/').FirstOrDefault();
			public AudioName audioType;
			public float timeRunning = 0f;
			// audio.pitch may be modulated based on Time.timeScale or randomized pitch, this is the unmodified value
			public float basePitch = 1f;
			public float randomizedPitch = 0f;
			public AudioSource audio;
			public bool stopSound = false;

			public void Stop() {
				stopSound = true;
			}

			[Serializable]
			public class AudioJobSave {
				readonly string id;
				readonly AudioName audioType;
				readonly float timeRunning;
				readonly float basePitch;
				readonly float randomizedPitch;
				readonly bool stopSound;

				// Serialized AudioSource
				readonly float pitch;
				readonly float volume;

				readonly int timeSamples; // exact time of playback
				// outputAudioMixerGroup not saved
				readonly bool loop;
				readonly bool ignoreListenerVolume;
				readonly bool playOnAwake;
				readonly bool ignoreListenerPause;
				readonly AudioVelocityUpdateMode velocityUpdateMode;
				readonly float panStereo;
				readonly float spatialBlend;
				readonly bool spatialize;
				readonly bool spatializePostEffects;
				readonly SerializableAnimationCurve customCurveCustomRolloff;
				readonly SerializableAnimationCurve customCurveSpatialBlend;
				readonly SerializableAnimationCurve customCurveReverbZoneMix;
				readonly SerializableAnimationCurve customCurveSpread;
				readonly float reverbZoneMix;
				readonly bool bypassEffects;
				readonly bool bypassListenerEffects;
				readonly bool bypassReverbZones;
				readonly float dopplerLevel;
				readonly float spread;
				readonly int priority;
				readonly bool mute;
				readonly float minDistance;
				readonly float maxDistance;
				readonly AudioRolloffMode rolloffMode;

				readonly bool isPlaying;

				public AudioJobSave(AudioJob audioJob) {
					this.id = audioJob.id;
					this.audioType = audioJob.audioType;
					this.timeRunning = audioJob.timeRunning;
					this.basePitch = audioJob.basePitch;
					this.randomizedPitch = audioJob.randomizedPitch;
					this.stopSound = audioJob.stopSound;

					this.pitch = audioJob.audio.pitch;
					this.volume = audioJob.audio.volume;
					this.timeSamples = audioJob.audio.timeSamples;
					this.loop = audioJob.audio.loop;
					this.ignoreListenerVolume = audioJob.audio.ignoreListenerVolume;
					this.playOnAwake = audioJob.audio.playOnAwake;
					this.ignoreListenerPause = audioJob.audio.ignoreListenerPause;
					this.velocityUpdateMode = audioJob.audio.velocityUpdateMode;
					this.panStereo = audioJob.audio.panStereo;
					this.spatialBlend = audioJob.audio.spatialBlend;
					this.spatialize = audioJob.audio.spatialize;
					this.spatializePostEffects = audioJob.audio.spatializePostEffects;
					this.customCurveCustomRolloff = audioJob.audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
					this.customCurveSpatialBlend = audioJob.audio.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
					this.customCurveReverbZoneMix = audioJob.audio.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
					this.customCurveSpread = audioJob.audio.GetCustomCurve(AudioSourceCurveType.Spread);
					this.reverbZoneMix = audioJob.audio.reverbZoneMix;
					this.bypassEffects = audioJob.audio.bypassEffects;
					this.bypassListenerEffects = audioJob.audio.bypassListenerEffects;
					this.bypassReverbZones = audioJob.audio.bypassReverbZones;
					this.dopplerLevel = audioJob.audio.dopplerLevel;
					this.spread = audioJob.audio.spread;
					this.priority = audioJob.audio.priority;
					this.mute = audioJob.audio.mute;
					this.minDistance = audioJob.audio.minDistance;
					this.maxDistance = audioJob.audio.maxDistance;
					this.rolloffMode = audioJob.audio.rolloffMode;

					this.isPlaying = audioJob.audio.isPlaying;
				}

				public AudioJob LoadAudioJob() {
					AudioJob audioJob = AudioManager.instance.GetOrCreateJob(this.audioType, this.id);
					
					audioJob.id = this.id;
					audioJob.audioType = this.audioType;
					audioJob.timeRunning = this.timeRunning;
					audioJob.basePitch = this.basePitch;
					audioJob.randomizedPitch = this.randomizedPitch;
					audioJob.stopSound = this.stopSound;

					audioJob.audio.pitch = this.pitch;
					audioJob.audio.volume = this.volume;
					audioJob.audio.timeSamples = this.timeSamples;
					audioJob.audio.loop = this.loop;
					audioJob.audio.ignoreListenerVolume = this.ignoreListenerVolume;
					audioJob.audio.playOnAwake = this.playOnAwake;
					audioJob.audio.ignoreListenerPause = this.ignoreListenerPause;
					audioJob.audio.velocityUpdateMode = this.velocityUpdateMode;
					audioJob.audio.panStereo = this.panStereo;
					audioJob.audio.spatialBlend = this.spatialBlend;
					audioJob.audio.spatialize = this.spatialize;
					audioJob.audio.spatializePostEffects = this.spatializePostEffects;
					audioJob.audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, this.customCurveCustomRolloff);
					audioJob.audio.SetCustomCurve(AudioSourceCurveType.SpatialBlend, this.customCurveSpatialBlend);
					audioJob.audio.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, this.customCurveReverbZoneMix);
					audioJob.audio.SetCustomCurve(AudioSourceCurveType.Spread, this.customCurveSpread);
					audioJob.audio.reverbZoneMix = this.reverbZoneMix;
					audioJob.audio.bypassEffects = this.bypassEffects;
					audioJob.audio.bypassListenerEffects = this.bypassListenerEffects;
					audioJob.audio.bypassReverbZones = this.bypassReverbZones;
					audioJob.audio.dopplerLevel = this.dopplerLevel;
					audioJob.audio.spread = this.spread;
					audioJob.audio.priority = this.priority;
					audioJob.audio.mute = this.mute;
					audioJob.audio.minDistance = this.minDistance;
					audioJob.audio.maxDistance = this.maxDistance;
					audioJob.audio.rolloffMode = this.rolloffMode;
					
					if (this.isPlaying) audioJob.audio.Play();

					return audioJob;
				}
			}
		}

		Transform soundsRoot;

        public Dictionary<AudioName, AudioSettings> defaultSettings = new Dictionary<AudioName, AudioSettings>();

        readonly Dictionary<string, AudioJob> audioJobs = new Dictionary<string, AudioJob>();

        readonly Dictionary<string, Action<AudioJob>> updateAudioJobs = new Dictionary<string, Action<AudioJob>>();
		// These two are used for serialization and deserialization of the above Dictionary when saving
		Dictionary<string, SerializableReference> serializableCustomUpdateAudioJobs = new Dictionary<string, SerializableReference>();
		Dictionary<string, SerializableReference> serializableUpdateAudioOnGameObject = new Dictionary<string, SerializableReference>();

		protected override void Awake() {
			base.Awake();
			foreach (var template in transform.GetComponentsInChildrenRecursively<AudioTemplate>()) {
				defaultSettings.Add(template.type, template.audioSettings);
			}

			soundsRoot = transform.Find("Sounds");
		}

		void Update() {
			// Perform update actions for running audio jobs that have one
			foreach (var updateJob in updateAudioJobs) {
				AudioJob job = audioJobs[updateJob.Key];
				Action<AudioJob> update = updateJob.Value;

				try {
					job.audio.pitch = (job.basePitch + job.randomizedPitch) * Time.timeScale;
					update.Invoke(job);
				}
				catch (Exception e) {
					debug.LogError($"Error occurred while updating audio job {job.id}: {e}");
					job.stopSound = true;
				}
			}

			// Remove audio jobs that are finished playing
			List<string> audioJobsToRemove = new List<string>();
			foreach (var jobWithId in audioJobs) {
				string id = jobWithId.Key;
				AudioJob job = jobWithId.Value;
				if (job.stopSound) {
					audioJobsToRemove.Add(id);
				}
			}
			foreach (var finished in audioJobsToRemove) {
				RemoveAudio(finished);
			}

			// Update the timeRunning on each job
			foreach (var job in audioJobs.Values) {
				job.timeRunning += Time.deltaTime;
			}

			void RemoveAudio(string id) {
				AudioSource audioToDestroy = audioJobs[id].audio;

				audioJobs.Remove(id);
				if (serializableCustomUpdateAudioJobs.ContainsKey(id)) {
					serializableCustomUpdateAudioJobs.Remove(id);
				}

				if (serializableUpdateAudioOnGameObject.ContainsKey(id)) {
					serializableUpdateAudioOnGameObject.Remove(id);
				}
				updateAudioJobs.Remove(id);

				// TODO: Instead of creating and destroying these, use a pool
				Destroy(audioToDestroy.gameObject);
			}
		}

		//////////////////////
		// Public Interface //
		//////////////////////
		// Plays an AudioClip, should only be used for global (non-3D) sounds
		public void Play(AudioName audioType, string uniqueIdentifier, bool shouldForcePlay = false, Action<AudioSource> settingsOverride = null) {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		// Plays an AudioClip at the given position
		public void PlayAtLocation(AudioName audioType, string uniqueIdentifier, Vector3 location, bool shouldForcePlay = false, Action<AudioSource> settingsOverride = null) {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				audioJob.audio.transform.position = location;
				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		// Plays an AudioClip as a child of the given GameObject
		public void PlayOnGameObject<T>(
			AudioName audioType,
			string uniqueIdentifier,
			T audioJobOnGameObject,
			bool shouldForcePlay = false,
			Action<AudioSource> settingsOverride = null
		) where T : SaveableObject, AudioJobOnGameObject {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				audioJob.audio.transform.position = audioJobOnGameObject.transform.position;

				Action<AudioJob> update = audioJobOnGameObject.UpdateAudio;
				serializableUpdateAudioOnGameObject[audioJob.id] = audioJobOnGameObject;
				updateAudioJobs[audioJob.id] = update;

				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		// Play an AudioClip with an arbitrary function modifying the source
		public void PlayWithUpdate<T>(
			AudioName audioType,
			string uniqueIdentifier,
			T customUpdate,
			bool shouldForcePlay = false,
			Action<AudioSource> settingsOverride = null
		) where T : SaveableObject, CustomAudioJob {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				if (shouldForcePlay || !serializableCustomUpdateAudioJobs.ContainsKey(audioJob.id)) {
					serializableCustomUpdateAudioJobs[audioJob.id] = customUpdate;
					updateAudioJobs[audioJob.id] = customUpdate.UpdateAudio;
				}

				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		public AudioJob GetAudioJob(AudioName audioType, string uniqueIdentifier) {
			return GetAudioJob(Id(audioType, uniqueIdentifier));
		}

		private AudioJob GetAudioJob(string id) {
			return audioJobs.ContainsKey(id) ? audioJobs[id] : null;
		}

		private static string Id(AudioName audioName, string uniqueIdentifier) {
			return $"{uniqueIdentifier}/{audioName}";
		}

		public AudioJob GetOrCreateJob(AudioName audioType, string uniqueIdentifier, Action<AudioSource> settingsOverride = null) {
			string id = Id(audioType, uniqueIdentifier);

			AudioJob audioJob;
			if (audioJobs.ContainsKey(id)) {
				audioJob = audioJobs[id];
			}
			else {
				GameObject newAudioGO = new GameObject(id);
				//newAudioGO.transform.SetParent(soundsRoot);
				newAudioGO.transform.position = Vector3.zero;

				AudioSettings settings = defaultSettings[audioType];

				AudioSource newAudioSource = newAudioGO.PasteComponent(settings.audioSource);
				settingsOverride?.Invoke(newAudioSource);

				newAudioGO.name = id;
				audioJob = new AudioJob {
					id = id,
					audioType = audioType,
					audio = newAudioSource,
					basePitch = newAudioSource.pitch,
					randomizedPitch = UnityEngine.Random.Range(-settings.randomizePitch, settings.randomizePitch)
				};

				newAudioSource.pitch += audioJob.randomizedPitch;
				audioJobs.Add(audioJob.id, audioJob);
			}

			return audioJob;
		}
		
#region Saving
		// There's only one AudioManager so we don't need a UniqueId here
		public override string ID => "AudioManager";

		[Serializable]
		public class AudioManagerSave : SerializableSaveObject<AudioManager> {
			SerializableDictionary<string, AudioJob.AudioJobSave> audioJobSaves;
			SerializableDictionary<string, SerializableReference> customAudioJobs;
			SerializableDictionary<string, SerializableReference> audioJobOnGameObjects;

			public AudioManagerSave(AudioManager audioManager) : base(audioManager) {
				this.audioJobSaves = audioManager.audioJobs.MapValues(value => new AudioJob.AudioJobSave(value));
				this.customAudioJobs = audioManager.serializableCustomUpdateAudioJobs;
				this.audioJobOnGameObjects = audioManager.serializableUpdateAudioOnGameObject;
			}

			public override void LoadSave(AudioManager audioManager) {
				audioManager.audioJobs.Clear();
				audioManager.updateAudioJobs.Clear();
				
				audioManager.serializableCustomUpdateAudioJobs = this.customAudioJobs;
				audioManager.serializableUpdateAudioOnGameObject = this.audioJobOnGameObjects;
				Dictionary<string, AudioJob.AudioJobSave> savedAudioJobs = this.audioJobSaves;
				
				foreach (var kv in savedAudioJobs) {
					audioManager.audioJobs[kv.Key] = kv.Value.LoadAudioJob();
				}

				foreach (var kv in audioManager.serializableCustomUpdateAudioJobs) {
					string id = kv.Key;
					SerializableReference customAudioJobReference = kv.Value;

					customAudioJobReference.Reference.MatchAction(
						saveableObject => {
							CustomAudioJob customAudioJob = saveableObject as CustomAudioJob;
							if (customAudioJob != null) {
								audioManager.updateAudioJobs[id] = customAudioJob.UpdateAudio;
							}
							else {
								audioManager.debug.LogError($"Failed to cast audioJob {id} as CustomAudioJob");
							}
						},
						serializableSaveObject => audioManager.audioJobs[id].Stop()
					);
				}

				foreach (var kv in audioManager.serializableUpdateAudioOnGameObject) {
					string id = kv.Key;
					SerializableReference updateAudioOnGameObjectReference = kv.Value;
					
					updateAudioOnGameObjectReference.Reference.MatchAction(
						saveableObject => {
							AudioJobOnGameObject audioJobOnGameObject = saveableObject as AudioJobOnGameObject;
							if (audioJobOnGameObject != null) {
								audioManager.updateAudioJobs[id] = audioJobOnGameObject.UpdateAudio;
							}
							else {
								audioManager.debug.LogError($"Failed to cast audioJob {id} as AudioJobOnGameObject");
							}
						},
						serializableSaveObject => audioManager.audioJobs[id].Stop()
					);
				}
			}
		}
#endregion
    }
}