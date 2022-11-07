using System;
using Audio;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

public class PlayerFallingSound : SaveableObject, CustomAudioJob {
    [SerializeField]
    PlayerMovement player;

    private const float highWindMaxVolume = .5f;
    private float curFallingSpeed = 0f;
    private const float windSpeedThreshold = 15f;   // Only play the wind falling sound if faster than this
    private const float terminalVelocity = 80f;     // Found through testing, update if inaccurate
    private const float lowWindSoundMinDistance = 8f; // Within this distance, nearby objects will make max volume low wind sound
    
    private float targetLowWindVolume = 0f;
    private const float targetLowWindVolumeIncreaseLerpSpeed = 10f;
    private const float targetLowWindVolumeDecreaseLerpSpeed = 3f;
    private float targetHighWindVolume = 0f;
    private const float targetHighWindVolumeLerpSpeed = 10f;

    private SphereCollider thisTrigger;
    private Collider nearbyCollider;
    private Vector3 cameraPos => Player.instance.playerCam.transform.position;

    private float distanceToNearbyCollider => nearbyCollider == null
        ? thisTrigger.radius
        : Vector3.Distance(cameraPos, nearbyCollider.ClosestPointOnBounds(cameraPos));

    protected override void Start() {
        base.Start();

        thisTrigger = GetComponent<SphereCollider>();
        
        AudioManager.instance.PlayWithUpdate(AudioName.FallingWind, ID, this);
        AudioManager.instance.PlayWithUpdate(AudioName.FallingWindLow, ID, this);
    }

    private void Update() {
        curFallingSpeed = Vector3.Dot(player.ProjectedVerticalVelocity(), Physics.gravity.normalized);
    }

    private void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer() || other.isTrigger) return;

        if (curFallingSpeed > windSpeedThreshold) {
            nearbyCollider = other;
        }
    }

    public void UpdateAudioJob(AudioManager.AudioJob job) {
        if (job.audioName == AudioName.FallingWind) {
            float t = Mathf.InverseLerp(windSpeedThreshold, terminalVelocity, curFallingSpeed);
            targetHighWindVolume = highWindMaxVolume * t;
            job.audio.volume = Mathf.Lerp(job.audio.volume, targetHighWindVolume, targetHighWindVolumeLerpSpeed);
        }
        else {
            if (nearbyCollider != null) {
                float speedT = Mathf.InverseLerp(windSpeedThreshold, terminalVelocity, curFallingSpeed);
                float distanceT = 1-Mathf.InverseLerp(lowWindSoundMinDistance, thisTrigger.radius, distanceToNearbyCollider);

                targetLowWindVolume = speedT * distanceT;
            }

            float curVolume = job.audio.volume;
            float lerpSpeed = targetLowWindVolume > curVolume
                ? targetLowWindVolumeIncreaseLerpSpeed
                : targetLowWindVolumeDecreaseLerpSpeed;
            job.audio.volume = Mathf.Lerp(curVolume, targetLowWindVolume, Time.deltaTime * lerpSpeed);
        }
    }

    public override string ID => "PlayerFallingSound";
}
