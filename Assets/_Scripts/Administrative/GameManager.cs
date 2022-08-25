using System;
using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using UnityEngine;
using Saving;
using Tayx.Graphy;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : Singleton<GameManager> {
    private bool _isApplicationQuitting = false;
    public bool IsApplicationQuitting => _isApplicationQuitting;
    
    // There are lots of ways the game can be in a loading state, this aggregates all of them into one
    public bool IsCurrentlyLoading =>
        !gameHasLoaded ||
        LevelManager.instance.IsCurrentlyLoadingScenes ||
        LevelManager.instance.isCurrentlySwitchingScenes ||
        SaveManager.isCurrentlyLoadingSave;
    
    public bool gameHasLoaded = false;
    IEnumerator Start() {
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
        SaveManager.GetOrCreateSaveManagerForScene(gameObject.scene.name);
        yield return new WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
        yield return new WaitUntil(() => !LevelManager.instance.isCurrentlySwitchingScenes);
        yield return new WaitForSeconds(1f);
        gameHasLoaded = true;
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;

        Application.targetFrameRate = 60;
    }

	public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnApplicationQuit() {
        _isApplicationQuitting = true;
    }

    public void UseQWERTYLayout() {
        KeyboardAndMouseInputs.UseKeyboardLayoutPreset(KeyboardAndMouseInputs.KeyboardLayoutPreset.QWERTY);
	}

    public void UseAZERTYLayout() {
        KeyboardAndMouseInputs.UseKeyboardLayoutPreset(KeyboardAndMouseInputs.KeyboardLayoutPreset.AZERTY);
    }

    public void LoadGame() {
        SaveManager.Load("Save1");
	}
}
