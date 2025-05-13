#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using DimensionObjectMechanics;

namespace OneTimeScripts {
    public static class DimensionObjectCollisionMatrixMigrationTool {
        [MenuItem("One Time Scripts/Migrate Collision Matrix to CollisionMatrixNew")]
        static void MigrateCollisionMatrix() {
            int migratedScene = 0;
            int migratedPrefabs = 0;

            // Migrate all scene objects
            foreach (var obj in Object.FindObjectsOfType<DimensionObject>()) {
                if (MigrateOnObject(obj)) migratedScene++;
            }

            // Migrate all prefabs in project
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                foreach (var obj in prefab.GetComponentsInChildren<DimensionObject>(true)) {
                    if (MigrateOnObject(obj)) {
                        EditorUtility.SetDirty(prefab);
                        migratedPrefabs++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Migration] Migrated {migratedScene} DimensionObject(s) in scene(s), {migratedPrefabs} in prefab(s).");
        }

        static bool MigrateOnObject(DimensionObject obj) {
            SerializedObject so = new SerializedObject(obj);

            var oldMatrixProp = so.FindProperty("collisionMatrix");
            var newMatrixProp = so.FindProperty("collisionMatrixNew");

            if (oldMatrixProp == null || newMatrixProp == null) {
                Debug.LogWarning($"Skipped {obj.name} because fields are missing.");
                return false;
            }

            DimensionObjectCollisionMatrix newMatrixInstance = obj.collisionMatrixNew;
            if (newMatrixInstance == null) {
                Debug.LogWarning($"Skipped {obj.name} because collisionMatrixNew is null.");
                return false;
            }

            // Copy the data
            bool[] oldMatrixArray = new bool[oldMatrixProp.arraySize];
            for (int i = 0; i < oldMatrixProp.arraySize; i++) {
                oldMatrixArray[i] = oldMatrixProp.GetArrayElementAtIndex(i).boolValue;
            }

            for (int i = 0; i < Mathf.Min(newMatrixInstance.collisionMatrix.Length, oldMatrixArray.Length); i++) {
                newMatrixInstance.collisionMatrix[i] = oldMatrixArray[i];
            }

            EditorUtility.SetDirty(obj);
            return true;
        }
    }
#endif
}