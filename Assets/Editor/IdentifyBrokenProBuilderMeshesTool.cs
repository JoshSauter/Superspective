using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Editor {
    public class IdentifyBrokenProBuilderMeshesTool : EditorWindow {
        
        [MenuItem("Tools/ProBuilder/Repair/Identify Broken ProBuilder Meshes")]
        public static void FindBrokenPBMeshesInSelection() {
            int brokenCount = 0;
            ProBuilderMesh[] pbMeshes = Selection.gameObjects.Length == 0 ? FindObjectsOfType<ProBuilderMesh>() : Selection.gameObjects.OfType<ProBuilderMesh>().ToArray();
            
            foreach (ProBuilderMesh pbMesh in pbMeshes) {
                if (pbMesh) {
                    try {
                        MeshValidation.RemoveDegenerateTriangles(pbMesh);
                    }
                    catch {
                        brokenCount++;
                        Debug.LogError($"Found broken mesh on {pbMesh.name}", pbMesh);
                    }
                }
            }
            
            if (brokenCount == 0) {
                Debug.Log("<color=green>No broken ProBuilderMeshes found in selection!</color>");
            }
            else {
                Debug.LogWarning($"<color=red>Found {brokenCount} broken ProBuilderMeshes in selection</color>");
            }
        }
    
        [MenuItem("Tools/ProBuilder/Repair/Unfuck Broken ProBuilder Meshes")]
        public static void UnfuckBrokenProbuilderMeshes() {
            OpenAllScenesTool.OpenAllScenes();
            
            EditorApplication.ExecuteMenuItem("Tools/ProBuilder/Repair/Rebuild All ProBuilder Objects");
            
            OpenAllScenesTool.CloseExtraScenes();
        }
    }
}
