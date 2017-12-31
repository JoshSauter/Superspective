using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WTF : MonoBehaviour {
	Camera cam;
	float magnitude = 1.5f;
	float startFoV;

	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();
		startFoV = cam.fieldOfView;
	}
	
	// Update is called once per frame
	void Update () {
		float t = 0.5f * Mathf.Sin(Time.time) + 0.5f;
		cam.fieldOfView = Mathf.Lerp(startFoV / magnitude, startFoV * magnitude, t);
	}
}
