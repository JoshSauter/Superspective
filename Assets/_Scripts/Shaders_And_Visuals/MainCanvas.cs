using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : Singleton<MainCanvas> {
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
				timeElapsedSinceStateChange = 0f;
			}
			_blackOverlayState = value;
		}
	}

	float timeElapsedSinceStateChange = 0f;
	float blackOverlayFadeTime = 2f;

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
			timeElapsedSinceStateChange += Time.fixedDeltaTime;
			float t = timeElapsedSinceStateChange / blackOverlayFadeTime;
			float nextAlpha = Mathf.Lerp(1.0f, 0.0f, t * t);
			if (nextAlpha < 0.001f) nextAlpha = 0f;
			blackOverlayAlpha = nextAlpha;

			if (blackOverlayAlpha == 0) {
				blackOverlayState = BlackOverlayState.Off;
			}
		}
	}
}
