using System;
using Editor;
using UnityEngine;
using UnityEditor;

public class CleanupScenesBeforeBuild : EditorWindow {
    private static bool windowOpen = false;

    [MenuItem("My Tools/Cleanup Scenes Before Build Tool &b")]
    public static void ShowWindow() {
        var window = GetWindow<CleanupScenesBeforeBuild>("Cleanup Before Build Tool (Alt+B)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        } else {
            window.Show();
            windowOpen = true;
        }
    }

    private void OnDestroy() {
        windowOpen = false;
    }

    private void OnGUI() {
        GUILayout.Label("Scene Cleanup Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Fix layers for CullEverything objects in all scenes")) {
            FixLayersForCullEverythingObjects();
        }
        
        if (GUILayout.Button("Fix broken ProBuilder meshes in all scenes")) {
            IdentifyBrokenProBuilderMeshesTool.UnfuckBrokenProbuilderMeshes();
        }

        if (GUILayout.Button("Turn off all DEBUG flags in open scenes")) {
            SelectOrDisableAllDebug.Execute(true);
        }
        
        // TODO: Add other cleanup tasks here, formalize this tool
    }

    private static void FixLayersForCullEverythingObjects() {
        OpenAllScenesTool.OpenAllScenes();
        
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        int cullEverythingLayer = LayerMask.NameToLayer("CullEverythingLayer");
        foreach (Renderer renderer in allRenderers) {
            if (renderer == null || renderer.sharedMaterials == null || renderer.gameObject == null || renderer.sharedMaterial) continue;
            try {
                if (renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial.name.Contains("CullEverything")) {
                    renderer.gameObject.layer = cullEverythingLayer;
                }
            }
            catch (Exception e) {
                Debug.LogError($"Error processing {renderer.gameObject.name}: {e}", renderer.gameObject);
            }
        }
        
        OpenAllScenesTool.CloseExtraScenes();
    }
}
