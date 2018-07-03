using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : Singleton<CameraShake> {
	bool inShakeCoroutine = false;

#if UNITY_EDITOR
	private void Update() {
		if (Input.GetKeyDown("c")) {
			Shake(2, 1, true);
		}
	}
#endif

	public void Shake(float duration, float intensity, bool decreaseOverDuration) {
		if (!inShakeCoroutine) {
			StartCoroutine(ShakeCoroutine(duration, intensity, decreaseOverDuration));
		}
	}

	IEnumerator ShakeCoroutine(float duration, float intensity, bool decreasesOverDuration) {
		inShakeCoroutine = true;

		Vector2 appliedOffset = Vector2.zero;
		float timeElapsed = 0;
		while (timeElapsed < duration) {
			float t = timeElapsed / duration;

			Vector2 random = Random.insideUnitCircle * intensity / 10f;
			Vector2 offset = Vector2.Lerp(Vector2.zero, -appliedOffset, t) + random;
			if (decreasesOverDuration) {
				offset *= 1 - t;
			}

			appliedOffset += offset;
			transform.localPosition += new Vector3(offset.x, offset.y, 0);

			timeElapsed += Time.deltaTime;
			yield return null;
		}
		transform.localPosition -= new Vector3(appliedOffset.x, appliedOffset.y, 0);

		inShakeCoroutine = false;
	}
}
