using System.Collections;
using System.Collections.Generic;
using Audio;
using SuperspectiveUtils;
using UnityEngine;

public class DrillScenery : MonoBehaviour {
    public Transform pendulum;
    private const float maxRotation = 42.5f;
    private const float minRotation = -48.2f;
    private const float animationTime = 7.5f;
    private const float cameraShakeT = 0.195f;
    private const float cameraShakeDuration = 2f;
    private const float cameraShakeAmplitude = 1f;

    public AnimationCurve rotationAnimationCurve;

    private float lastT;
    
    // Start is called before the first frame update
    void Start() {
        lastT = (Time.time % animationTime) / animationTime;
    }

    // Update is called once per frame
    void Update() {
        float t = (Time.time % animationTime) / animationTime;
        if (lastT < cameraShakeT && t >= cameraShakeT) {
            if (gameObject.IsInActiveScene()) {
                float distanceFactor = 1 - Mathf.Pow(Mathf.InverseLerp(4f, 48f, Vector3.Distance(Player.instance.PlayerCam.transform.position, transform.position)), 2);
                float amplitude = distanceFactor * cameraShakeAmplitude;
                CameraShake.instance.Shake(cameraShakeDuration, amplitude, 0f);
            }
        }
        pendulum.rotation = Quaternion.Euler(Vector3.forward * Mathf.Lerp(minRotation, maxRotation, rotationAnimationCurve.Evaluate(t)) + new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y));

        lastT = t;
    }
}