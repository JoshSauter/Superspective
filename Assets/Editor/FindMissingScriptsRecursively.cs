using System.Collections.Generic;
using System.Linq;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

namespace FLGCoreEditor.Utilities {
    public class FindMissingScriptsRecursively : EditorWindow {
        static int _goCount;
        private static GameObject[] prevSelection;

        static bool _bHaveRun;

        public void OnGUI() {
            if (GUILayout.Button("Find Missing Scripts in selected GameObjects")) FindInSelected();

            if (!_bHaveRun) return;

            EditorGUILayout.TextField($"{_goCount} GameObjects Selected");
            if (prevSelection != null) {
                if (GUILayout.Button("Reselect from previous selection")) {
                    ReselectPrevSelection();
                }
            }
        }

        [MenuItem("My Tools/Find Missing Scripts Recursively")]
        public static void ShowWindow() {
            GetWindow(typeof(FindMissingScriptsRecursively));
        }

        static void FindInSelected() {
            GameObject[] go = Selection.gameObjects;
            _goCount = 0;
            List<GameObject> recursiveGameObjects = new List<GameObject>();
            foreach (GameObject g in go) {
                recursiveGameObjects.AddRange(g.GetComponentsInChildrenRecursively<Transform>()
                    .Select(t => t.gameObject));
            }
            
            GameObject[] gameObjectsWithMissingScripts = recursiveGameObjects.Where(MissingScriptsOnGameObject).ToArray();
            Selection.objects = gameObjectsWithMissingScripts;

            _goCount = Selection.objects.Length;
            prevSelection = gameObjectsWithMissingScripts;

            _bHaveRun = true;

            AssetDatabase.SaveAssets();
        }

        static void ReselectPrevSelection() {
            prevSelection = prevSelection.Where(MissingScriptsOnGameObject).ToArray();
            Selection.objects = prevSelection;
            _goCount = Selection.objects.Length;
        }

        static bool MissingScriptsOnGameObject(GameObject g) {
            var allComponents = g.GetComponents(typeof(Component));
            return allComponents.ToList().Exists(c => c == null);
        }
    }
}