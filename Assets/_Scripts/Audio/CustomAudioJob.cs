using System;
using Audio;

public interface CustomAudioJob {
    void UpdateAudioJob(AudioManager.AudioJob job);
}

public static class CustomAudioJobExt {
    public static void UpdateAudio(this CustomAudioJob customAudioJob, AudioManager.AudioJob audioJob) {
        try {
            customAudioJob.UpdateAudioJob(audioJob);
        }
        catch (Exception) {
            audioJob.Stop();
        }
    }
}