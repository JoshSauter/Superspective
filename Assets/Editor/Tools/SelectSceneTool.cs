using LevelManagement;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SelectSceneTool : EditorWindow {
    private static Levels selectedLevel = Levels.ManagerScene;  // Default selection
    private static bool windowOpen = false;

    // Add a menu item and shortcut (Ctrl+L) to open the tool
    [MenuItem("Tools/Select Scene Tool %l")]  // % is Ctrl, # is Shift, & is Alt
    public static void ShowWindow() {
        // Opens or focuses the window
        var window = GetWindow<SelectSceneTool>("Select Level Tool (Ctrl+L)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        }
        else {
            selectedLevel = LevelManager.instance.startingScene;
            window.Show();
            windowOpen = true;
        }
    }
        
    // Reset the flag when the window is destroyed
    private void OnDestroy() {
        windowOpen = false;
    }

    // Draw the GUI for the window
    private void OnGUI() {
        GUILayout.Label("Select a Level", EditorStyles.boldLabel);

        // Dropdown to select a level from the enum
        selectedLevel = (Levels)EditorGUILayout.EnumPopup("Level", selectedLevel);

        // Confirm button to open the selected level
        if (GUILayout.Button("Open")) {
            OpenSelectedScene();
        }

        // Open All button to open all scenes
        if (GUILayout.Button("Open All")) {
            OpenAllScenes();
        }
        
        if (GUILayout.Button("Save and Close Extra Scenes")) {
            OpenAllScenesTool.CloseExtraScenes();
        }
    }

    private void OpenSelectedScene() {
        LevelManager.instance.startingScene = selectedLevel;
        LevelManager.instance.ChangeLevelInEditor();
        EditorSceneManager.MarkSceneDirty(LevelManager.instance.gameObject.scene);
    }

    private void OpenAllScenes() {
        OpenAllScenesTool.OpenAllScenes();
    }
}
