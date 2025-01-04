using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.Tools {
    public class PhysicsDebug : EditorWindow {
        private static bool windowOpen = false;
        public static string collisionName;
    
        [MenuItem("My Tools/Superspective Physics/Print Filtered Collisions")]
        public static void ShowWindow() {
            // Opens or focuses the window
            var window = GetWindow<PhysicsDebug>("Physics Debug");
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
            GUILayout.Label("Physics Debug", EditorStyles.boldLabel);
            
            collisionName = EditorGUILayout.TextField("Collision Name Filter", collisionName);

            // Confirm button to open the selected level
            if (GUILayout.Button("Show all cached collisions matching name")) {
                foreach (var cachedCollisionDebugString in SuperspectivePhysics.IgnoredColliderPairsDebugString().Where(s => s.Contains(collisionName))) {
                    Debug.Log(cachedCollisionDebugString);
                }
            }
        }
    }
}