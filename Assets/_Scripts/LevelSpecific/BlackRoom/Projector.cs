using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Projector : MonoBehaviour {
	float minSize = .5f;
	float maxSize = 2;
	float currentSize = 1;
	float frustumSizeChangeSpeed = 1;

	float minRotation = -40;
	float maxRotation = 40;
	float currentRotation = 0;
	float rotationSpeed = 10;

	float circumferenceRotationSpeed = 10;

	Animator anim;
	float curAnimTime = 0.15f;
	float desiredAnimTime = 0.1f;
	float animLerpSpeed = 0.2f;
	float verticalMovespeed = .15f;

	private void Start() {
		anim = GetComponent<Animator>();
		desiredAnimTime = curAnimTime;
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKey("f")) {
			ChangeFrustumSize(1 + (Input.GetKey(KeyCode.LeftShift) ? -frustumSizeChangeSpeed/100f : frustumSizeChangeSpeed/100f));
		}
		if (Input.GetKey("g")) {
			ChangeAngle(Input.GetKey(KeyCode.LeftShift) ? -rotationSpeed : rotationSpeed);
		}
		if (Input.GetKey("h")) {
			RotateAroundCircumference(Input.GetKey(KeyCode.LeftShift) ? -circumferenceRotationSpeed : circumferenceRotationSpeed);
		}
		if (Input.GetKey("j")) {
			MoveProjectorVertical(Input.GetKey(KeyCode.LeftShift) ? -verticalMovespeed : verticalMovespeed);
		}

		if (anim != null) {
			curAnimTime = Mathf.Lerp(curAnimTime, desiredAnimTime, animLerpSpeed);
			GetComponent<Animator>().Play("Projector", 0, curAnimTime);
		}
	}

	// Stretches the far plane of the frustum within the bounds minSize <-> maxSize
	void ChangeFrustumSize(float multiplier) {
		currentSize *= multiplier;
		if (currentSize < minSize || currentSize > maxSize) {
			currentSize = Mathf.Clamp(currentSize, minSize, maxSize);
			return;
		}
		Vector3 curScale = transform.localScale;
		curScale.x *= multiplier;
		curScale.z *= multiplier;
		transform.localScale = curScale;
	}

	// Rotates the projector along the y-axis of rotation, within the bounds minRotation <-> maxRotation
	void ChangeAngle(float rotation) {
		rotation *= Time.deltaTime;
		float newRotation = currentRotation + rotation;
		if (newRotation < minRotation) {
			rotation = minRotation - currentRotation;
		}
		else if (newRotation > maxRotation) {
			rotation = maxRotation - currentRotation;
		}
		currentRotation += rotation;
		transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + Vector3.up * rotation);
	}

	// Moves the projector along the circumference of the puzzle area by rotating its parent gameobject's transform
	void RotateAroundCircumference(float rotation) {
		transform.parent.localRotation = Quaternion.Euler(transform.parent.localRotation.eulerAngles + Vector3.up * rotation * Time.deltaTime);
	}

	void MoveProjectorVertical(float amount) {
		amount *= Time.deltaTime;
		if (anim != null) {
			desiredAnimTime = Mathf.Clamp01(desiredAnimTime + amount);
		}
	}
}
