using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceGradient : MonoBehaviour {
    public Shader shader;
    public Gradient gradient;

    private Material material;

    public float minDistance = 0;
    public float maxDistance = 60;

	// Use this for initialization
	void Awake () {
        if (shader != null) {
            GetComponent<Renderer>().material = new Material(shader);
        }
        material = GetComponent<Renderer>().material;

        // Initialize gradient colorkey values for shader
        GradientColorKey lastColorKey = gradient.colorKeys[gradient.colorKeys.Length - 1];
        for (int i = 0; i < 8; i++) {
            // Some fucky calculations to make this work with less than max (8) color keys defined for gradient
            GradientColorKey colorKey = (i < gradient.colorKeys.Length) ? gradient.colorKeys[i] : new GradientColorKey(lastColorKey.color, (i == 7) ? 1 : lastColorKey.time);
            material.SetColor("_Gradient" + i, colorKey.color);
            material.SetFloat("_GradientTime" + i, colorKey.time);
        }
        material.SetFloat("_MinDistance", minDistance);
        material.SetFloat("_MaxDistance", maxDistance);
    }
}
