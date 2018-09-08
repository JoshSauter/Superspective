using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelChangeTrigger : MagicTrigger {
	[SerializeField]
	public Level levelForward;
	[SerializeField]
	public Level levelBackward;

	// Use this for initialization
	void Start () {
		OnMagicTriggerStayOneTime += TriggerLevelForward;
		OnNegativeMagicTriggerStayOneTime += TriggerLevelBackward;
	}

	void TriggerLevelForward(Collider c) {
		LevelManager.instance.SwitchActiveScene(levelForward);
	}

	void TriggerLevelBackward(Collider c) {
		LevelManager.instance.SwitchActiveScene(levelBackward);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelChangeTrigger))]
[CanEditMultipleObjects]
public class LevelChangeTriggerEditor : MagicTriggerEditor {
	SerializedProperty levelForward;
	SerializedProperty levelBackward;

	protected override void OnEnable() {
		base.OnEnable();
		levelForward = serializedObject.FindProperty("levelForward");
		levelBackward = serializedObject.FindProperty("levelBackward");
	}

	public override void MoreOnInspectorGUI() {
		base.MoreOnInspectorGUI();

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(levelForward);
		Level currentLevelForward = (Level)Enum.GetValues(typeof(Level)).GetValue(levelForward.enumValueIndex);
		if (EditorGUI.EndChangeCheck()) {
			foreach (System.Object obj in targets) {
				var trigger = ((LevelChangeTrigger)obj);
				trigger.levelForward = currentLevelForward;
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
			}
		}
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(levelBackward);
		Level currentLevelBackward = (Level)Enum.GetValues(typeof(Level)).GetValue(levelBackward.enumValueIndex);
		if (EditorGUI.EndChangeCheck()) {
			foreach (System.Object obj in targets) {
				var trigger = ((LevelChangeTrigger)obj);
				trigger.levelBackward = currentLevelBackward;
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
			}
		}

		EditorGUILayout.Space();
	}
}

#endif