using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System;

namespace Audio {
	public class AudioManager : Singleton<AudioManager> {
		[Serializable]
		public class AudioJob {
			public string id;
			public AudioName audioType;
			public bool removeSound = false;
			public float timeRunning = 0f;
			public AudioSource audio;

			// Will apply default settings to AudioJob if no overrides are specified
			//public AudioJob ApplySettings() {
			//	AudioSettings settings = AudioManager.instance.defaultSettings[audioType];
			//	if (settingsOverride != null) {
			//		settings.randomizePitch = settingsOverride.randomizePitch;
			//		if (settingsOverride.audioSource != null) {
			//			settings.audioSource = settingsOverride.audioSource;
			//		}
			//	}

			//	audio.GetCopyOf(settings.audioSource);
			//	audio.pitch += UnityEngine.Random.Range(-settings.randomizePitch, settings.randomizePitch);

			//	return this;
			//}
		}

		Transform soundsRoot;

        public Dictionary<AudioName, AudioSettings> defaultSettings = new Dictionary<AudioName, AudioSettings>();

		Dictionary<string, AudioJob> audioJobs = new Dictionary<string, AudioJob>();
		Dictionary<string, Action<AudioJob>> updateAudioJobs = new Dictionary<string, Action<AudioJob>>();

		private void Awake() {
			foreach (var template in Utils.GetComponentsInChildrenRecursively<AudioTemplate>(transform)) {
				defaultSettings.Add(template.type, template.audioSettings);
			}

			soundsRoot = transform.Find("Sounds");
		}

		private void Update() {
			// Perform update actions for running audio jobs that have one
			foreach (var updateJob in updateAudioJobs) {
				AudioJob job = audioJobs[updateJob.Key];
				Action<AudioJob> update = updateJob.Value;

				update.Invoke(job);
			}

			// Remove audio jobs that are finished playing
			List<string> audioJobsToRemove = new List<string>();
			foreach (var jobWithId in audioJobs) {
				string id = jobWithId.Key;
				AudioJob job = jobWithId.Value;
				if (job.removeSound) {
					audioJobsToRemove.Add(id);
				}
			}
			foreach (var finished in audioJobsToRemove) {
				AudioSource audioToDestroy = audioJobs[finished].audio;

				audioJobs.Remove(finished);
				updateAudioJobs.Remove(finished);

				Destroy(audioToDestroy.gameObject);
			}

			// Update the timeRunning on each job
			foreach (var job in audioJobs.Values) {
				job.timeRunning += Time.deltaTime;
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
		public void PlayOnGameObject(AudioName audioType, string uniqueIdentifier, GameObject gameObject, bool shouldForcePlay = false, Action<AudioSource> settingsOverride = null) {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				audioJob.audio.transform.position = gameObject.transform.position;

				Action<AudioJob> update = (job) => {
					if (gameObject == null) {
						job.removeSound = true;
						return;
					}
					job.audio.transform.position = gameObject.transform.position;
				};
				updateAudioJobs[audioJob.id] = update;

				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		// Play an AudioClip with an arbitrary function modifying the source
		public void PlayWithUpdate(AudioName audioType, string uniqueIdentifier, Action<AudioJob> update, bool shouldForcePlay = false, Action<AudioSource> settingsOverride = null) {
			AudioJob audioJob = GetOrCreateJob(audioType, uniqueIdentifier, settingsOverride);

			if (!audioJob.audio.isPlaying || shouldForcePlay) {
				updateAudioJobs[audioJob.id] = update;

				settingsOverride?.Invoke(audioJob.audio);
				audioJob.audio.Play();
			}
		}

		public AudioJob GetAudioJob(AudioName audioType, string uniqueIdentifier) {
			string id = $"{audioType}_{uniqueIdentifier}";

			if (audioJobs.ContainsKey(id)) {
				return audioJobs[id];
			}
			else {
				return null;
			}
		}

		public AudioJob GetOrCreateJob(AudioName audioType, string uniqueIdentifier, Action<AudioSource> settingsOverride = null) {
			string id = $"{audioType}_{uniqueIdentifier}";

			AudioJob audioJob;
			if (audioJobs.ContainsKey(id)) {
				audioJob = audioJobs[id];
			}
			else {
				GameObject newAudioGO = new GameObject(id);
				newAudioGO.transform.SetParent(soundsRoot);
				newAudioGO.transform.position = Vector3.zero;

				AudioSettings settings = defaultSettings[audioType];

				AudioSource newAudioSource = newAudioGO.PasteComponent<AudioSource>(settings.audioSource);
				newAudioSource.pitch += UnityEngine.Random.Range(-settings.randomizePitch, settings.randomizePitch);
				settingsOverride?.Invoke(newAudioSource);

				newAudioGO.name = id;
				audioJob = new AudioJob {
					id = id,
					audioType = audioType,
					audio = newAudioSource
				};

				audioJobs.Add(audioJob.id, audioJob);
			}

			return audioJob;
		}
    }
}