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
    const string PLAYER_PREFS_KEY = "StoredLevelsFromEditMode";
    
    static UnloadScenesBeforePlay() {
        EditorApplication.playModeStateChanged += UnloadAllScenesButManagerScene;
    }

    static void UnloadAllScenesButManagerScene(PlayModeStateChange state) {
        switch (state) {
            case PlayModeStateChange.EnteredEditMode:
                // Reload all scenes except manager scene after returning to edit mode
                ReloadAllScenesExcept(LevelManager.ManagerScene);
                break;
            case PlayModeStateChange.ExitingEditMode:
                // Unload all scenes except manager scene before entering play mode
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, "");
                UnloadAllScenesExcept(LevelManager.ManagerScene);
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
        HashSet<string> scenesThatShouldRemainLoaded = LevelManager.instance.allLevels
            .Find(lvl => lvl.level == LevelManager.instance.startingScene).connectedLevels
            .Append(LevelManager.instance.startingScene)
            .Select(lvl => lvl.ToName())
            .ToHashSet();

        int numScenes = SceneManager.sceneCount;
        List<Scene> scenesToUnload = new List<Scene>();
        List<Scene> dirtyScenes = new List<Scene>();
        bool saveAllScenes = false; // Tracks whether Save All was selected in the dialog
        for (int i = 0; i < numScenes; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            
            if (scene.name != sceneName && !scenesThatShouldRemainLoaded.Contains(scene.name)) {
                scenesToUnload.Add(scene);
                if (scene.isDirty) {
                    dirtyScenes.Add(scene);
                }
            }
        }

        string saveData = string.Join(",", scenesToUnload.Select(s => s.path));
        
        List<Scene> scenesSaved = new List<Scene>();
        foreach (Scene sceneToUnload in scenesToUnload) {
            
            string dirtySceneNames = string.Join("\n", dirtyScenes.Except(scenesSaved).Select(s => s.name));
            const int SAVE_SCENE = 0;
            const int DONT_SAVE = 1;
            const int SAVE_ALL_SCENES = 2;
            int choice = saveAllScenes ? SAVE_ALL_SCENES : EditorUtility.DisplayDialogComplex(
                "Save Scenes?",
                $"Scene: {sceneToUnload.name}\n\nThe following scenes have unsaved changes:\n{dirtySceneNames}\n\nWhat would you like to do?",
                "Save", // Option 0
                "Don't Save", // Option 1
                "Save All" // Option 2
            );
            
            bool saveScene = false;
            switch (choice) {
                case SAVE_SCENE:
                    saveScene = true;
                    break;
                case DONT_SAVE:
                    break;
                case SAVE_ALL_SCENES:
                    saveScene = true;
                    saveAllScenes = true;
                    break;
            }

            if (saveScene) {
                EditorSceneManager.SaveScene(sceneToUnload);
                scenesSaved.Add(sceneToUnload);
            }
            
            EditorApplication.delayCall += () => {
                try {
                    EditorSceneManager.CloseScene(sceneToUnload, true);
                } catch (Exception e) {
                    Debug.LogError($"Failed unloading scene {sceneToUnload}, details: {e}");
                }
            };
        }
        
        PlayerPrefs.SetString(PLAYER_PREFS_KEY, saveData);
    }

    static void ReloadAllScenesExcept(string sceneName) {
        List<String> scenesThatGotUnloaded = PlayerPrefs.GetString(PLAYER_PREFS_KEY).Split(',').ToList();
        foreach (var scene in scenesThatGotUnloaded) {
            if (scene == sceneName || scene == "") continue;
            
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
        }
    }
}
