using LevelManagement;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SelectSceneTool : EditorWindow {
    private static Levels selectedLevel = Levels.ManagerScene;  // Default selection
    private static bool windowOpen = false;

    private static List<Levels> sceneHistory = new List<Levels>(); // Last 4 scenes

    private const string SceneHistoryKey = "SelectSceneTool_RecentScenes"; // Key for persistence

    [MenuItem("Tools/Select Scene Tool %l")]
    public static void ShowWindow() {
        var window = GetWindow<SelectSceneTool>("Select Level Tool (Ctrl+L)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        } else {
            selectedLevel = LevelManager.instance.startingScene;
            window.LoadSceneHistory(); // Load saved history
            window.Show();
            windowOpen = true;
        }
    }

    private void OnDestroy() {
        windowOpen = false;
    }

    private void OnGUI() {
        GUILayout.Label("Select a Level", EditorStyles.boldLabel);

        // Dropdown to select a level from the enum
        selectedLevel = (Levels)EditorGUILayout.EnumPopup("Level", selectedLevel);

        if (GUILayout.Button("Open")) {
            OpenSelectedScene();
        }

        if (GUILayout.Button("Open All")) {
            OpenAllScenes();
        }

        if (GUILayout.Button("Save and Close Extra Scenes")) {
            OpenAllScenesTool.CloseExtraScenes();
        }

        // Display the history of last 4 scenes
        GUILayout.Space(10);
        GUILayout.Label("Last 4 Scenes", EditorStyles.boldLabel);

        if (sceneHistory.Count > 0) {
            List<Levels> sceneHistoryCopy = new List<Levels>(sceneHistory); // Copy to avoid modifying the original list
            foreach (Levels level in sceneHistoryCopy) {
                // Nicify the enum name for button labels
                string buttonText = ObjectNames.NicifyVariableName(level.ToString());
                
                if (GUILayout.Button(buttonText)) {
                    SwitchToScene(level);
                }
            }
        } else {
            GUILayout.Label("No recent scenes", EditorStyles.helpBox);
        }
    }

    private void OpenSelectedScene() {
        LevelManager.instance.startingScene = selectedLevel;
        LevelManager.instance.ChangeLevelInEditor();
        EditorSceneManager.MarkSceneDirty(LevelManager.instance.gameObject.scene);
        UpdateSceneHistory(selectedLevel);
    }

    private void OpenAllScenes() {
        OpenAllScenesTool.OpenAllScenes();
    }

    private void SwitchToScene(Levels level) {
        selectedLevel = level;
        OpenSelectedScene(); // Reuse the existing OpenSelectedScene logic
    }

    private void UpdateSceneHistory(Levels level) {
        if (sceneHistory.Contains(level)) {
            sceneHistory.Remove(level);
        }

        sceneHistory.Insert(0, level);

        if (sceneHistory.Count > 4) {
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
        }

        SaveSceneHistory(); // Save to EditorPrefs
    }

    private void SaveSceneHistory() {
        string serializedHistory = string.Join(",", sceneHistory);
        EditorPrefs.SetString(SceneHistoryKey, serializedHistory);
    }

    private void LoadSceneHistory() {
        if (EditorPrefs.HasKey(SceneHistoryKey)) {
            string serializedHistory = EditorPrefs.GetString(SceneHistoryKey);

            sceneHistory = new List<Levels>();
            foreach (string levelName in serializedHistory.Split(',')) {
                if (System.Enum.TryParse(levelName, out Levels level)) {
                    sceneHistory.Add(level);
                }
            }
        }
    }
}
