using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reticle : Singleton<Reticle> {
	RectTransform thisTransform;
	RectTransform centerTransform;
	Vector2 thisTransformSize => thisTransform.anchorMax - thisTransform.anchorMin;

	Vector2 centerTransformSize => centerTransform.anchorMax - centerTransform.anchorMin;

	public Vector2 thisTransformPos {
		get => (thisTransform.anchorMax + thisTransform.anchorMin) / 2f;
		private set {
			Vector2 size = thisTransformSize;
			thisTransform.anchorMax = value + size / 2f;
			thisTransform.anchorMin = value - size / 2f;
		}
	}
	Vector2 centerTransformPos {
		get => (centerTransform.anchorMax + centerTransform.anchorMin) / 2f;
		set {
			Vector2 size = centerTransformSize;
			centerTransform.anchorMax = value + size / 2f;
			centerTransform.anchorMin = value - size / 2f;
		}
	}
	PlayerButtonInput input;

	Quaternion startRotation;
	// Which angles are we allowed to stop at (any multiple of 120 degrees so one of the lines is vertical)
	Quaternion[] stopRotations;
	public float curRotationSpeed = 0;
	float maxRotationSpeed = 1440;
	float accelerationLerp = 8f;
	float deccelerationLerp = 16f;

	float timeHeld = float.MaxValue;
	float minTimeHeld = .3f;

	// Use this for initialization
	void Start () {
		thisTransform = GetComponent<RectTransform>();
		centerTransform = transform.parent.Find("ReticleCenter").GetComponent<RectTransform>();

		input = PlayerButtonInput.instance;
		startRotation = thisTransform.rotation;

		stopRotations = new Quaternion[3];
		for (int i = 0; i < 3; i++) {
			stopRotations[i] = thisTransform.rotation;
			thisTransform.Rotate(Vector3.forward * 120);
		}
		thisTransform.rotation = startRotation;
	}
	
	// Update is called once per frame
	void Update () {
		if (input.InteractPressed) {
			timeHeld = 0;
		}
		if (input.InteractPressed || timeHeld < minTimeHeld) {
			curRotationSpeed = Mathf.Lerp(curRotationSpeed, maxRotationSpeed, accelerationLerp * Time.deltaTime);
			thisTransform.Rotate(Vector3.back * curRotationSpeed * Time.deltaTime);
			timeHeld += Time.deltaTime;
		}
		else {
			timeHeld = float.MaxValue;
			Quaternion closestStopRot = GetClosestStopRotation();
			curRotationSpeed = Mathf.Lerp(curRotationSpeed, 0, deccelerationLerp * Time.deltaTime);
			thisTransform.rotation = Quaternion.Lerp(thisTransform.rotation, closestStopRot, deccelerationLerp * Time.deltaTime);
		}
	}

	Quaternion GetClosestStopRotation() {
		float minAngleDiff = float.MaxValue;
		Quaternion minAngleStopRotation = stopRotations[0];
		foreach (Quaternion stopRot in stopRotations) {
			float angleDiff = thisTransform.rotation.eulerAngles.z - stopRot.eulerAngles.z;
			// Only consider angles which we move forward towards
			if (angleDiff < 0) continue;
			if (Mathf.Abs(angleDiff) < Mathf.Abs(minAngleDiff)) {
				minAngleDiff = angleDiff;
				minAngleStopRotation = stopRot;
			}
		}
		return minAngleStopRotation;
	}

	public void MoveReticle(Vector2 destination) {
		destination.x = Mathf.Clamp01(destination.x);
		destination.y = Mathf.Clamp01(destination.y);
		thisTransformPos = destination;
		centerTransformPos = destination;
	}
}
