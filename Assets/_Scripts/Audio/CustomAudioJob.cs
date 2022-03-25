using System;
using Audio;
using UnityEngine;

public interface CustomAudioJob {
    void UpdateAudioJob(AudioManager.AudioJob job);
}

public static class CustomAudioJobExt {
    public static void UpdateAudio(this CustomAudioJob customAudioJob, AudioManager.AudioJob audioJob) {
        try {
            customAudioJob.UpdateAudioJob(audioJob);
        }
        catch (Exception e) {
            Debug.LogError($"Error during custom Audio Update: {e}");
            audioJob.Stop();
        }
    }
}