using System;
using System.Collections;
using System.Collections.Generic;
using LevelManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class OpenAllScenesTool {
    [MenuItem("My Tools/Scenes/Open All Scenes")]
    public static void OpenAllScenes() {
        foreach (var level in LevelManager.instance.allLevels) {
            try {
                string sceneName = level.level.ToName();
                Scene scene = EditorSceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid()) {
                    string path = $"Assets/{(sceneName != Levels.TestScene.ToName() ? "__Scenes" : "PrototypeAndTesting")}/{sceneName}.unity";
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
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
        LevelManager.instance.LoadDefaultPlayerPosition();
    }
}
