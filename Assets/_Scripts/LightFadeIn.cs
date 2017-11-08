using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFadeIn : MonoBehaviour {
    Light light;
    float targetIntensity;
    public float fadeInTime = 5;
    public MonoBehaviour[] scriptsToEnableAfter;

    // Use this for initialization
    void Awake () {
		light = GetComponent<Light>();
        targetIntensity = light.intensity;
        light.intensity = 0;
    }
	
    IEnumerator Start() {
        float timeElapsed = 0;
        while (timeElapsed < fadeInTime) {
            timeElapsed += Time.deltaTime;

            light.intensity = (timeElapsed / fadeInTime) * targetIntensity;

            yield return null;
        }
        light.intensity = targetIntensity;

        foreach (MonoBehaviour script in scriptsToEnableAfter) {
            script.enabled = true;
        }
    }
}
