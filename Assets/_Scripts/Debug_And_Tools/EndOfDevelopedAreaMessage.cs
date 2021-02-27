using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

[RequireComponent(typeof(Collider))]
public class EndOfDevelopedAreaMessage : MonoBehaviour {
	float timeBeforeFade = 3;
	float fadeTime = 2;
	public TMPro.TextMeshProUGUI text;

	void OnTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer()) {
			StartCoroutine(DisplayEndOfDevelopedAreaMessage());
		}
	}

	IEnumerator DisplayEndOfDevelopedAreaMessage() {
		GetComponent<Collider>().enabled = false;

		text.enabled = true;
		yield return new WaitForSeconds(timeBeforeFade);

		Color startColor = text.color;
		Color targetColor = startColor;
		targetColor.a = 0;

		float timeElapsed = 0;
		while (timeElapsed < fadeTime) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / fadeTime;

			text.color = Color.Lerp(startColor, targetColor, t);

			yield return null;
		}

		text.enabled = false;
		text.color = startColor;
	}
}
