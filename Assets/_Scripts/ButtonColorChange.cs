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

		buttonToReactTo.OnButtonPressFinish += ButtonPressFinish;
		buttonToReactTo.OnButtonDepressFinish += ButtonDepressFinish;
	}

	private void Update() {
		UpdateColor();
	}

	void UpdateColor() {
		float t;
		switch (buttonToReactTo.state) {
			case Button.State.ButtonPressing:
				t = buttonToReactTo.timeSinceStateChange / buttonToReactTo.timeToPressButton;

				r.SetMainColor(Color.Lerp(startColor, pressColor, buttonToReactTo.buttonPressCurve.Evaluate(t)));
				r.SetColor("_EmissionColor", Color.Lerp(startEmission, pressEmission, buttonToReactTo.buttonPressCurve.Evaluate(t)));
				break;
			case Button.State.ButtonDepressing:
				t = buttonToReactTo.timeSinceStateChange / buttonToReactTo.timeToDepressButton;

				r.SetMainColor(Color.Lerp(pressColor, startColor, buttonToReactTo.buttonDepressCurve.Evaluate(t)));
				r.SetColor("_EmissionColor", Color.Lerp(pressEmission, startEmission, buttonToReactTo.buttonDepressCurve.Evaluate(t)));
				break;
			default:
				break;
		}
	}

	void ButtonPressFinish(Button b) {
		r.SetMainColor(pressColor);
		r.SetColor("_EmissionColor", pressEmission);
	}
	void ButtonDepressFinish(Button b) {
		r.SetMainColor(startColor);
		r.SetColor("_EmissionColor", startEmission);
	}
}
