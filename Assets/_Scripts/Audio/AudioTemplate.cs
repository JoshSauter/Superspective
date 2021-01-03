using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio {
    // Audio Templates exist only as children of the AudioManager and link the AudioType enum
    // to a Unity AudioSource which describes the default settings for the sound, including the AudioClip
    [RequireComponent(typeof(AudioSource))]
    public class AudioTemplate : MonoBehaviour {
        public AudioName type;
        public AudioSettings audioSettings;

		private void OnValidate() {
			if (audioSettings.audioSource == null) {
                audioSettings.audioSource = GetComponent<AudioSource>();
                if (audioSettings.audioSource == null) {
                    audioSettings.audioSource = gameObject.AddComponent<AudioSource>();
				}
			}

            if (audioSettings.audioSource.clip == null) {
                Debug.LogWarning($"Please provide an audio clip to {gameObject.name}'s AudioTemplate.");
			}
		}
	}
}