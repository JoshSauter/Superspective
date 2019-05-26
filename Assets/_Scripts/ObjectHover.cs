using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHover : MonoBehaviour {
	public bool useLocalCoordinates = true;
	public float maxDisplacementUp = 0.125f;
	public float maxDisplacementForward = 0f;
	public float maxDisplacementRight = 0;
	public float period = 1f;
	Vector3 up;
	Vector3 forward;
	Vector3 right;

	Vector3 displacementCounter = Vector3.zero;
	public bool hoveringPaused = false;
	float hoveringPauseLerp = 0.1f;

	float timeElapsed = 0;

	// Use this for initialization
	void Start() {
		up = useLocalCoordinates ? transform.up : Vector3.up;
		forward = useLocalCoordinates ? transform.forward : Vector3.forward;
		right = useLocalCoordinates ? transform.right : Vector3.right;
	}

	// Update is called once per frame
	void Update() {
		if (!hoveringPaused) {
			timeElapsed += Time.deltaTime;
			float t = Time.deltaTime * Mathf.Cos(Mathf.PI * 2 * timeElapsed / period);
			Vector3 displacementUp = maxDisplacementUp * t * up;
			Vector3 displacementForward = maxDisplacementForward * t * forward;
			Vector3 displacementRight = maxDisplacementRight * t * right;
			Vector3 displacementVector = displacementUp + displacementForward + displacementRight;
			displacementCounter += displacementVector;
			transform.position += displacementVector;
		}
		else {
			Vector3 thisFrameMovement = -hoveringPauseLerp * displacementCounter;
			transform.position += thisFrameMovement;
			displacementCounter += thisFrameMovement;
		}
	}
}
