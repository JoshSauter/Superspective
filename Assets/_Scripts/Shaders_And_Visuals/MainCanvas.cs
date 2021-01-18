using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : Singleton<MainCanvas> {
	public TempMenu tempMenu;
	public Image blackOverlay;
	public enum BlackOverlayState {
		Off,
		On,
		FadingOut
	}

	BlackOverlayState _blackOverlayState = BlackOverlayState.Off;
	public BlackOverlayState blackOverlayState {
		get { return _blackOverlayState; }
		set {
			if (value != BlackOverlayState.Off) {
				blackOverlayAlpha = 1f;
			}
			_blackOverlayState = value;
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

	void FixedUpdate() {
		if (blackOverlayState == BlackOverlayState.FadingOut) {
			float nextAlpha = Mathf.Lerp(blackOverlayAlpha, 0f, blackOverlayFadeSpeed * Time.fixedDeltaTime);
			if (nextAlpha < 0.001f) nextAlpha = 0f;
			blackOverlayAlpha = nextAlpha;

			if (blackOverlayAlpha == 0) {
				blackOverlayState = BlackOverlayState.Off;
			}
		}
	}
}
