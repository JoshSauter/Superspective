using System;
using Audio;
using LevelSpecific.BlackRoom.BlackRoom3;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

public class LargeSpinningPipeFX : SaveableObject, CustomAudioJob {
    float maxShakeDistance = 32f;
    float minShakeDistance = 8f;
    float shakeIntensity = 0.4f;
    float shakeDuration = 0.75f;
    float shakePeriod = 4f;
    
    float loopingMachinerySoundMaxVolume = .65f;

    public override string ID => $"{gameObject.scene.name}_{gameObject.FullPath()}";

    protected override void Init() {
        AudioManager.instance.PlayWithUpdate(AudioName.LoopingMachinery, ID, this, true, UpdateAudioJob);
    }
    
    public void UpdateAudioJob(AudioManager.AudioJob job) {
        job.audio.volume = loopingMachinerySoundMaxVolume;
            
        Vector3 curPos = transform.position;
        Vector3 playerPos = Player.instance.transform.position;
        curPos.y = playerPos.y;
        Transform audioTransform = job.audio.transform;
        audioTransform.position = curPos;

        float distance = Vector3.Distance(audioTransform.position, playerPos);
        if (distance < maxShakeDistance && job.audio.time % shakePeriod < 0.5f) {
            float intensity = shakeIntensity * Mathf.InverseLerp(maxShakeDistance, minShakeDistance, distance);
            CameraShake.instance.Shake(shakeDuration, intensity, 0f);
        }
    }
}