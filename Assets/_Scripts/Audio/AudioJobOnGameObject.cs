using System;
using UnityEngine;

namespace Audio {
    public interface AudioJobOnGameObject {
        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob);
    }

    public static class AudioJobOnGameObjectExt {
        public static void UpdateAudio(this AudioJobOnGameObject audioJobOnGameObject, AudioManager.AudioJob audioJob) {
            try {
                audioJob.audio.transform.position = audioJobOnGameObject.GetObjectToPlayAudioOn(audioJob).position;
            }
            catch (Exception) {
                audioJob.Stop();
            }
        }
    }
}
