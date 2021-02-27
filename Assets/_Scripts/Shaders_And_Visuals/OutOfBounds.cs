using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SuperspectiveUtils;
using LevelManagement;

public class OutOfBounds : MonoBehaviour {
	public Levels levelToLoad;

	Image redBackground;
	Image blackForeground;

	TextMeshProUGUI countdownText;
	TextMeshProUGUI returnToPlayzoneText;
	float initialTimeLeft = 10;
	float timeLeft;

	IEnumerator countdownCoroutine;
	IEnumerator animateBackgroundCoroutine;
	IEnumerator quickFadeToBlackCoroutine;

	// Use this for initialization
	void Awake () {
		timeLeft = initialTimeLeft;
		countdownText = transform.Find("Countdown").GetComponent<TextMeshProUGUI>();
		returnToPlayzoneText = countdownText.transform.GetComponentsInChildrenOnly<TextMeshProUGUI>()[0];
		blackForeground = transform.parent.Find("Black").GetComponent<Image>();
		redBackground = transform.Find("RedBackground").GetComponent<Image>();
	}

	void OnEnable() {
		countdownCoroutine = Countdown();
		animateBackgroundCoroutine = AnimateBackground();
		quickFadeToBlackCoroutine = QuickFadeToBlack();

		ResetState();
		StartCoroutines();
	}

	void OnDisable() {
		StopCoroutines();
		ResetState();
	}

	void ResetState() {
		timeLeft = initialTimeLeft;
		redBackground.color = Color.clear;
		blackForeground.color = Color.clear;
		countdownText.enabled = false;
	}

	void StopCoroutines() {
		redBackground.enabled = false;
		blackForeground.enabled = false;
		countdownText.enabled = false;

		StopCoroutine(countdownCoroutine);
		StopCoroutine(animateBackgroundCoroutine);
		StopCoroutine(quickFadeToBlackCoroutine);
	}

	void StartCoroutines() {
		redBackground.enabled = true;
		blackForeground.enabled = true;
		countdownText.enabled = true;

		StartCoroutine(countdownCoroutine);
		StartCoroutine(animateBackgroundCoroutine);
		StartCoroutine(quickFadeToBlackCoroutine);
	}
	
	IEnumerator Countdown() {
		float fontMaxSize = 80;
		
		while (timeLeft >= 0) {
			if (timeLeft < 10)
				countdownText.enabled = true;
			countdownText.text = (Mathf.CeilToInt(timeLeft)).ToString();
			float t = Mathf.Min(1, 2 * (1 - timeLeft % 1));
			countdownText.fontSize = Mathf.Lerp(0, fontMaxSize, t*t);

			countdownText.color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, t*t);

			timeLeft -= Time.deltaTime;
			yield return null;
		}

		StartCoroutine(LoadLevel());
	}

	IEnumerator AnimateBackground() {
		Color red = new Color(0.6509434f, 0.119749f, 0.119749f, 0.7215686f);
		Color clear = new Color(0.6509434f, 0.119749f, 0.119749f, 0);

		while (timeLeft >= 0) {
			float secondT = 0.5f + 0.5f * -Mathf.Cos((initialTimeLeft - timeLeft) * Mathf.PI * 2);
			//redBackground.fillAmount = t;
			redBackground.color = Color.Lerp(clear, red, secondT);

			yield return null;
		}
	}

	IEnumerator QuickFadeToBlack() {
		float fadeTime = 2f;

		while (timeLeft >= fadeTime) {
			yield return null;
		}

		while (timeLeft < fadeTime && timeLeft > 0) {
			float t = 1 - (timeLeft / fadeTime);

			blackForeground.color = Color.Lerp(Color.clear, Color.black, Mathf.Sqrt(t));

			yield return null;
		}
		blackForeground.color = Color.black;
	}

	IEnumerator LoadLevel() {
		LevelManager.instance.SwitchActiveScene(levelToLoad);

		yield return new WaitForSeconds(2);

		redBackground.enabled = false;
		countdownText.enabled = false;
		returnToPlayzoneText.enabled = false;

		UnityEngine.Transform player = GameObject.FindGameObjectWithTag("Player").transform;
		player.position = new Vector3(0, 10, -15);
		player.rotation = new Quaternion(0, 0, 0, 1);
		player.GetComponent<PlayerLook>().rotationY = -45;

		float fadeTime = 2f;
		float timeElapsed = 0;

		while (timeElapsed < fadeTime) {
			float t = timeElapsed / fadeTime;

			blackForeground.color = Color.Lerp(Color.black, Color.clear, t*t);

			timeElapsed += Time.deltaTime;

			yield return null;
		}
	}
}
