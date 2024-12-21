using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LevelManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class OpenAllScenesTool {
    static string GetScenePathByName(string sceneName) {
        foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes) {
            if (editorScene.enabled && Path.GetFileNameWithoutExtension(editorScene.path) == sceneName) {
                return editorScene.path;
            }
        }

        return default;
    }
    
    [MenuItem("My Tools/Scenes/Open All Scenes")]
    public static void OpenAllScenes() {
        foreach (var level in LevelManager.instance.allLevels) {
            try {
                string sceneName = level.level.ToName();
                Scene scene = EditorSceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid()) {
                    EditorSceneManager.OpenScene(GetScenePathByName(sceneName), OpenSceneMode.Additive);
                }
            }
            catch (Exception e) {
                Debug.LogError($"Error opening scene {level.level}: {e}");
            }
        }
    }

    [MenuItem("My Tools/Scenes/Save and Close Extra Scenes")]
    public static void CloseExtraScenes() {
        foreach (var level in LevelManager.instance.allLevels) {
            try {
                string sceneName = level.level.ToName();
                Scene scene = EditorSceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isDirty) {
                    EditorSceneManager.SaveScene(scene);
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
        
        // Closes all scenes that aren't the ManagerScene nor any scene that should be loaded based on the startingLevel
        LevelManager.instance.ChangeLevelInEditor();
    }
}
