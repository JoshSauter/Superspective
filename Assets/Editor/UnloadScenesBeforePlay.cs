using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
class UnloadScenesBeforePlay {
    const string playerPrefsKey = "StoredLevelsFromEditMode";
    
    static UnloadScenesBeforePlay() {
        EditorApplication.playModeStateChanged += UnloadAllScenesButManagerScene;
    }

    static void UnloadAllScenesButManagerScene(PlayModeStateChange state) {
        switch (state) {
            case PlayModeStateChange.EnteredEditMode:
                // Reload all scenes except manager scene after returning to edit mode
                ReloadAllScenesExcept("_ManagerScene");
                break;
            case PlayModeStateChange.ExitingEditMode:
                // Unload all scenes except manager scene before entering play mode
                PlayerPrefs.SetString(playerPrefsKey, "");
                UnloadAllScenesExcept("_ManagerScene");
                break;
            case PlayModeStateChange.EnteredPlayMode:
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    
    static void UnloadAllScenesExcept(string sceneName) {
        int numScenes = SceneManager.sceneCount;
        List<Scene> scenesThatGotUnloaded = new List<Scene>();
        for (int i = 0; i < numScenes; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != sceneName) {
                scenesThatGotUnloaded.Add(scene);
            }
        }

        string saveData = string.Join(",", scenesThatGotUnloaded.Select(s => s.path));
        
        foreach (var sceneToUnload in scenesThatGotUnloaded) {
            try {
                if (sceneToUnload.isDirty) {
                    EditorSceneManager.SaveScene(sceneToUnload);
                }
                EditorSceneManager.CloseScene(sceneToUnload, true);
            }
            catch (Exception e) {
                Debug.LogError($"Failed unloading scene {sceneToUnload}, details: {e}");
            }
        }
        
        PlayerPrefs.SetString(playerPrefsKey, saveData);
    }

    static void ReloadAllScenesExcept(string sceneName) {
        List<String> scenesThatGotUnloaded = PlayerPrefs.GetString(playerPrefsKey).Split(',').ToList();
        foreach (var scene in scenesThatGotUnloaded) {
            if (scene == sceneName) continue;
            
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
        }
    }
}
