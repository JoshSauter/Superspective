using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class LibraryElevator : MonoBehaviour {
	public float elevateSpeed = 2;
	public float rotationSpeedDegrees = 10;

	bool inElevateCoroutine = false;
	bool inRotateCoroutine = false;

	private void Update() {
		if (Input.GetKeyDown("r")) {
			StartCoroutine(RotateDegrees(90));
		}
	}

	IEnumerator ElevateAndRotate() {
		inElevateCoroutine = true;

		// Shake camera before moving up
		float shakeDuration = 2;
		CameraShake.instance.Shake(shakeDuration, 1f, true);
		// Move slightly down before moving up
		float fallDistance = 0.5f;
		Vector3 startPos = transform.position;
		while (startPos.y - transform.position.y < fallDistance) {
			float t = (transform.position.y - startPos.y) / fallDistance;
			transform.parent.position -= transform.up * Mathf.Lerp(elevateSpeed, 1f, t) * Time.fixedDeltaTime;

			yield return new WaitForFixedUpdate();
		}

		// Pause before moving up
		yield return new WaitForSeconds(shakeDuration);

		// Move straight up at first
		startPos = transform.position;
		float distanceToMoveStraightUp = 5f;
		while (transform.position.y - startPos.y < distanceToMoveStraightUp) {
			float t = (transform.position.y - startPos.y) / distanceToMoveStraightUp;
			transform.parent.position += transform.up * Mathf.Lerp(0.25f, elevateSpeed, t) * Time.fixedDeltaTime;

			yield return new WaitForFixedUpdate();
		}

		while (inElevateCoroutine) {
			transform.parent.position += transform.up * elevateSpeed * Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator RotateDegrees(float degrees) {
		inRotateCoroutine = true;

		float rotateDuration = 2f;
		float timeElapsed = 0;
		float degreesPerSecond = degrees / rotateDuration;
		while (timeElapsed < rotateDuration) {
			float t = timeElapsed / rotateDuration;

			transform.parent.Rotate(Vector3.up, degreesPerSecond * Time.fixedDeltaTime);

			timeElapsed += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}

		inRotateCoroutine = false;
	}

	private void OnCollisionEnter(Collision collision) {
		if (collision.gameObject.tag == "Player") {
			collision.transform.SetParent(transform);
			if (!inElevateCoroutine) {
				StartCoroutine(ElevateAndRotate());
			}
		}
	}

	private void OnCollisionExit(Collision collision) {
		if (collision.gameObject.tag == "Player") {
			collision.transform.SetParent(null);
		}
	}
}
