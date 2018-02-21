using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialPulseEffect : MonoBehaviour {
    Transform player;
    Material mat;

    float maxFactor = 0.16f;
    float minFactor = 0.01f;

    float maxDistance = 20;

	// Use this for initialization
	void Start () {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        mat = GetComponent<Renderer>().material;
	}
	
	// Update is called once per frame
	void Update () {
        float t = (transform.position - player.position).magnitude / maxDistance;
        mat.SetFloat("_InvFade", Mathf.Lerp(minFactor, maxFactor, 1 - t * t));
	}
}
