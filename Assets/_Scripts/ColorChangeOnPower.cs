using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;

public class ColorChangeOnPower : MonoBehaviour {
	public bool useMaterialAsStartColor = true;
	public Color depoweredColor;
	[ColorUsage(true, true)]
	public Color depoweredEmission;
	public Color poweredColor;
	[ColorUsage(true, true)]
	public Color poweredEmission;

	public AnimationCurve colorChangeAnimationCurve;
	public float timeToChangeColor = 0.25f;
	public PowerTrail powerTrailToReactTo;
	EpitaphRenderer r;

	// Use this for initialization
	void Start() {
		if (powerTrailToReactTo == null) {
			powerTrailToReactTo = GetComponent<PowerTrail>();
		}
		if (powerTrailToReactTo == null) {
			Debug.LogWarning("No Power Trail to react to, disabling color change script", gameObject);
			enabled = false;
			return;
		}

		r = GetComponent<EpitaphRenderer>();
		if (r == null) {
			r = gameObject.AddComponent<EpitaphRenderer>();
		}

		if (useMaterialAsStartColor) {
			depoweredColor = r.GetMainColor();
			depoweredEmission = r.GetColor("_EmissionColor");
		}
		else {
			r.SetMainColor(depoweredColor);
			r.SetColor("_EmissionColor", depoweredEmission);
		}

		powerTrailToReactTo.OnPowerFinish += PowerOn;
		powerTrailToReactTo.OnDepowerBegin += PowerOff;
	}

	void PowerOn() {
		StartCoroutine(PowerOnCoroutine());
	}
	void PowerOff() {
		StartCoroutine(PowerOffCoroutine());
	}

	IEnumerator PowerOnCoroutine() {
		float timeElapsed = 0;
		while (timeElapsed < timeToChangeColor) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeToChangeColor;

			r.SetMainColor(Color.Lerp(depoweredColor, poweredColor, colorChangeAnimationCurve.Evaluate(t)));
			r.SetColor("_EmissionColor", Color.Lerp(depoweredEmission, poweredEmission, colorChangeAnimationCurve.Evaluate(t)));

			yield return null;
		}

		r.SetMainColor(poweredColor);
	}

	IEnumerator PowerOffCoroutine() {
		float timeElapsed = 0;
		while (timeElapsed < timeToChangeColor) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeToChangeColor;

			r.SetMainColor(Color.Lerp(poweredColor, depoweredColor, colorChangeAnimationCurve.Evaluate(t)));
			r.SetColor("_EmissionColor", Color.Lerp(poweredEmission, depoweredEmission, colorChangeAnimationCurve.Evaluate(t)));

			yield return null;
		}

		r.SetMainColor(depoweredColor);
	}
}
