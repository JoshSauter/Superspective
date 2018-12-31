using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectScale : MonoBehaviour {
	public float minSize = 1f;
	public float maxSize = 1f;
	public float period = 3f;

	private Vector3 startScale;

	private float timeElapsed = 0;
	
	// Update is called once per frame
	void Update () {
		timeElapsed += Time.deltaTime;
		transform.localScale = startScale.normalized * startScale.magnitude * Mathf.Lerp(minSize, maxSize, Mathf.Cos(timeElapsed * 2 * Mathf.PI / period) * 0.5f + 0.5f);
	}

	private void OnEnable() {
		startScale = transform.localScale;
	}

	private void OnDisable() {
		transform.localScale = startScale;
		timeElapsed = 0;
	}
}
