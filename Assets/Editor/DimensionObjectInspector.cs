using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EpitaphUtils;

[CustomEditor(typeof(DimensionObject))]
[CanEditMultipleObjects]
public class DimensionObjectInspector : Editor {
	SerializedProperty DEBUG;

	SerializedProperty startingVisibilityState;
	SerializedProperty visibilityState;

	SerializedProperty baseDimension;

	SerializedProperty reverseVisibilityStates;
	SerializedProperty treatChildrenAsOneObjectRecursively;

	SerializedProperty overrideOnOffAngles;

	protected virtual void OnEnable() {
		DEBUG = serializedObject.FindProperty("DEBUG");

		startingVisibilityState = serializedObject.FindProperty("startingVisibilityState");
		visibilityState = serializedObject.FindProperty("visibilityState");

		baseDimension = serializedObject.FindProperty("baseDimension");

		treatChildrenAsOneObjectRecursively = serializedObject.FindProperty("treatChildrenAsOneObjectRecursively");
		reverseVisibilityStates = serializedObject.FindProperty("reverseVisibilityStates");

		overrideOnOffAngles = serializedObject.FindProperty("overrideOnOffAngles");
	}

	public override void OnInspectorGUI() {
		DimensionObject script = target as DimensionObject;
		float defaultWidth = EditorGUIUtility.labelWidth;
		serializedObject.Update();

		DEBUG.boolValue = EditorGUILayout.Toggle("Debug?", DEBUG.boolValue);

		AddSeparator();

		EditorGUI.BeginChangeCheck();
		script.startingVisibilityState = (VisibilityState)EditorGUILayout.EnumPopup("Starting visibility state: ", script.startingVisibilityState);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((DimensionObject)obj).startingVisibilityState = script.startingVisibilityState;
				if (!Application.isPlaying) {
					((DimensionObject)obj).visibilityState = script.startingVisibilityState;
				}
			}
		}
		EditorGUILayout.LabelField("Visibility State:", visibilityState.enumDisplayNames[visibilityState.intValue]);

		AddSeparator();

		GUILayout.Label("Config:", EditorStyles.miniBoldLabel);

		EditorGUI.BeginChangeCheck();
		script.findPillarsTechnique = (DimensionObject.FindPillarsTechnique)EditorGUILayout.EnumPopup("Find pillars by: ", script.findPillarsTechnique);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((DimensionObject)obj).findPillarsTechnique = script.findPillarsTechnique;
			}
		}

		EditorGUILayout.Space();
		switch (script.findPillarsTechnique) {
			case DimensionObject.FindPillarsTechnique.whitelist:
				SerializedProperty whitelist = serializedObject.FindProperty("whitelist");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(whitelist, new GUIContent("Whitelist: "), true);
				if (EditorGUI.EndChangeCheck())
					serializedObject.ApplyModifiedProperties();
				break;
			case DimensionObject.FindPillarsTechnique.automaticSphere:
				UpdateSearchRadiusForAll(script);
				break;
			case DimensionObject.FindPillarsTechnique.automaticSphereWithBlacklist: {
					UpdateSearchRadiusForAll(script);
					SerializedProperty blacklist = serializedObject.FindProperty("blacklist");
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(blacklist, new GUIContent("Blacklist: "), true);
					if (EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}
				break;
			case DimensionObject.FindPillarsTechnique.automaticBox:
				UpdateSearchBoxSizeForAll(script);
				break;
			case DimensionObject.FindPillarsTechnique.automaticBoxWithBlacklist: {
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
		treatChildrenAsOneObjectRecursively.boolValue = EditorGUILayout.Toggle("Treat children as one object?", treatChildrenAsOneObjectRecursively.boolValue);
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
					((DimensionObject)obj).onAngle = Angle.Degrees(EditorGUILayout.FloatField("On Angle Degrees: ", script.onAngle.degrees));
					((DimensionObject)obj).offAngle = Angle.Degrees(EditorGUILayout.FloatField("Off Angle Degrees: ", script.offAngle.degrees));
				}
			}
		}
		else {
			EditorGUILayout.LabelField("On Angle: ", script.onAngle.ToString());
			EditorGUILayout.LabelField("Off Angle: ", script.offAngle.ToString());
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void UpdateSearchRadiusForAll(DimensionObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchRadius = EditorGUILayout.FloatField("Search radius: ", script.pillarSearchRadius);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((DimensionObject)obj).pillarSearchRadius = script.pillarSearchRadius;
			}
		}
	}

	private void UpdateSearchBoxSizeForAll(DimensionObject script) {
		EditorGUI.BeginChangeCheck();
		script.pillarSearchBoxSize = EditorGUILayout.Vector3Field("Search box size: ", script.pillarSearchBoxSize);
		if (EditorGUI.EndChangeCheck()) {
			foreach (Object obj in targets) {
				((DimensionObject)obj).pillarSearchBoxSize = script.pillarSearchBoxSize;
			}
		}
	}

	private void AddSeparator() {
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
	}
}
