using System.Collections;
using System.Collections.Generic;
using Saving;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MigrateSaveObjectInitialState : EditorWindow {
    private static bool windowOpen = false;
    
    // Add a menu item and shortcut (Ctrl+Shift+H) to open the tool
    [MenuItem("Tools/Migrate Superspective Object Initial State %#h")]  // % is Ctrl, # is Shift, & is Alt
    public static void ShowWindow() {
        // Opens or focuses the window
        var window = GetWindow<MigrateSaveObjectInitialState>("Migrate SuperspectiveObject Initial State (Ctrl+Shift+H)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        }
        else {
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
        GUILayout.Label("Migrate Superspective Object Initial State", EditorStyles.boldLabel);

        // Confirm button to open the selected level
        if (GUILayout.Button("Do Migration")) {
            DoMigration();
        }
    }

    private static void DoMigration() {
        // Step 1: Open all scenes
        OpenAllScenesTool.OpenAllScenes();
        
        // Step 2: Find all SuperspectiveObjects
        var superspectiveObjects = FindObjectsOfType<SuperspectiveObject>();
        
        // Step 3: Migrate the initial state
        foreach (var superspectiveObject in superspectiveObjects) {
            superspectiveObject.gameObjectStartsInactive = !superspectiveObject.gameObject.activeSelf;
            superspectiveObject.scriptStartsDisabled = !superspectiveObject.enabled;
            superspectiveObject._startPosition = superspectiveObject.transform.position;
            superspectiveObject._startRotation = superspectiveObject.transform.rotation;
            
            EditorSceneManager.MarkSceneDirty(superspectiveObject.gameObject.scene);
        }
        
        // Step 4: Save all scenes
        OpenAllScenesTool.CloseExtraScenes();
    }
}
