using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BladeEdgeDetection))]
[CanEditMultipleObjects]
public class BladeEdgeDetectionInspector : UnityEditor.Editor {
	SerializedProperty debugMode;

	bool thicknessHelp = false;
	SerializedProperty checkPortalDepth;
	SerializedProperty depthSensitivity;
	SerializedProperty normalSensitivity;
	SerializedProperty sampleDistance;

	bool weightedEdgesHelp = false;
	SerializedProperty weightedEdgeMode;
	SerializedProperty depthWeightEffect;
	SerializedProperty normalWeightEffect;
	SerializedProperty depthWeightMin;
	SerializedProperty normalWeightMin;

	bool edgeColorsHelp = false;
	SerializedProperty edgeColorMode;
	SerializedProperty edgeColor;
	SerializedProperty edgeColorGradient;
	SerializedProperty edgeColorGradientTexture;

	void OnEnable() {
		debugMode = serializedObject.FindProperty("debugMode");

		checkPortalDepth = serializedObject.FindProperty("checkPortalDepth");
		depthSensitivity = serializedObject.FindProperty("depthSensitivity");
		normalSensitivity = serializedObject.FindProperty("normalSensitivity");
		sampleDistance = serializedObject.FindProperty("sampleDistance");

		weightedEdgeMode = serializedObject.FindProperty("weightedEdgeMode");
		depthWeightEffect = serializedObject.FindProperty("depthWeightEffect");
		normalWeightEffect = serializedObject.FindProperty("normalWeightEffect");
		depthWeightMin = serializedObject.FindProperty("depthWeightMin");
		normalWeightMin = serializedObject.FindProperty("normalWeightMin");

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

		LabelWithHelpButton(ref thicknessHelp, "Thickness:");
		checkPortalDepth.boolValue = EditorGUILayout.Toggle("Avoid drawing on top of portals?", checkPortalDepth.boolValue);
		sampleDistance.intValue = EditorGUILayout.IntField("Sample distance: ", sampleDistance.intValue);
		string pixelPlural = (sampleDistance.intValue == 1) ? " pixel " : " pixels ";
		OptionalHelpBox(thicknessHelp, "Each depth and normal value will be compared against the depth and normal values " + sampleDistance.intValue + pixelPlural + "away");
		EditorGUILayout.Space();

		LabelWithHelpButton(ref weightedEdgesHelp, "Weighted edges:");
		weightedEdgeMode.enumValueIndex = (int)(BladeEdgeDetection.WeightedEdgeMode)EditorGUILayout.EnumPopup("Weighted edge mode: ", (BladeEdgeDetection.WeightedEdgeMode)weightedEdgeMode.enumValueIndex);
		EditorGUILayout.Space();
		switch ((BladeEdgeDetection.WeightedEdgeMode)weightedEdgeMode.enumValueIndex) {
			case BladeEdgeDetection.WeightedEdgeMode.Unweighted:
				OptionalHelpBox(weightedEdgesHelp, "All edges will have equal strength regardless of depth or normal differences");
				break;
			case BladeEdgeDetection.WeightedEdgeMode.WeightedByDepth:
				OptionalHelpBox(weightedEdgesHelp, "Depth-detected edges will vary in strength depending on magnitude of depth difference");
				depthWeightEffect.floatValue = EditorGUILayout.FloatField("Depth weight effect: ", depthWeightEffect.floatValue);
				
				OptionalHelpBox(weightedEdgesHelp, "Depth min alpha values sets the baseline alpha that weighted edges mode can lower the original color to.");
				depthWeightMin.floatValue = EditorGUILayout.FloatField("Depth weight min alpha: ", depthWeightMin.floatValue);
				break;
			case BladeEdgeDetection.WeightedEdgeMode.WeightedByNormals:
				OptionalHelpBox(weightedEdgesHelp, "Normal-detected edges will vary in strength depending on magnitude of normal difference");
				normalWeightEffect.floatValue = EditorGUILayout.FloatField("Normal weight effect: ", normalWeightEffect.floatValue);
				
				OptionalHelpBox(weightedEdgesHelp, "Normal min alpha values sets the baseline alpha that weighted edges mode can lower the original color to.");
				normalWeightMin.floatValue = EditorGUILayout.FloatField("Normal weight min alpha: ", normalWeightMin.floatValue);
				break;
			case BladeEdgeDetection.WeightedEdgeMode.WeightedByDepthAndNormals:
				OptionalHelpBox(weightedEdgesHelp, "All edges will vary in strength depending on magnitude of depth & normal differences");
				depthWeightEffect.floatValue = EditorGUILayout.FloatField("Depth weight effect: ", depthWeightEffect.floatValue);
				normalWeightEffect.floatValue = EditorGUILayout.FloatField("Normal weight effect: ", normalWeightEffect.floatValue);
				
				EditorGUILayout.Space();
				
				OptionalHelpBox(weightedEdgesHelp, "Depth/Normal min alpha values sets the baseline alpha that weighted edges mode can lower the original color to.");
				depthWeightMin.floatValue = EditorGUILayout.FloatField("Depth weight min alpha: ", depthWeightMin.floatValue);
				normalWeightMin.floatValue = EditorGUILayout.FloatField("Normal weight min alpha: ", normalWeightMin.floatValue);
				break;
		}

		AddSeparator();

		LabelWithHelpButton(ref edgeColorsHelp, "Colors:");
		edgeColorMode.enumValueIndex = (int)(BladeEdgeDetection.EdgeColorMode)EditorGUILayout.EnumPopup("Edge Color Mode: ", (BladeEdgeDetection.EdgeColorMode)edgeColorMode.enumValueIndex);
		switch ((BladeEdgeDetection.EdgeColorMode)edgeColorMode.enumValueIndex) {
			case BladeEdgeDetection.EdgeColorMode.SimpleColor:
				edgeColor.colorValue = EditorGUILayout.ColorField("Edge color: ", edgeColor.colorValue);
				OptionalHelpBox(edgeColorsHelp, "All edges will have the same color");
				break;
			case BladeEdgeDetection.EdgeColorMode.Gradient:
				GUIContent gradientLabel = new GUIContent("Edge gradient: ");
				EditorGUILayout.PropertyField(edgeColorGradient, gradientLabel);
				OptionalHelpBox(edgeColorsHelp, "Edges colors will be sampled from the gradient based on distance from the camera");
				break;
			case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
				GUIContent colorRampLabel = new GUIContent("Edge gradient texture: ");
				EditorGUILayout.PropertyField(edgeColorGradientTexture, colorRampLabel);
				OptionalHelpBox(edgeColorsHelp, "Edge colors will be sampled from the gradient texture (horizontally) based on the distance from the camera");
				break;
		}

		EditorGUILayout.Space();
		serializedObject.ApplyModifiedProperties();
	}

	void AddSeparator() {
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
	}

	void OptionalHelpBox(bool enabled, string text) {
		if (enabled) {
			EditorGUILayout.HelpBox(text, MessageType.Info);
		}
	}

	void LabelWithHelpButton(ref bool helpButtonPressed, string label) {
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, EditorStyles.miniBoldLabel);
		helpButtonPressed = GUILayout.Toggle(helpButtonPressed, "?", "Button", GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();
	}
}