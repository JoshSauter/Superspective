using System.Linq;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Editor {
    public class IdentifyBrokenProBuilderMeshesTool : EditorWindow {
        
        [MenuItem("Tools/ProBuilder/Repair/Identify Broken ProBuilder Meshes")]
        public static void FindBrokenPBMeshesInSelection() {
            int brokenCount = 0;
            var originalSelection = Selection.objects;
            ProBuilderMesh[] pbMeshes = Selection.gameObjects.Length == 0 ? FindObjectsOfType<ProBuilderMesh>() : Selection.gameObjects.OfType<ProBuilderMesh>().ToArray();
            
            foreach (ProBuilderMesh pbMesh in pbMeshes) {
                if (pbMesh) {
                    Selection.activeGameObject = pbMesh.gameObject;
                    try {
                        MeshValidation.RemoveDegenerateTriangles(pbMesh);
                        EditorApplication.ExecuteMenuItem("Tools/ProBuilder/Repair/Fix Meshes in Selection");
                    }
                    catch {
                        brokenCount++;
                        Debug.LogError($"Found broken mesh on {pbMesh.FullPath()}", pbMesh);
                    }
                }
            }
            
            if (brokenCount == 0) {
                Debug.Log("<color=green>No broken ProBuilderMeshes found in selection!</color>");
            }
            else {
                Debug.LogWarning($"<color=red>Found {brokenCount} broken ProBuilderMeshes in selection</color>");
            }

            Selection.objects = originalSelection;
        }
    
        [MenuItem("Tools/ProBuilder/Repair/Fix Broken ProBuilder Meshes Everywhere For Build")]
        public static void UnfuckBrokenProbuilderMeshes() {
            OpenAllScenesTool.OpenAllScenes();
            
            EditorApplication.ExecuteMenuItem("Tools/ProBuilder/Repair/Rebuild All ProBuilder Objects");
            
            OpenAllScenesTool.CloseExtraScenes();
        }
    }
}
