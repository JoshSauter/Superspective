using System;
using Saving;
using UnityEngine;

public class Headbob : MonoBehaviour, SaveableObject {
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
    public float headbobAmount = .5f;
    float curAmplitude = 1f;
    PlayerMovement playerMovement;

    void Start() {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void FixedUpdate() {
        Vector3 playerVelocity = playerMovement.ProjectedHorizontalVelocity();
        float playerSpeed = playerVelocity.magnitude;
        if (playerMovement.grounded.isGrounded && playerSpeed > 0.2f) {
            curPeriod = Mathf.Lerp(maxPeriod, minPeriod, Mathf.InverseLerp(0, 20f, playerSpeed));
            curAmplitude = headbobAmount * Mathf.Lerp(
                minAmplitude,
                maxAmplitude,
                Mathf.InverseLerp(0, 20f, playerSpeed)
            );

            t += Time.fixedDeltaTime / curPeriod;
            t = Mathf.Repeat(t, 1f);

            float thisFrameBobAmount = viewBobCurve.Evaluate(t) * curAmplitude;
            curBobAmount = thisFrameBobAmount;
        }
        else {
            t = 0;
            float nextBobAmount = Mathf.Lerp(curBobAmount, 0f, 4f * Time.fixedDeltaTime);

            curBobAmount = nextBobAmount;
        }
    }

#region Saving
    // There's only one player so we don't need a UniqueId here
    public bool SkipSave { get; set; }
    public string ID => "Headbob";

    [Serializable]
    class HeadbobSave {
        float curAmplitude;
        float curBobAmount;
        float curPeriod;
        float headbobAmount;

        float t;

        public HeadbobSave(Headbob headbob) {
            curBobAmount = headbob.curBobAmount;
            t = headbob.t;
            curPeriod = headbob.curPeriod;
            headbobAmount = headbob.headbobAmount;
            curAmplitude = headbob.curAmplitude;
        }

        public void LoadSave(Headbob headbob) {
            headbob.curBobAmount = curBobAmount;
            headbob.t = t;
            headbob.curPeriod = curPeriod;
            headbob.headbobAmount = headbobAmount;
            headbob.curAmplitude = curAmplitude;
        }
    }

    public object GetSaveObject() {
        return new HeadbobSave(this);
        ;
    }

    public void LoadFromSavedObject(object savedObject) {
        HeadbobSave save = savedObject as HeadbobSave;

        save.LoadSave(this);
    }
#endregion
}