using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelChangeTrigger : MagicTrigger {
	public string levelForward;
	public string levelBackward;

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
		
		script.levelForward = EditorGUILayout.TextField("Forward level: ", script.levelForward);
		script.levelBackward = EditorGUILayout.TextField("Backward level: ", script.levelBackward);

		EditorGUILayout.Space();
	}
}

#endif