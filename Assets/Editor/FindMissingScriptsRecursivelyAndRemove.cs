using UnityEditor;
using UnityEngine;

namespace FLGCoreEditor.Utilities {
    public class FindMissingScriptsRecursivelyAndRemove : EditorWindow {
        static int _goCount;
        static int _componentsCount;
        static int _missingCount;

        static bool _bHaveRun;

        public void OnGUI() {
            if (GUILayout.Button("Find Missing Scripts in selected GameObjects")) FindInSelected();

            if (!_bHaveRun) return;

            EditorGUILayout.TextField($"{_goCount} GameObjects Selected");
            if (_goCount > 0) EditorGUILayout.TextField($"{_componentsCount} Components");
            if (_goCount > 0) EditorGUILayout.TextField($"{_missingCount} Deleted");
        }

        [MenuItem("My Tools/Find Missing Scripts Recursively And Remove")]
        public static void ShowWindow() {
            GetWindow(typeof(FindMissingScriptsRecursivelyAndRemove));
        }

        static void FindInSelected() {
            GameObject[] go = Selection.gameObjects;
            _goCount = 0;
            _componentsCount = 0;
            _missingCount = 0;
            foreach (GameObject g in go) {
                FindInGo(g);
            }

            _bHaveRun = true;
            Debug.Log($"Searched {_goCount} GameObjects, {_componentsCount} components, found {_missingCount} missing");

            AssetDatabase.SaveAssets();
        }

        static void FindInGo(GameObject g) {
            _goCount++;

            _missingCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
        }
    }
}