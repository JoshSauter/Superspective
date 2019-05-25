using UnityEditor;
using UnityEngine;
using System.Collections;

[InitializeOnLoad]
public static class ToggleSceneViewFxOnPlayMode  {

	static ToggleSceneViewFxOnPlayMode() {
		EditorApplication.playModeStateChanged += HandlePlayModeStateChange;
		EditorApplication.projectChanged += TemporarilyDisableFx;
	}

	private static void HandlePlayModeStateChange(PlayModeStateChange newState) {
		if (SceneViewFX.instance == null) return;

		switch (newState) {
			case PlayModeStateChange.ExitingEditMode:
				TemporarilyDisableFx();
				break;
			case PlayModeStateChange.EnteredPlayMode:
				RestoreFx();
				break;
			case PlayModeStateChange.ExitingPlayMode:
				TemporarilyDisableFx();
				break;
			case PlayModeStateChange.EnteredEditMode:
				RestoreFx();
				break;
		}
	}

	private static void TemporarilyDisableFx() {
		SceneViewFX.instance.cachedEnableState = SceneViewFX.instance.enabled;
		SceneViewFX.instance.enabled = false;
	}

	private static void RestoreFx() {
		SceneViewFX.instance.enabled = SceneViewFX.instance.cachedEnableState;
	}
}
