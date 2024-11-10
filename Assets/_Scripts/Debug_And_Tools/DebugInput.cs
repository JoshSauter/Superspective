using LevelManagement;
using NovaMenuUI;
using UnityEngine;

public static class DebugInput {
    public static bool isDebugBuildOverride = false;

    public static bool IsDebugBuild => isDebugBuildOverride || Debug.isDebugBuild;
    
    public static bool GetKeyDown(KeyCode keyCode) => IsDebugBuild && Input.GetKeyDown(keyCode);
    public static bool GetKeyDown(string key) => IsDebugBuild && Input.GetKeyDown(key);
    public static bool GetKeyUp(KeyCode keyCode) => IsDebugBuild && Input.GetKeyUp(keyCode);
    public static bool GetKeyUp(string key) => IsDebugBuild && Input.GetKeyUp(key);
    public static bool GetKey(KeyCode keyCode) => IsDebugBuild && Input.GetKey(keyCode);
    public static bool GetKey(string key) => IsDebugBuild && Input.GetKey(key);

    // Ctrl+Shift+W for global "win-the-puzzle" cheatcode (Up to individual puzzles to implement)
    public static bool InstaSolvePuzzle(this Component c) {
        // if (!GameManager.instance.gameHasLoaded) return false;
        
        return IsDebugBuild &&
               !NovaPauseMenu.instance.PauseMenuIsOpen &&
               LevelManager.enumToSceneName[c.gameObject.scene.name] == LevelManager.instance.ActiveScene &&
               Input.GetKey(KeyCode.LeftControl) &&
               Input.GetKey(KeyCode.LeftShift) &&
               Input.GetKeyDown(KeyCode.W);
    }
}
