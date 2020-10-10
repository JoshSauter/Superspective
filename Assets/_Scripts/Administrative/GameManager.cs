using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour {
    void Start() {
        Application.quitting += () => SaveManager.DeleteSave(SaveManager.temp);

        SaveManager.AddSaveManagerForScene(gameObject.scene.name);
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
