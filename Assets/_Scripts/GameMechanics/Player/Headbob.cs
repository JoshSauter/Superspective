using System;
using Saving;
using UnityEngine;

public class Headbob : SaveableObject<Headbob, Headbob.HeadbobSave> {
    const float minPeriod = .24f;
    const float maxPeriod = .87f;
    const float minAmplitude = .5f;
    const float maxAmplitude = 1.25f;

    public AnimationCurve viewBobCurve;

    // This value is read from CameraFollow to apply the camera transform offset in one place
    public float curBobAmount;

    // Time in the animation curve
    public float t;
    public float curPeriod = 1f;
    public float headbobAmount => 2 * Settings.Gameplay.Headbob.value / 100f;
    float curAmplitude = 1f;
    private float effectiveAmplitude => curAmplitude * Player.instance.growShrink.currentScale;
    PlayerMovement playerMovement;

    protected override void Start() {
        base.Start();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void FixedUpdate() {
        Vector3 playerVelocity = playerMovement.ProjectHorizontalVelocity(playerMovement.averageVelocityRecently) / Player.instance.growShrink.currentScale;
        // Don't bob faster when we artificially speed up the player
        float playerSpeed = playerVelocity.magnitude / playerMovement.movespeedMultiplier;
        if (playerMovement.grounded.isGrounded && playerSpeed > 0.2f && !NoClipMode.instance.noClipOn) {
            curPeriod = Mathf.Lerp(maxPeriod, minPeriod, Mathf.InverseLerp(0, 20f, playerSpeed));
            curAmplitude = headbobAmount * Mathf.Lerp(
                minAmplitude,
                maxAmplitude,
                Mathf.InverseLerp(0, 20f, playerSpeed)
            );

            t += Time.fixedDeltaTime / curPeriod;
            t = Mathf.Repeat(t, 1f);

            float thisFrameBobAmount = viewBobCurve.Evaluate(t) * effectiveAmplitude;
            curBobAmount = thisFrameBobAmount;
        }
        else {
            t = 0;
            float nextBobAmount = Mathf.Lerp(curBobAmount, 0f, 4f * Time.fixedDeltaTime);

            curBobAmount = nextBobAmount;
        }
    }

#region Saving
    public override string ID => "Headbob";

    [Serializable]
    public class HeadbobSave : SerializableSaveObject<Headbob> {
        float curAmplitude;
        float curBobAmount;
        float curPeriod;

        float t;

        public HeadbobSave(Headbob headbob) : base(headbob) {
            curBobAmount = headbob.curBobAmount;
            t = headbob.t;
            curPeriod = headbob.curPeriod;
            curAmplitude = headbob.curAmplitude;
        }

        public override void LoadSave(Headbob headbob) {
            headbob.curBobAmount = curBobAmount;
            headbob.t = t;
            headbob.curPeriod = curPeriod;
            headbob.curAmplitude = curAmplitude;
        }
    }
#endregion
}