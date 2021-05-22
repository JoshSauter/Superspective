using UnityEngine;

namespace Audio {
    public interface AudioJobOnGameObject {
        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob);
    }

    public static class AudioJobOnGameObjectExt {
        public static void UpdateAudio(this AudioJobOnGameObject audioJobOnGameObject, AudioManager.AudioJob audioJob) {
            audioJob.audio.transform.position = audioJobOnGameObject.GetObjectToPlayAudioOn(audioJob).position;
        }
    }
}
