using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFlicker : MonoBehaviour {
    Light light;

    bool inFlickerCoroutine = false;
    float refractoryPeriodMin = .5f;
    float refractoryPeriodMax = 2f;

    float turnOffTimeMin = 0f;
    float turnOffTimeMax = 0.1f;

	// Use this for initialization
	void Start () {
        light = GetComponent<Light>();
	}

    private void FixedUpdate() {
        if (!inFlickerCoroutine && Random.value > 0.97f) {
            StartCoroutine(Flicker(Random.Range(0.1f, 0.15f)));
        }
    }

    IEnumerator Flicker(float duration) {
        inFlickerCoroutine = true;
        float turnOffTime = Random.Range(turnOffTimeMin, turnOffTimeMax);
        float startIntensity = light.intensity;

        float timeElapsed = 0;
        while (timeElapsed < duration) {
            timeElapsed += Time.deltaTime;
            if (timeElapsed < turnOffTime) {
                float t = timeElapsed / turnOffTime;
                light.intensity = (1 - t) * startIntensity;
            }
            else {
                light.intensity = 0;
            }

            yield return null;
        }

        light.enabled = true;
        timeElapsed = 0;
        float refractoryPeriod = Random.Range(refractoryPeriodMin, refractoryPeriodMax);
        while (timeElapsed < refractoryPeriod) {
            timeElapsed += Time.deltaTime;
            if (timeElapsed < turnOffTime) {
                float t = timeElapsed / turnOffTime;
                light.intensity = t * startIntensity;
            }
            else {
                light.intensity = startIntensity;
            }

            yield return null;
        }

        inFlickerCoroutine = false;
    }
}
