using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Saving;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

public class SelectOrDisableAllDebug : EditorWindow {
    private bool turnOffDebug = false;

    [MenuItem("My Tools/Selection/Select or Disable All DEBUG")]
    private static void ShowWindow() {
        var window = GetWindow<SelectOrDisableAllDebug>();
        window.titleContent = new GUIContent("Debug Manager");
        window.Show();
    }

    private void OnGUI() {
        GUILayout.Label("SaveableObject Debug Manager", EditorStyles.boldLabel);

        turnOffDebug = EditorGUILayout.Toggle("Turn off all DEBUG?", turnOffDebug);

        if (GUILayout.Button("Execute")) {
            Execute();
        }
    }

    private void Execute() {
        GameObject[] selectedGameObjects = Selection.gameObjects;
        List<SuperspectiveObject> saveableObjects = new List<SuperspectiveObject>();

        if (selectedGameObjects.Length > 0) {
            foreach (GameObject go in selectedGameObjects) {
                GetSaveableObjectsRecursive(go.transform, saveableObjects);
            }
        }
        else {
            foreach (GameObject go in GetAllRootGameObjectsInLoadedScenes()) {
                GetSaveableObjectsRecursive(go.transform, saveableObjects);
            }
        }
        
        StringBuilder debugText = new StringBuilder();
        debugText.Append($"{saveableObjects.Count} SaveableObjects found");

        string listSaveableObjects;
        
        string SaveableObjectPath(SuperspectiveObject so) {
            string className = so.GetType().Name;
            return $"{so.FullPath()}.{className}";
        }

        if (turnOffDebug) {
            List<SuperspectiveObject> disabled = DisableDebug(saveableObjects);
            debugText.Append($", {disabled.Count} where DEBUG got turned off");
            listSaveableObjects = string.Join("\n", disabled.ConvertAll(SaveableObjectPath).ToArray());
        }
        else {
            List<SuperspectiveObject> enabled = saveableObjects.Where(so => so.DEBUG).ToList();
            debugText.Append($", {enabled.Count} where DEBUG is turned on");
            listSaveableObjects = string.Join("\n", enabled.ConvertAll(SaveableObjectPath).ToArray());
        }
        debugText.AppendLine(":");
        debugText.Append(listSaveableObjects);

        Debug.Log(debugText.ToString());
        
        // Select these objects in the editor
        Selection.objects = saveableObjects.ToArray();
    }

    private void GetSaveableObjectsRecursive(Transform parent, List<SuperspectiveObject> saveableObjects) {
        SuperspectiveObject[] thisObjSaveableObjects = parent.GetComponents<SuperspectiveObject>();
        if (thisObjSaveableObjects != null && thisObjSaveableObjects.Length > 0) {
            saveableObjects.AddRange(thisObjSaveableObjects);
        }

        foreach (Transform child in parent) {
            GetSaveableObjectsRecursive(child, saveableObjects);
        }
    }

    private GameObject[] GetAllRootGameObjectsInLoadedScenes() {
        List<GameObject> rootObjects = new List<GameObject>();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded) {
                rootObjects.AddRange(scene.GetRootGameObjects());
            }
        }

        return rootObjects.ToArray();
    }

    private List<SuperspectiveObject> DisableDebug(List<SuperspectiveObject> saveableObjects) {
        Undo.RecordObjects(saveableObjects.ToArray(), "Disable DEBUG on SaveableObjects");
        
        HashSet<SuperspectiveObject> disabledObjects = new HashSet<SuperspectiveObject>();
        foreach (SuperspectiveObject saveableObject in saveableObjects) {
            if (saveableObject.DEBUG) {
                disabledObjects.Add(saveableObject);
            }
            saveableObject.DEBUG = false;
            EditorUtility.SetDirty(saveableObject);
        }

        return disabledObjects.ToList();
    }
}