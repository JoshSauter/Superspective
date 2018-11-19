using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ObscurePillar))]
[RequireComponent(typeof(EpitaphRenderer))]
public class ChangeColorsOnPillarActive : MonoBehaviour {
	public bool changeColorsOnActive;
	public bool changeColorsOnInactive;

	public Color activeColor = new Color(0.372549f, 0.1215686f, 0.9058824f);
	public Color inactiveColor = new Color(.2f, .2f, .2f);

	ObscurePillar thisPillar;
	EpitaphRenderer thisRenderer;
	GlassGlow optionalGlass;

	// Use this for initialization
	void Start () {
		thisPillar = GetComponent<ObscurePillar>();
		thisRenderer = GetComponent<EpitaphRenderer>();
		optionalGlass = GetComponentInChildren<GlassGlow>();
		ObscurePillar.OnActivePillarChanged += ChangeColors;
	}

	void ChangeColors(ObscurePillar previousPillar) {
		if (changeColorsOnActive && ObscurePillar.activePillar == thisPillar) {
			thisRenderer.SetMainColor(activeColor);
			if (optionalGlass != null) {
				optionalGlass.enabled = true;
				optionalGlass.glowColor = activeColor;
			}
		}
		else if (changeColorsOnInactive && previousPillar == thisPillar) {
			thisRenderer.SetMainColor(inactiveColor);
			if (optionalGlass != null) {
				optionalGlass.enabled = false;
			}
		}
	}
}
