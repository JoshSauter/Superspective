using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EpitaphUtils;

[CustomEditor(typeof(PillarDimensionObject))]
[CanEditMultipleObjects]
public class DimensionObjectInspector : Editor {
	SerializedProperty DEBUG;

	SerializedProperty visibilityState;

	SerializedProperty baseDimension;

	SerializedProperty reverseVisibilityStates;
	SerializedProperty ignoreMaterialChanges;
	SerializedProperty treatChildrenAsOneObjectRecursively;
	SerializedProperty continuouslyUpdateOnOffAngles;

	SerializedProperty overrideOnOffAngles;

	protected virtual void OnEnable() {
		DEBUG = serializedObject.FindProperty("DEBUG");

		visibilityState = serializedObject.FindProperty("visibilityState");

		baseDimension = serializedObject.FindProperty("baseDimension");

		reverseVisibilityStates = serializedObject.FindProperty("reverseVisibilityStates");
		ignoreMaterialChanges = serializedObject.FindProperty("ignoreMaterialChanges");
		treatChildrenAsOneObjectRecursively = serializedObject.FindProperty("treatChildrenAsOneObjectRecursively");
		continuouslyUpdateOnOffAngles = serializedObject.FindProperty("continuouslyUpdateOnOffAngles");

		overrideOnOffAngles = serializedObject.FindProperty("overrideOnOffAngles");
	}

	public override void OnInspectorGUI() {
		PillarDimensionObject script = target as PillarDimensionObject;
		float defaultWidth = EditorGUIUtility.labelWidth;
		serializedObject.Update();

		DEBUG.boolValue = EditorGUILayout.Toggle("Debug?", DEBUG.boolValue);

		AddSeparator();

		EditorGUI.BeginChangeCheck();
		script.startingVisibilityState = (VisibilityState)EditorGUILayout.EnumPopup("Starting visibility state: ", script.startingVisibilityState);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PillarDimensionObject)obj).startingVisibilityState = script.startingVisibilityState;
				if (!Application.isPlaying) {
					((PillarDimensionObject)obj).visibilityState = script.startingVisibilityState;
				}
			}
		}
		EditorGUILayout.LabelField("Visibility State:", visibilityState.enumDisplayNames[visibilityState.intValue]);

		AddSeparator();

		GUILayout.Label("Config:", EditorStyles.miniBoldLabel);

		EditorGUI.BeginChangeCheck();
		script.findPillarsTechnique = (PillarDimensionObject.FindPillarsTechnique)EditorGUILayout.EnumPopup("Find pillars by: ", script.findPillarsTechnique);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PillarDimensionObject)obj).findPillarsTechnique = script.findPillarsTechnique;
			}
		}

		EditorGUILayout.Space();
		switch (script.findPillarsTechnique) {
			case PillarDimensionObject.FindPillarsTechnique.whitelist:
				SerializedProperty whitelist = serializedObject.FindProperty("whitelist");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(whitelist, new GUIContent("Whitelist: "), true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				break;
			case PillarDimensionObject.FindPillarsTechnique.automaticSphere:
				UpdateSearchRadiusForAll(script);
				break;
			case PillarDimensionObject.FindPillarsTechnique.automaticSphereWithBlacklist: {
					UpdateSearchRadiusForAll(script);
					SerializedProperty blacklist = serializedObject.FindProperty("blacklist");
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(blacklist, new GUIContent("Blacklist: "), true);
					if (EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}
				break;
			case PillarDimensionObject.FindPillarsTechnique.automaticBox:
				UpdateSearchBoxSizeForAll(script);
				break;
			case PillarDimensionObject.FindPillarsTechnique.automaticBoxWithBlacklist: {
					UpdateSearchBoxSizeForAll(script);
					SerializedProperty blacklist = serializedObject.FindProperty("blacklist");
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(blacklist, new GUIContent("Blacklist: "), true);
					if (EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}
				break;
		}

		EditorGUILayout.Space();

		baseDimension.intValue = EditorGUILayout.IntSlider("Dimension:", baseDimension.intValue, 0, 123);
		EditorGUILayout.Space();
		EditorGUIUtility.labelWidth = 200;
		reverseVisibilityStates.boolValue = EditorGUILayout.Toggle("Reverse visibility states?", reverseVisibilityStates.boolValue);
		ignoreMaterialChanges.boolValue = EditorGUILayout.Toggle("Ignore material changes?", ignoreMaterialChanges.boolValue);
		treatChildrenAsOneObjectRecursively.boolValue = EditorGUILayout.Toggle("Treat children as one object?", treatChildrenAsOneObjectRecursively.boolValue);
		continuouslyUpdateOnOffAngles.boolValue = EditorGUILayout.Toggle("Continuously Update OnOff Angles?", continuouslyUpdateOnOffAngles.boolValue);

		EditorGUIUtility.labelWidth = defaultWidth;

		AddSeparator();

		GUILayout.Label("On/Off Angles:", EditorStyles.miniBoldLabel);
		EditorGUIUtility.labelWidth = 150;
		overrideOnOffAngles.boolValue = EditorGUILayout.Toggle("Override On/Off Angles?", overrideOnOffAngles.boolValue);
		EditorGUIUtility.labelWidth = defaultWidth;

		if (overrideOnOffAngles.boolValue) {

			EditorGUI.BeginChangeCheck();
			script.onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
			script.offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
			if (EditorGUI.EndChangeCheck()) {
				foreach (Object obj in targets) {
					((PillarDimensionObject)obj).onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
					((PillarDimensionObject)obj).offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
				}
			}
		}
		else {
			EditorGUILayout.LabelField("On Angle: ", script.onAngle.ToString());
			EditorGUILayout.LabelField("Off Angle: ", script.offAngle.ToString());
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void UpdateSearchRadiusForAll(PillarDimensionObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchRadius = EditorGUILayout.FloatField("Search radius: ", script.pillarSearchRadius);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PillarDimensionObject)obj).pillarSearchRadius = script.pillarSearchRadius;
			}
		}
	}

	private void UpdateSearchBoxSizeForAll(PillarDimensionObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchBoxSize = EditorGUILayout.Vector3Field("Search box size: ", script.pillarSearchBoxSize);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((PillarDimensionObject)obj).pillarSearchBoxSize = script.pillarSearchBoxSize;
			}
		}
	}

	private void AddSeparator() {
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
	}
}
