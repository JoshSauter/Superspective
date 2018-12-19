using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BladeEdgeDetection))]
[CanEditMultipleObjects]
public class BladeEdgeDetectionInspector : Editor {
	SerializedProperty debugMode;

	SerializedProperty doubleSidedEdges;
	SerializedProperty depthSensitivity;
	SerializedProperty normalSensitivity;
	SerializedProperty sampleDistance;

	SerializedProperty edgeColorMode;
	SerializedProperty edgeColor;
	SerializedProperty edgeColorGradient;
	SerializedProperty edgeColorGradientTexture;

	void OnEnable() {
		debugMode = serializedObject.FindProperty("debugMode");

		doubleSidedEdges = serializedObject.FindProperty("doubleSidedEdges");
		depthSensitivity = serializedObject.FindProperty("depthSensitivity");
		normalSensitivity = serializedObject.FindProperty("normalSensitivity");
		sampleDistance = serializedObject.FindProperty("sampleDistance");

		edgeColorMode = serializedObject.FindProperty("edgeColorMode");
		edgeColor = serializedObject.FindProperty("edgeColor");
		edgeColorGradient = serializedObject.FindProperty("edgeColorGradient");
		edgeColorGradientTexture = serializedObject.FindProperty("edgeColorGradientTexture");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		debugMode.boolValue = EditorGUILayout.Toggle("Debug mode?", debugMode.boolValue);
		if (debugMode.boolValue) {
			GUILayout.Label("Debug colors:\nRed:\tDepth-detected edge\nGreen:\tNormal-detected edge\nYellow:\tBoth", EditorStyles.miniBoldLabel);
		}

		AddSeparator();

		GUILayout.Label("Sensitivity:", EditorStyles.miniBoldLabel);
		depthSensitivity.floatValue = EditorGUILayout.FloatField("Depth sensitivity: ", depthSensitivity.floatValue);
		normalSensitivity.floatValue = EditorGUILayout.FloatField("Normal sensitivity: ", normalSensitivity.floatValue);
		EditorGUILayout.Space();

		GUILayout.Label("Thickness:", EditorStyles.miniBoldLabel);
		doubleSidedEdges.boolValue = EditorGUILayout.Toggle("Double-sided edges?", doubleSidedEdges.boolValue);
		sampleDistance.intValue = EditorGUILayout.IntField("Sample distance: ", sampleDistance.intValue);

		AddSeparator();

		GUILayout.Label("Colors:", EditorStyles.miniBoldLabel);
		edgeColorMode.enumValueIndex = (int)(BladeEdgeDetection.EdgeColorMode)EditorGUILayout.EnumPopup("Edge Color Mode: ", (BladeEdgeDetection.EdgeColorMode)edgeColorMode.enumValueIndex);
		switch ((BladeEdgeDetection.EdgeColorMode)edgeColorMode.enumValueIndex) {
			case BladeEdgeDetection.EdgeColorMode.simpleColor:
				edgeColor.colorValue = EditorGUILayout.ColorField("Edge color: ", edgeColor.colorValue);
				break;
			case BladeEdgeDetection.EdgeColorMode.gradient:
				GUIContent gradientLabel = new GUIContent("Edge gradient: ");
				EditorGUILayout.PropertyField(edgeColorGradient, gradientLabel);
				break;
			case BladeEdgeDetection.EdgeColorMode.colorRampTexture:
				GUIContent colorRampLabel = new GUIContent("Edge gradient texture: ");
				EditorGUILayout.PropertyField(edgeColorGradientTexture, colorRampLabel);
				break;
		}

		EditorGUILayout.Space();
		serializedObject.ApplyModifiedProperties();
	}

	private void AddSeparator() {
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
	}
}