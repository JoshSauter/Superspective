﻿using System;
using Audio;
using LevelSpecific.BlackRoom.BlackRoom3;
using NaughtyAttributes;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

public class LargeSpinningPipeFX : SuperspectiveObject, CustomAudioJob {
    public AudioName audioToPlay = AudioName.LoopingMachinery;
    public float maxShakeDistance = 32f;
    public float minShakeDistance = 8f;
    float shakeIntensity = 4f;
    public float shakeDuration = 0.75f;
    public float shakePeriod = 4f;
    
    public float loopingMachinerySoundMaxVolume = .65f;

    public override string ID => $"{gameObject.scene.name}_{gameObject.FullPath()}";

    protected override void Init() {
        base.Init();
        AudioManager.instance.PlayWithUpdate(audioToPlay, ID, this, true, UpdateAudioJob);
    }

    protected override void OnDisable() {
        if (GameManager.instance.IsApplicationQuitting) return;
        base.OnDisable();
        
        AudioManager.instance.GetAudioJob(audioToPlay, ID).Stop();
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
            CameraShake.instance.Shake(() => audioTransform.position, shakeIntensity, shakeDuration);
        }
    }
}
