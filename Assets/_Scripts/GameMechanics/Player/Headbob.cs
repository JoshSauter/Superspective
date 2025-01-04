using System;
using PortalMechanics;
using Saving;
using UnityEngine;

public class Headbob : SuperspectiveObject<Headbob, Headbob.HeadbobSave> {
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
    private float effectiveAmplitude => curAmplitude * Player.instance.Scale;
    PlayerMovement playerMovement;

    protected override void Start() {
        base.Start();
        playerMovement = GetComponent<PlayerMovement>();
        Portal.BeforeAnyPortalPlayerTeleport += HandlePlayerTeleport;
    }

    protected override void OnDisable() {
        base.OnDisable();
        Portal.BeforeAnyPortalPlayerTeleport -= HandlePlayerTeleport;
    }

    void HandlePlayerTeleport(Portal portal) {
        if (portal.changeScale) {
            curBobAmount *= portal.ScaleFactor;
        }
    }

    void FixedUpdate() {
        Vector3 playerVelocity = playerMovement.ProjectHorizontalVelocity(playerMovement.AverageVelocityRecently) / Player.instance.Scale;
        // Don't bob faster when we artificially speed up the player
        float playerSpeed = playerVelocity.magnitude / playerMovement.movespeedMultiplier;
        if (playerMovement.IsGrounded && playerSpeed > 0.2f && !NoClipMode.instance.noClipOn) {
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

    public override void LoadSave(HeadbobSave save) {
        curBobAmount = save.curBobAmount;
        t = save.t;
        curPeriod = save.curPeriod;
        curAmplitude = save.curAmplitude;
    }
    
    public override string ID => "Headbob";

    [Serializable]
    public class HeadbobSave : SaveObject<Headbob> {
        public float curAmplitude;
        public float curBobAmount;
        public float curPeriod;
        public float t;

        public HeadbobSave(Headbob headbob) : base(headbob) {
            curBobAmount = headbob.curBobAmount;
            t = headbob.t;
            curPeriod = headbob.curPeriod;
            curAmplitude = headbob.curAmplitude;
        }
    }
#endregion
}