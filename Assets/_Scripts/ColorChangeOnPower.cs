using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;

public class ColorChangeOnPower : MonoBehaviour {
	public enum ActivationTiming {
		OnPowerBegin,
		OnPowerFinish,
		OnDepowerBegin,
		OnDepowerFinish
	}
	public ActivationTiming timing = ActivationTiming.OnPowerFinish;
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
	public EpitaphRenderer[] renderers;

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

		if (renderers == null || renderers.Length == 0) {
			renderers = GetComponents<EpitaphRenderer>();
		}
		if (renderers == null || renderers.Length == 0) {
			renderers = new EpitaphRenderer[1];
			renderers[0] = gameObject.AddComponent<EpitaphRenderer>();
		}

		foreach (var r in renderers) {
			if (useMaterialAsStartColor) {
				depoweredColor = r.GetMainColor();
				depoweredEmission = r.GetColor("_EmissionColor");
			}
			else {
				r.SetMainColor(depoweredColor);
				r.SetColor("_EmissionColor", depoweredEmission);
			}
		}

		switch (timing) {
			case ActivationTiming.OnPowerBegin:
				powerTrailToReactTo.OnPowerBegin += PowerOn;
				powerTrailToReactTo.OnDepowerFinish += PowerOff;
				break;
			case ActivationTiming.OnPowerFinish:
				powerTrailToReactTo.OnPowerFinish += PowerOn;
				powerTrailToReactTo.OnDepowerBegin += PowerOff;
				break;
			case ActivationTiming.OnDepowerBegin:
				powerTrailToReactTo.OnDepowerBegin += PowerOn;
				powerTrailToReactTo.OnPowerFinish += PowerOff;
				break;
			case ActivationTiming.OnDepowerFinish:
				powerTrailToReactTo.OnDepowerFinish += PowerOn;
				powerTrailToReactTo.OnPowerBegin += PowerOff;
				break;
		}
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

			foreach (var r in renderers) {
				r.SetMainColor(Color.Lerp(depoweredColor, poweredColor, colorChangeAnimationCurve.Evaluate(t)));
				r.SetColor("_EmissionColor", Color.Lerp(depoweredEmission, poweredEmission, colorChangeAnimationCurve.Evaluate(t)));
			}

			yield return null;
		}

		foreach (var r in renderers) {
			r.SetMainColor(poweredColor);
		}
	}

	IEnumerator PowerOffCoroutine() {
		float timeElapsed = 0;
		while (timeElapsed < timeToChangeColor) {
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / timeToChangeColor;

			foreach (var r in renderers) {
				r.SetMainColor(Color.Lerp(poweredColor, depoweredColor, colorChangeAnimationCurve.Evaluate(t)));
				r.SetColor("_EmissionColor", Color.Lerp(poweredEmission, depoweredEmission, colorChangeAnimationCurve.Evaluate(t)));
			}

			yield return null;
		}

		foreach (var r in renderers) {
			r.SetMainColor(depoweredColor);
		}
	}
}
