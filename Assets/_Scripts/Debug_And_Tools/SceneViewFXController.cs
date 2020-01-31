using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneViewFXController : Singleton<SceneViewFXController> {

#if UNITY_EDITOR
	// Only set to true after a delay after pausing from fullscreen (referenced from ToggleSceneViewFxOnPlayMode.cs)
	public bool needsToBeRestored = false;
	public bool needsToBeRepaused = false;
	public bool wasMaximized = false;

	private void OnDrawGizmos() {
		if (needsToBeRepaused) {
			needsToBeRepaused = false;
			PauseAndRestoreMaximizeOnPlay();
		}
		if (needsToBeRestored) {
			needsToBeRestored = false;
			RestoreFxFromFullScreenPause();
		}
	}

	private static void RestoreFx() {
		if (SceneViewFX.instance != null) {
			SceneViewFX.instance.enabled = SceneViewFX.instance.cachedEnableState;
		}
	}

	private static void RestoreFxFromFullScreenPause() {
		if (SceneViewFX.instance != null) {
			instance.wasMaximized = GameWindow.instance.maximizeOnPlay;
			GameWindow.instance.maximizeOnPlay = false;
			SceneViewFX.instance.enabled = SceneViewFX.instance.cachedEnableState;
			EditorApplication.isPaused = false;
			instance.needsToBeRepaused = true;
		}
	}

	private static void PauseAndRestoreMaximizeOnPlay() {
		if (SceneViewFX.instance != null) {
			EditorApplication.isPaused = true;
			GameWindow.instance.maximizeOnPlay = instance.wasMaximized;
		}
	}
#endif
}
