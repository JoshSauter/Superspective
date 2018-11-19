using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EpitaphRenderer))]
public class GlassGlow : MonoBehaviour {
	public Color glowColor = Color.white;
	Color startEmissionColor;

	const string emissionColorName = "_EmissionColor";
	EpitaphRenderer renderer;

	// Use this for initialization
	void Start () {
		renderer = GetComponent<EpitaphRenderer>();
		startEmissionColor = renderer.GetColor(emissionColorName);
	}
	
	// Update is called once per frame
	void Update () {
		Color emissionColor = glowColor * (0.5f * Mathf.Sin(Time.time) + 0.5f);
		renderer.SetColor(emissionColorName, emissionColor);
	}

	private void OnDisable() {
		renderer.SetColor(emissionColorName, startEmissionColor);
	}
}
