using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Editor {
    public class IdentifyBrokenProBuilderMeshesTool : EditorWindow {
        
        [MenuItem("Tools/ProBuilder/Repair/Identify Broken ProBuilder Meshes")]
        public static void FindBrokenPBMeshesInSelection() {
            int brokenCount = 0;
            foreach (var go in Selection.gameObjects) {
                ProBuilderMesh pbMesh = go.GetComponent<ProBuilderMesh>();
                if (pbMesh) {
                    try {
                        MeshValidation.RemoveDegenerateTriangles(pbMesh);
                    }
                    catch {
                        brokenCount++;
                        Debug.LogError($"Found broken mesh on {go.name}", go);
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
    }
}