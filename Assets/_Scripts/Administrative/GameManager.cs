using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using UnityEngine;
using Saving;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : Singleton<GameManager> {
    public bool gameHasLoaded = false;
    IEnumerator Start() {
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
        SaveManager.GetOrCreateSaveManagerForScene(gameObject.scene.name);
        yield return new WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
        yield return new WaitForSeconds(1f);
        gameHasLoaded = true;
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
    }

    void Update() {
        
    }

	public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void UseQWERTYLayout() {
        KeyboardAndMouseInputs.UseKeyboardLayoutPreset(KeyboardAndMouseInputs.KeyboardLayoutPreset.QWERTY);
	}

    public void UseAZERTYLayout() {
        KeyboardAndMouseInputs.UseKeyboardLayoutPreset(KeyboardAndMouseInputs.KeyboardLayoutPreset.AZERTY);
    }

    public void SaveGame() {
        SaveManager.Save("Save1");
	}

    public void LoadGame() {
        SaveManager.Load("Save1");
	}
}
