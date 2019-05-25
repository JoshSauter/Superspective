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
				if (!maximizeOnPlay) {
					RestoreFx();
				}
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

	public static bool maximizeOnPlay {
		get {
#if UNITY_EDITOR
			var windows = (EditorWindow[])Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
			foreach (var window in windows) {
				if (window != null && window.GetType().FullName == "UnityEditor.GameView") {
					return window.maximized;
				}
			}

			return false;
#endif
		}
	}

}
