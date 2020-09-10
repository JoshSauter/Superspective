using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonColorChange : MonoBehaviour {
	public bool useMaterialAsStartColor = true;
	public Color startColor;
	[ColorUsage(true, true)]
	public Color startEmission = Color.black;
	public Color pressColor = Color.white;
	[ColorUsage(true, true)]
	public Color pressEmission = Color.black;

	public Button buttonToReactTo;
	EpitaphRenderer r;

	[Button("Swap powered/depowered colors")]
	void SwapPoweredDepoweredColors() {
		Color tempColor = startColor;
		Color tempEmission = startEmission;

		startColor = pressColor;
		startEmission = pressEmission;

		pressColor = tempColor;
		pressEmission = tempEmission;
	}

	// Use this for initialization
	void Start () {
		if (buttonToReactTo == null) {
			buttonToReactTo = GetComponent<Button>();
		}
		if (buttonToReactTo == null) {
			Debug.LogWarning("No button to react to, disabling color change script", gameObject);
			enabled = false;
			return;
		}

		r = GetComponent<EpitaphRenderer>();
		if (r == null) {
			r = gameObject.AddComponent<EpitaphRenderer>();
		}

		if (useMaterialAsStartColor) {
			startColor = r.GetMainColor();
			startEmission = r.GetColor("_EmissionColor");
		}
		else {
			r.SetMainColor(startColor);
			r.SetColor("_EmissionColor", startEmission);
		}

		buttonToReactTo.OnButtonPressBegin += ButtonPressBegin;
		buttonToReactTo.OnButtonDepressBegin += ButtonDepressBegin;
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

			r.SetMainColor(Color.Lerp(startColor, pressColor, b.buttonPressCurve.Evaluate(t)));
			r.SetColor("_EmissionColor", Color.Lerp(startEmission, pressEmission, b.buttonPressCurve.Evaluate(t)));

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
			r.SetColor("_EmissionColor", Color.Lerp(pressEmission, startEmission, b.buttonDepressCurve.Evaluate(t)));

			yield return null;
		}

		r.SetMainColor(startColor);
	}
}
