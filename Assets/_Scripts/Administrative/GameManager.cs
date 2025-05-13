using System;
using System.Collections;
using System.Collections.Generic;
using DeveloperConsole;
using LevelManagement;
using NaughtyAttributes;
using NovaMenuUI;
using UnityEngine;
using Saving;
using Sirenix.OdinInspector;
using Tayx.Graphy;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : Singleton<GameManager> {
    [SerializeField]
    private GameObject novaUI;
    private bool _isApplicationQuitting = false;
    public bool IsApplicationQuitting => _isApplicationQuitting;

    [Header("--- Game Version ---")]
    [NaughtyAttributes.OnValueChanged("OnVersionChange")]
    public string version;

    // Can be set w/ develop console command, used as the base timescale for the game
    [ShowInInspector]
    public static float timeScale = 1f;

    public bool justResumed = false;
    private void OnApplicationFocus(bool hasFocus) {
        Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !hasFocus;
        if (hasFocus) {
            justResumed = true;
        }
    }

    private void Update() {
        if (justResumed) {
            justResumed = false;
        }
    }

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
    [ShowNativeProperty]
    public bool IsCurrentlyLoading =>
        !gameHasLoaded ||
        LevelManager.instance.IsCurrentlySwitchingScenes ||
        LevelManager.instance.isCurrentlySwitchingScenes ||
        SaveManager.isCurrentlyLoadingSave;

    public bool IsCurrentlyPaused => NovaPauseMenu.instance.PauseMenuIsOpen || DevConsoleBehaviour.instance.IsActive;

    private void Awake() {
        // Hack so that I don't always have to have NovaUI enabled before pressing Play
        novaUI.SetActive(true);
        settingsHaveLoaded = false;
    }

    public bool gameHasLoaded = false;
    public static bool firstLaunch = true;
    public bool settingsHaveLoaded = false;
    IEnumerator Start() {
        MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.On;
        SaveManager.GetOrCreateSaveManagerForLevel(gameObject.scene.name.ToLevel());
        SaveManager.LoadSettings();
        settingsHaveLoaded = true;
        yield return new WaitWhile(() => LevelManager.instance.activeSceneName == "");
        yield return new WaitUntil(() => !LevelManager.instance.IsCurrentlySwitchingScenes);
        yield return new WaitUntil(() => !LevelManager.instance.isCurrentlySwitchingScenes);
        yield return new WaitForSeconds(1f);
        gameHasLoaded = true;
        firstLaunch = false;
        // Disable if you add a title screen or something where you don't want the mouse locked for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (MainCanvas.instance.blackOverlayState == MainCanvas.BlackOverlayState.On) {
            MainCanvas.instance.blackOverlayState = MainCanvas.BlackOverlayState.FadingOut;
        }
    }

    public void RestartGame() {
        Time.timeScale = 1f;
        SuperspectivePhysics.ResetState();
        SaveManager.ClearAllState();
        gameHasLoaded = false;
        SceneManager.LoadScene(this.gameObject.scene.buildIndex);
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
