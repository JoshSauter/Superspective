using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : Singleton<CameraShake> {
	public bool DEBUG = false;
	bool inShakeCoroutine = false;

	float returnToCenterLerpSpeed = 2f;
	public Vector2 totalOffsetApplied = Vector2.zero;

#if UNITY_EDITOR
	private void Update() {
		if (DEBUG && Input.GetKeyDown("c")) {
			if (!inShakeCoroutine) {
				Shake(2, 1, 0);
			}
			else {
				CancelShake();
			}
		}

		if (!inShakeCoroutine) {
			if (totalOffsetApplied.magnitude > 0.001f) {
				Vector2 nextTotalOffsetApplied = Vector2.Lerp(totalOffsetApplied, Vector2.zero, returnToCenterLerpSpeed * Time.deltaTime);
				Vector2 offset = nextTotalOffsetApplied - totalOffsetApplied;
				transform.localPosition += new Vector3(offset.x, offset.y, 0);
				totalOffsetApplied = nextTotalOffsetApplied;
			}
			else if (totalOffsetApplied.magnitude > 0f) {
				transform.localPosition -= new Vector3(totalOffsetApplied.x, totalOffsetApplied.y, 0);
				totalOffsetApplied = Vector2.zero;
			}
		}
	}
#endif

	public void Shake(float duration, float intensityMultiplier, AnimationCurve curve) {
		if (!inShakeCoroutine) {
			StartCoroutine(ShakeCoroutine(duration, intensityMultiplier, curve));
		}
	}

	public void Shake(float duration, float startIntensity, float endIntensity) {
		if (!inShakeCoroutine) {
			StartCoroutine(ShakeCoroutine(duration, startIntensity, endIntensity));
		}
	}

	public void CancelShake() {
		inShakeCoroutine = false;
		StopAllCoroutines();
	}

	IEnumerator ShakeCoroutine(float duration, float startIntensity, float endIntensity) {
		inShakeCoroutine = true;

		Vector2 appliedOffset = Vector2.zero;
		float timeElapsed = 0;
		float intensity = startIntensity;
		while (timeElapsed < duration) {
			float t = timeElapsed / duration;

			intensity = Mathf.Lerp(startIntensity, endIntensity, t);
			Vector2 random = Random.insideUnitCircle * intensity / 10f;
			Vector2 offset = Vector2.Lerp(Vector2.zero, -appliedOffset, t) + random;

			appliedOffset += offset;
			transform.localPosition += new Vector3(offset.x, offset.y, 0);
			totalOffsetApplied += offset;

			timeElapsed += Time.deltaTime;
			yield return null;
		}
		transform.localPosition -= new Vector3(appliedOffset.x, appliedOffset.y, 0);
		totalOffsetApplied -= appliedOffset;

		inShakeCoroutine = false;
	}

	IEnumerator ShakeCoroutine(float duration, float intensityMultiplier, AnimationCurve curve) {
		inShakeCoroutine = true;

		Vector2 appliedOffset = Vector2.zero;
		float timeElapsed = 0;
		while (timeElapsed < duration) {
			float t = timeElapsed / duration;

			float intensity = curve.Evaluate(t) * intensityMultiplier;
			Vector2 random = Random.insideUnitCircle * intensity / 10f;
			Vector2 offset = Vector2.Lerp(Vector2.zero, -appliedOffset, t) + random;

			appliedOffset += offset;
			transform.localPosition += new Vector3(offset.x, offset.y, 0);
			totalOffsetApplied += offset;

			timeElapsed += Time.deltaTime;
			yield return null;
		}
		transform.localPosition -= new Vector3(appliedOffset.x, appliedOffset.y, 0);
		totalOffsetApplied -= appliedOffset;

		inShakeCoroutine = false;
	}
}
