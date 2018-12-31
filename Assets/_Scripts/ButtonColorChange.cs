using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class ButtonColorChange : MonoBehaviour {
	Color startColor;
	public Color pressColor = Color.white;

	Button thisButton;
	EpitaphRenderer r;

	// Use this for initialization
	void Start () {
		r = GetComponent<EpitaphRenderer>();
		if (r == null) {
			r = gameObject.AddComponent<EpitaphRenderer>();
		}

		startColor = r.GetMainColor();
		thisButton = GetComponent<Button>();
		thisButton.OnButtonPressBegin += ButtonPressBegin;
		thisButton.OnButtonDepressBegin += ButtonDepressBegin;
	}

	void ButtonPressBegin(Button b) {
		StartCoroutine(ButtonPress(b));
	}
	void ButtonDepressBegin(Button b) {
		StartCoroutine(ButtonDepress(b));
	}

	IEnumerator ButtonPress(Button b) {
		float timeElapsed = 0;
		while (timeElapsed < b.timeToPressButton) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / b.timeToPressButton;

			r.SetMainColor(Color.Lerp(startColor, pressColor, b.buttonDepressCurve.Evaluate(t)));

			yield return null;
		}

		r.SetMainColor(pressColor);
	}

	IEnumerator ButtonDepress(Button b) {
		float timeElapsed = 0;
		while (timeElapsed < b.timeToPressButton) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / b.timeToPressButton;

			r.SetMainColor(Color.Lerp(pressColor, startColor, b.buttonDepressCurve.Evaluate(t)));

			yield return null;
		}

		r.SetMainColor(startColor);
	}
}
