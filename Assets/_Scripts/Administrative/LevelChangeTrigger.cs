using System.Collections;
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
	public override void OnInspectorGUI() {
		LevelChangeTrigger script = target as LevelChangeTrigger;
		base.OnInspectorGUI();

		EditorGUI.BeginChangeCheck();
		script.levelForward = (Level)EditorGUILayout.EnumPopup("Forward level: ", script.levelForward);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((LevelChangeTrigger)obj).levelForward = script.levelForward;
			}
		}
		EditorGUI.BeginChangeCheck();
		script.levelBackward = (Level)EditorGUILayout.EnumPopup("Backward level: ", script.levelBackward);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((LevelChangeTrigger)obj).levelBackward = script.levelBackward;
			}
		}

		EditorGUILayout.Space();
	}
}

#endif