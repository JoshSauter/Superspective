using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpitaphUtils;

public class LightProjector : MonoBehaviour {
	float minSize = .5f;
	float maxSize = 2;
	float currentSize = 1;
	float frustumSizeChangeSpeed = 1;

    Animator sideToSideAnim;
    float curSideToSideAnimTime = 0.5f;
    float desiredSideToSideAnimTime = 0.5f;
    float sideToSideAnimLerpSpeed = 0.2f;
	float rotationSpeed = .4f;

	float circumferenceRotationSpeed = 10;

	Animator upAndDownAnim;
	float curUpAndDownAnimTime = 0.15f;
	float desiredUpAndDownAnimTime = 0.15f;
	float upAndDownAnimLerpSpeed = 0.2f;
	float verticalMovespeed = .15f;

	private void Start() {
		upAndDownAnim = GetComponent<Animator>();
        sideToSideAnim = transform.parent.GetComponent<Animator>();
		desiredUpAndDownAnimTime = curUpAndDownAnimTime;
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKey("f")) {
			if (Input.GetKey(KeyCode.LeftShift)) {
				DecreaseFrustumSize();
			}
			else {
				IncreaseFrustumSize();
			}
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

		if (upAndDownAnim != null) {
			curUpAndDownAnimTime = Mathf.Lerp(curUpAndDownAnimTime, desiredUpAndDownAnimTime, upAndDownAnimLerpSpeed);
			upAndDownAnim.Play("ProjectorUpDown", 0, curUpAndDownAnimTime);
		}
        if (sideToSideAnim != null) {
            curSideToSideAnimTime = Mathf.Lerp(curSideToSideAnimTime, desiredSideToSideAnimTime, sideToSideAnimLerpSpeed);
            sideToSideAnim.Play("ProjectorSideToSide", 1, curSideToSideAnimTime);
        }
	}

	public void IncreaseFrustumSize() {
		ChangeFrustumSize(1 + frustumSizeChangeSpeed / 100f);
	}

	public void DecreaseFrustumSize() {
		ChangeFrustumSize(1 - frustumSizeChangeSpeed / 100f);
	}

	// Stretches the far plane of the frustum within the bounds minSize <-> maxSize
	void ChangeFrustumSize(float multiplier) {
		currentSize *= multiplier;
		if (currentSize < minSize || currentSize > maxSize) {
			currentSize = Mathf.Clamp(currentSize, minSize, maxSize);
			return;
		}
		Vector3 curScale = transform.GetChild(0).localScale;
		curScale.x *= multiplier;
		curScale.z *= multiplier;
		transform.GetChild(0).localScale = curScale;
	}

	// Rotates the projector along the y-axis of rotation, within the bounds minRotation <-> maxRotation
	void ChangeAngle(float rotation) {
		rotation *= Time.deltaTime;
        if (sideToSideAnim != null) {
            desiredSideToSideAnimTime = Mathf.Clamp01(desiredSideToSideAnimTime + rotation);
        }
	}

	// Moves the projector along the circumference of the puzzle area by rotating its parent gameobject's transform
	void RotateAroundCircumference(float rotation) {
		transform.parent.parent.localRotation = Quaternion.Euler(transform.parent.parent.localRotation.eulerAngles + Vector3.up * rotation * Time.deltaTime);
	}

	void MoveProjectorVertical(float amount) {
		amount *= Time.deltaTime;
		if (upAndDownAnim != null) {
			desiredUpAndDownAnimTime = Mathf.Clamp01(desiredUpAndDownAnimTime + amount);
		}
	}
}
