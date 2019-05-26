using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EpitaphRenderer))]
public class GlassGlow : MonoBehaviour {
	public Color glowColor = Color.white;
	Color startEmissionColor;

	const string emissionColorName = "_EmissionColor";
	EpitaphRenderer thisRenderer;

	// Use this for initialization
	void Start () {
		thisRenderer = GetComponent<EpitaphRenderer>();
		startEmissionColor = thisRenderer.GetColor(emissionColorName);
	}
	
	// Update is called once per frame
	void Update () {
		Color emissionColor = glowColor * (0.5f * Mathf.Sin(Time.time) + 0.5f);
		thisRenderer.SetColor(emissionColorName, emissionColor);
	}

	private void OnDisable() {
		thisRenderer.SetColor(emissionColorName, startEmissionColor);
	}
}
