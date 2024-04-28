using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : Singleton<MainCanvas> {
	[SerializeField]
	private Canvas canvas;
	public Image blackOverlay;
	public enum BlackOverlayState {
		Off,
		On,
		FadingOut
	}

	BlackOverlayState _blackOverlayState = BlackOverlayState.Off;
	public BlackOverlayState blackOverlayState {
		get => _blackOverlayState;
		set {
			if (value != BlackOverlayState.Off) {
				BlackOverlayAlpha = 1f;
				timeElapsedSinceStateChange = 0f;
			}
			_blackOverlayState = value;
		}
	}

	float timeElapsedSinceStateChange = 0f;
	float blackOverlayFadeTime = 2f;

	public float BlackOverlayAlpha {
		get => blackOverlay.color.a;
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
			BlackOverlayAlpha = nextAlpha;

			if (BlackOverlayAlpha == 0) {
				blackOverlayState = BlackOverlayState.Off;
			}
		}
	}

	private void Update() {
		if (!GameManager.instance.gameHasLoaded) return;

		bool canvasIsVisible = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
		bool pauseMenuIsOpen = NovaPauseMenu.instance.PauseMenuIsOpen;
		if (pauseMenuIsOpen == canvasIsVisible) {
			// Hack to easily hide the canvas when the pause menu is open
			canvas.renderMode = pauseMenuIsOpen ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
		}
	}
}
