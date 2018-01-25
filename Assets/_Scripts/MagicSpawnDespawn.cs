using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MagicSpawnDespawn : MagicTrigger {
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;
	public MonoBehaviour[] scriptsToEnable;
	public MonoBehaviour[] scriptsToDisable;

	// Use this for initialization
	protected virtual void Start () {
        OnMagicTriggerStayOneTime += EnableDisableObjects;
	}

    protected void EnableDisableObjects(Collider o) {
        foreach (var objectToEnable in objectsToEnable) {
            objectToEnable.SetActive(true);
        }
        foreach (var objectToDisable in objectsToDisable) {
            objectToDisable.SetActive(false);
        }
		foreach (var scriptToEnable in scriptsToEnable) {
			scriptToEnable.enabled = true;
		}
		foreach (var scriptToDisable in scriptsToDisable) {
			scriptToDisable.enabled = false;
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

		SerializedProperty scriptsToEnable = serializedObject.FindProperty("scriptsToEnable");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(scriptsToEnable, true);
		if (EditorGUI.EndChangeCheck())
			serializedObject.ApplyModifiedProperties();

		SerializedProperty scriptsToDisable = serializedObject.FindProperty("scriptsToDisable");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(scriptsToDisable, true);
		if (EditorGUI.EndChangeCheck())
			serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
    }
}

#endif