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
    [MenuItem("My Tools/Open All Scenes")]
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
}
