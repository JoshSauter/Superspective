using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ForceActivePillarToggle : MagicTrigger {
	public ObscurePillar forwardTriggeredPillar;
	public ObscurePillar backwardTriggeredPillar;

	// Use this for initialization
	protected virtual void Start () {
		OnMagicTriggerStayOneTime += TriggerPillarChangeForward;
		OnNegativeMagicTriggerStayOneTime += TriggerPillarChangeBackward;
	}
	
	private void TriggerPillarChangeForward(Collider unused) {
		ObscurePillar.activePillar = forwardTriggeredPillar;
	}

	private void TriggerPillarChangeBackward(Collider unused) {
		ObscurePillar.activePillar = backwardTriggeredPillar;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ForceActivePillarToggle))]
[CanEditMultipleObjects]
public class ForceActivePillarToggleEditor : MagicTriggerEditor {
	SerializedProperty forwardTriggeredPillar;
	SerializedProperty backwardTriggeredPillar;

	protected override void OnEnable() {
		base.OnEnable();
		forwardTriggeredPillar = serializedObject.FindProperty("forwardTriggeredPillar");
		backwardTriggeredPillar = serializedObject.FindProperty("backwardTriggeredPillar");
	}

	public override void MoreOnInspectorGUI() {
		base.MoreOnInspectorGUI();

		EditorGUILayout.Space();

		forwardTriggeredPillar.objectReferenceValue = EditorGUILayout.ObjectField("Forward Pillar: ", forwardTriggeredPillar.objectReferenceValue, typeof(ObscurePillar), true) as ObscurePillar;

		EditorGUILayout.Space();

		backwardTriggeredPillar.objectReferenceValue = EditorGUILayout.ObjectField("Backward Pillar: ", backwardTriggeredPillar.objectReferenceValue, typeof(ObscurePillar), true) as ObscurePillar;
	}
}

#endif