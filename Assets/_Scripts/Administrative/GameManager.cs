using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour {
    void Start() {
        
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
}
