using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DimensionPillar))]
[RequireComponent(typeof(EpitaphRenderer))]
public class ChangeColorsOnPillarActive : MonoBehaviour {
	public bool changeColorsOnActive;
	public bool changeColorsOnInactive;

	public Color activeColor = new Color(0.372549f, 0.1215686f, 0.9058824f);
	public Color inactiveColor = new Color(.2f, .2f, .2f);

	DimensionPillar thisPillar;
	EpitaphRenderer thisRenderer;
	GlassGlow optionalGlass;

	// Use this for initialization
	void Start () {
		thisPillar = GetComponent<DimensionPillar>();
		thisRenderer = GetComponent<EpitaphRenderer>();
		optionalGlass = GetComponentInChildren<GlassGlow>();
		DimensionPillar.OnActivePillarChanged += ChangeColors;
	}

	void ChangeColors(DimensionPillar previousPillar) {
		if (changeColorsOnActive && DimensionPillar.activePillar == thisPillar) {
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
