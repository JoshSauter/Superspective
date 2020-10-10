using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : Singleton<MainCanvas> {
	public TempMenu tempMenu;
	public Image blackOverlay;
	private bool _blackOverlayEnabled = false;
	public bool blackOverlayEnabled {
		get { return _blackOverlayEnabled; }
		set {
			blackOverlayAlpha = 1f;
			_blackOverlayEnabled = value;
		}
	}
	float blackOverlayFadeSpeed = 4f;

	public float blackOverlayAlpha {
		get { return blackOverlay.color.a; }
		set {
			Color col = blackOverlay.color;
			col.a = value;
			blackOverlay.color = col;
		}
	}

	private void Update() {
		if (!blackOverlayEnabled && blackOverlayAlpha > 0) {
			float nextAlpha = Mathf.Lerp(blackOverlayAlpha, 0f, blackOverlayFadeSpeed * Time.deltaTime);
			if (nextAlpha < 0.001f) nextAlpha = 0f;
			blackOverlayAlpha = nextAlpha;
		}
	}
}
