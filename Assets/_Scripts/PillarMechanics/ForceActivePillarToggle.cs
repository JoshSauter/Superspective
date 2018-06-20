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
	public override void OnInspectorGUI() {
		ForceActivePillarToggle script = target as ForceActivePillarToggle;
		base.OnInspectorGUI();

		EditorGUILayout.Space();

		script.forwardTriggeredPillar = EditorGUILayout.ObjectField("Forward Pillar: ", script.forwardTriggeredPillar, typeof(ObscurePillar), true) as ObscurePillar;

		EditorGUILayout.Space();

		script.backwardTriggeredPillar = EditorGUILayout.ObjectField("Backward Pillar: ", script.backwardTriggeredPillar, typeof(ObscurePillar), true) as ObscurePillar;
	}
}

#endif