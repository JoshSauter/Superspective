using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MagicSpawnDespawn : MagicTrigger {
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

	// Use this for initialization
	void Start () {
        OnMagicTriggerStay += EnableDisableObjects;
	}

    private void EnableDisableObjects(Collider o) {
        foreach (var objectToEnable in objectsToEnable) {
            objectToEnable.SetActive(true);
        }
        foreach (var objectToDisable in objectsToDisable) {
            objectToDisable.SetActive(false);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MagicSpawnDespawn))]
[CanEditMultipleObjects]
public class MagicSpawnDespawnEditor : MagicTriggerEditor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        SerializedProperty objectsToEnable = serializedObject.FindProperty("objectsToEnable");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(objectsToEnable, true);
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();

        SerializedProperty objectsToDisable = serializedObject.FindProperty("objectsToDisable");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(objectsToDisable, true);
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
    }
}

#endif