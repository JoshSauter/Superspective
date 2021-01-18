using System.Threading.Tasks;
using UnityEditor;

[InitializeOnLoad]
public static class ToggleSceneViewFxOnPlayMode {
    static SceneViewFXController controller;

    static ToggleSceneViewFxOnPlayMode() {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChange;
        EditorApplication.pauseStateChanged += HandlePauseStateChange;
        EditorApplication.projectChanged += TemporarilyDisableFx;

        controller = SceneViewFXController.instance;
    }

    static void HandlePlayModeStateChange(PlayModeStateChange newState) {
        if (SceneViewFX.instance == null) return;

        switch (newState) {
            case PlayModeStateChange.ExitingEditMode:
                TemporarilyDisableFx();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                if (!GameWindow.instance.maximizeOnPlay) RestoreFx();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                TemporarilyDisableFx();
                break;
            case PlayModeStateChange.EnteredEditMode:
                RestoreFx();
                break;
        }
    }

    // On Pause, if we were maximized and the cachedEnableState is on, enable the SceneViewFX
    // On Unpause, if we were maximized and the SceneViewFX is on, cache their state and turn SceneViewFX off
    static void HandlePauseStateChange(PauseState pauseStatus) {
        if (GameWindow.instance.maximizeOnPlay && EditorApplication.isPlaying) {
            // Paused
            if (pauseStatus == PauseState.Paused) {
                controller = SceneViewFXController.instance;
                RestoreFxAsync(1500);
            }
            // Unpaused
            else
                TemporarilyDisableFx();
        }
    }

    static void TemporarilyDisableFx() {
        if (SceneViewFX.instance != null) {
            SceneViewFX.instance.cachedEnableState = SceneViewFX.instance.enabled;
            SceneViewFX.instance.enabled = false;
        }
    }

    static void RestoreFx() {
        if (SceneViewFX.instance != null) SceneViewFX.instance.enabled = SceneViewFX.instance.cachedEnableState;
    }

    static async void RestoreFxAsync(int delayMs) {
        await Task.Delay(delayMs).ConfigureAwait(false);
        controller.needsToBeRestored = true;
    }
}