using System;
using System.Linq;
using Audio;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

public class PlayerFallingSound : SuperspectiveObject, CustomAudioJob {
    [SerializeField]
    PlayerMovement player;

    private const float highWindMaxVolume = .5f;
    private float curFallingSpeed = 0f;
    private const float windSpeedThreshold = 15f;   // Only play the wind falling sound if faster than this
    private const float terminalVelocity = 80f;     // Found through testing, update if inaccurate
    private const float lowWindSoundMinDistance = 8f; // Within this distance, nearby objects will make max volume low wind sound
    private const float lowWindSoundMaxDistance = 16f;
    
    private float targetLowWindVolume = 0f;
    private const float targetLowWindVolumeIncreaseLerpSpeed = 10f;
    private const float targetLowWindVolumeDecreaseLerpSpeed = 3f;
    private float targetHighWindVolume = 0f;
    private const float targetHighWindVolumeLerpSpeed = 10f;

    private Collider nearbyCollider;
    private Vector3 CameraPos => Player.instance.PlayerCam.transform.position;

    private float PlayerScale => Player.instance.Scale;

    private float DistanceToNearbyCollider => nearbyCollider == null
        ? lowWindSoundMaxDistance
        : GetDistance(nearbyCollider);

    Vector3 GetClosestPosition(Collider c) {
        // This isn't accurate, revisit later if you want to fix this
        if (c.transform.rotation != Quaternion.identity) {
            Quaternion rotation = c.transform.rotation;
            c.transform.rotation = Quaternion.identity;
            Vector3 point = c.ClosestPointOnBounds(CameraPos);
            c.transform.rotation = rotation;
            return point;
        }
        return c.ClosestPointOnBounds(CameraPos);
    }
    
    float GetDistance(Collider c) {
        return Vector3.Distance(CameraPos, GetClosestPosition(c)) / PlayerScale;
    }

    protected override void Init() {
        base.Init();
        
        // Start with the audio at zero to not blast the player with wind sound when they start the game
        AudioManager.instance.PlayWithUpdate(AudioName.FallingWind, ID, this, settingsOverride: job => job.audio.volume = 0);
        AudioManager.instance.PlayWithUpdate(AudioName.FallingWindLow, ID, this, settingsOverride: job => job.audio.volume = 0);
    }

    private void Update() {
        curFallingSpeed = Vector3.Dot(player.ProjectedVerticalVelocity() / PlayerScale, Physics.gravity.normalized);
    }

    private void FixedUpdate() {
        if (curFallingSpeed > windSpeedThreshold) {
            Collider[] hitColliders = Physics.OverlapSphere(Player.instance.PlayerCam.transform.position, lowWindSoundMaxDistance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore);
            
            nearbyCollider = hitColliders
                .Where(c => !(c.TaggedAsPlayer() || c.isTrigger || c.gameObject.layer == LayerMask.NameToLayer("CollideWithPlayerOnly")))
                .ToDictionary(c => c, GetDistance)
                .OrderBy(kv => kv.Value)
                .FirstOrDefault()
                .Key;
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
                float distanceT = 1-Mathf.InverseLerp(lowWindSoundMinDistance, lowWindSoundMaxDistance, DistanceToNearbyCollider);

                targetLowWindVolume = speedT * distanceT;
            }
            else {
                targetLowWindVolume = 0f;
            }

            float curVolume = job.audio.volume;
            float lerpSpeed = targetLowWindVolume > curVolume
                ? targetLowWindVolumeIncreaseLerpSpeed
                : targetLowWindVolumeDecreaseLerpSpeed;
            job.audio.volume = Mathf.Lerp(curVolume, targetLowWindVolume, Time.deltaTime * lerpSpeed);
        }
    }

    private void OnDrawGizmosSelected() {
        if (nearbyCollider == null || !DEBUG) return;
        Color color = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetClosestPosition(nearbyCollider), GetDistance(nearbyCollider));
        Gizmos.color = color;
    }

    public override string ID => "PlayerFallingSound";
}
