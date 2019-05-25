using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ForceActivePillarToggle : MagicTrigger {
	public bool forwardSameScenePillar = true;
	public bool backwardSameScenePillar = true;
	// Setting up references for pillars in same scene can be done directly
	public DimensionPillar forwardTriggeredPillar;
	public DimensionPillar backwardTriggeredPillar;
	// Setting up references for pillars in different scenes has to be done through scene + gameObject names
	public Level forwardPillarLevel;
	public string forwardPillarName;
	public Level backwardPillarLevel;
	public string backwardPillarName;

	// Use this for initialization
	protected virtual void Start () {
		OnMagicTriggerStayOneTime += TriggerPillarChangeForward;
		OnNegativeMagicTriggerStayOneTime += TriggerPillarChangeBackward;
	}
	
	private void TriggerPillarChangeForward(Collider unused) {
		if (forwardTriggeredPillar == null && !forwardSameScenePillar) {
			string pillarKey = PillarKey(forwardPillarLevel, forwardPillarName);
			if (DimensionPillar.pillars.ContainsKey(pillarKey)) {
				forwardTriggeredPillar = DimensionPillar.pillars[pillarKey];
			}
		}

		DimensionPillar.activePillar = forwardTriggeredPillar;
	}

	private void TriggerPillarChangeBackward(Collider unused) {
		if (backwardTriggeredPillar == null && !backwardSameScenePillar) {
			string pillarKey = PillarKey(backwardPillarLevel, backwardPillarName);
			if (DimensionPillar.pillars.ContainsKey(pillarKey)) {
				backwardTriggeredPillar = DimensionPillar.pillars[pillarKey];
			}
		}

		DimensionPillar.activePillar = backwardTriggeredPillar;
	}

	private string PillarKey(Level level, string name) {
		return LevelManager.instance.GetSceneName(level) + " " + name;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ForceActivePillarToggle))]
[CanEditMultipleObjects]
public class ForceActivePillarToggleEditor : MagicTriggerEditor {
	SerializedProperty forwardTriggeredPillar;
	SerializedProperty backwardTriggeredPillar;

	SerializedProperty forwardSameScenePillar;
	SerializedProperty backwardSameScenePillar;

	public SerializedProperty forwardPillarLevel;
	public SerializedProperty forwardPillarName;
	public SerializedProperty backwardPillarLevel;
	public SerializedProperty backwardPillarName;

	protected override void OnEnable() {
		base.OnEnable();
		forwardTriggeredPillar = serializedObject.FindProperty("forwardTriggeredPillar");
		backwardTriggeredPillar = serializedObject.FindProperty("backwardTriggeredPillar");

		forwardSameScenePillar = serializedObject.FindProperty("forwardSameScenePillar");
		backwardSameScenePillar = serializedObject.FindProperty("backwardSameScenePillar");

		forwardPillarLevel = serializedObject.FindProperty("forwardPillarLevel");
		forwardPillarName = serializedObject.FindProperty("forwardPillarName");
		backwardPillarLevel = serializedObject.FindProperty("backwardPillarLevel");
		backwardPillarName = serializedObject.FindProperty("backwardPillarName");
	}

	public override void MoreOnInspectorGUI() {
		base.MoreOnInspectorGUI();

		EditorGUILayout.Space();

		forwardSameScenePillar.boolValue = EditorGUILayout.Toggle("Forward Pillar is in same scene?", forwardSameScenePillar.boolValue);
		if (forwardSameScenePillar.boolValue) {
			forwardTriggeredPillar.objectReferenceValue = EditorGUILayout.ObjectField("Forward Pillar: ", forwardTriggeredPillar.objectReferenceValue, typeof(DimensionPillar), true) as DimensionPillar;
		}
		else {
			forwardPillarLevel.enumValueIndex = (int)(Level)EditorGUILayout.EnumPopup("Level of Pillar: ", (Level)forwardPillarLevel.enumValueIndex);
			forwardPillarName.stringValue = EditorGUILayout.TextField("Name of Pillar: ", forwardPillarName.stringValue);
		}

		EditorGUILayout.Space();

		backwardSameScenePillar.boolValue = EditorGUILayout.Toggle("Backward Pillar is in same scene?", backwardSameScenePillar.boolValue);
		if (backwardSameScenePillar.boolValue) {
			backwardTriggeredPillar.objectReferenceValue = EditorGUILayout.ObjectField("Backward Pillar: ", backwardTriggeredPillar.objectReferenceValue, typeof(DimensionPillar), true) as DimensionPillar;
		}
		else {
			backwardPillarLevel.enumValueIndex = (int)(Level)EditorGUILayout.EnumPopup("Level of Pillar: ", (Level)backwardPillarLevel.enumValueIndex);
			backwardPillarName.stringValue = EditorGUILayout.TextField("Name of Pillar: ", backwardPillarName.stringValue);
		}
	}
}

#endif