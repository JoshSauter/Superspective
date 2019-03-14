using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Projector : MonoBehaviour {
	float minSize = .5f;
	float maxSize = 2;
	float currentSize = 1;
	float frustumSizeChangeSpeed = 1;

	float minRotation = -55;
	float maxRotation = 55;
	float currentRotation = 0;
	float rotationSpeed = 10;

	float circumferenceRotationSpeed = 10;

	float minHeight = 1.5f;
	float maxHeight = 40f;
	float verticalMovespeed = 10;

	// Use this for initialization
	void Start () {
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
		float newHeight = transform.localPosition.y + amount;
		if (newHeight < minHeight) {
			amount = minHeight - transform.localPosition.y;
		}
		else if (newHeight > maxHeight) {
			amount = maxHeight - transform.localPosition.y;
		}
		transform.localPosition += Vector3.up * amount;
		transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + Vector3.right * amount * 1.4f);
	}
}
