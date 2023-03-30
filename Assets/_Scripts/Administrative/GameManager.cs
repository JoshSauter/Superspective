using System;
using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using Tayx.Graphy;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : Singleton<GameManager> {
    [SerializeField]
    private GameObject pauseMenu;
    private bool _isApplicationQuitting = false;
    public bool IsApplicationQuitting => _isApplicationQuitting;

    [Header("--- Game Version ---")]
    [OnValueChanged("OnVersionChange")]
    public string version;

    private void OnVersionChange() {
        #if UNITY_EDITOR
        if (string.IsNullOrEmpty(version)) return;

        if (PlayerSettings.bundleVersion != version) {
            Debug.Log($"Updating version from {PlayerSettings.bundleVersion} to {version}");
            PlayerSettings.bundleVersion = version;
        }
        #endif
    }

    // There are lots of ways the game can be in a loading state, this aggregates all of them into one
    public bool IsCurrentlyLoading =>
        !gameHasLoaded ||
        LevelManager.instance.IsCurrentlyLoadingScenes ||
        LevelManager.instance.isCurrentlySwitchingScenes ||
        SaveManager.isCurrentlyLoadingSave;

    public bool IsCurrentlyPaused => NovaPauseMenu.instance.PauseMenuIsOpen;

    private void Awake() {
        // Hack so that I don't always have to have NovaUI enabled before pressing Play
        pauseMenu.SetActive(true);
    }

    public bool gameHasLoaded = false;
    IEnumerator Start() {
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
        SaveManager.GetOrCreateSaveManagerForScene(gameObject.scene.name);
        SaveManager.LoadSettings();
        yield return new WaitWhile(() => LevelManager.instance.activeSceneName == "");
        yield return new WaitUntil(() => !LevelManager.instance.IsCurrentlyLoadingScenes);
        yield return new WaitUntil(() => !LevelManager.instance.isCurrentlySwitchingScenes);
        yield return new WaitForSeconds(1f);
        gameHasLoaded = true;
        // Disable if you add a title screen or something where you don't want the mouse locked for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (MainCanvas.instance.blackOverlayState == MainCanvas.BlackOverlayState.On) {
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
        }
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
}
