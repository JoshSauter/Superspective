using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PillarDimensionObject))]
[CanEditMultipleObjects]
public class PillarDimensionObjectInspector : DimensionObjectInspector {
    SerializedProperty channel;

    SerializedProperty colliderBoundsOverride;

    // DimensionObject properties (base class)
    SerializedProperty DEBUG;

    // PillarDimensionObject properties
    SerializedProperty dimension;
    SerializedProperty dimensionShiftQuadrant;
    SerializedProperty ignoreMaterialChanges;
    SerializedProperty maxAngle;
    SerializedProperty minAngle;

    SerializedProperty pillars;
    SerializedProperty playerQuadrant;
    SerializedProperty reverseVisibilityStates;
    SerializedProperty thisObjectMoves;
    SerializedProperty thisRigidbody;
    SerializedProperty treatChildrenAsOneObjectRecursively;
    SerializedProperty ignoreChildrenWithDimensionObject;
    SerializedProperty visibilityState;

    protected virtual void OnEnable() {
        base.OnEnable();
        DEBUG = serializedObject.FindProperty("DEBUG");
        treatChildrenAsOneObjectRecursively = serializedObject.FindProperty("treatChildrenAsOneObjectRecursively");
        ignoreChildrenWithDimensionObject = serializedObject.FindProperty("ignoreChildrenWithDimensionObject");
        visibilityState = serializedObject.FindProperty("visibilityState");
        channel = serializedObject.FindProperty("channel");
        reverseVisibilityStates = serializedObject.FindProperty("reverseVisibilityStates");
        ignoreMaterialChanges = serializedObject.FindProperty("ignoreMaterialChanges");

        dimension = serializedObject.FindProperty("_dimension");
        playerQuadrant = serializedObject.FindProperty("playerQuadrant");
        dimensionShiftQuadrant = serializedObject.FindProperty("dimensionShiftQuadrant");
        minAngle = serializedObject.FindProperty("minAngle");
        maxAngle = serializedObject.FindProperty("maxAngle");

        colliderBoundsOverride = serializedObject.FindProperty("colliderBoundsOverride");

        pillars = serializedObject.FindProperty("pillars");
        thisObjectMoves = serializedObject.FindProperty("thisObjectMoves");
        thisRigidbody = serializedObject.FindProperty("thisRigidbody");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        serializedObject.Update();

        DEBUG.boolValue = EditorGUILayout.Toggle("Debug?", DEBUG.boolValue);

        AddSeparator();

        GUILayout.Label("State:", EditorStyles.miniBoldLabel);

        EditorGUILayout.LabelField("Visibility State:", visibilityState.enumDisplayNames[visibilityState.intValue]);
        EditorGUILayout.LabelField(
            "Appears as Visibility State:",
            reverseVisibilityStates.boolValue
                ? visibilityState.enumDisplayNames[(visibilityState.intValue + 2) % 4]
                : visibilityState.enumDisplayNames[visibilityState.intValue]
        );
        EditorGUILayout.LabelField("Player Quadrant:", playerQuadrant.enumDisplayNames[playerQuadrant.intValue]);
        EditorGUILayout.LabelField(
            "Dimension Shift Quadrant:",
            dimensionShiftQuadrant.enumDisplayNames[dimensionShiftQuadrant.intValue]
        );
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField("Min angle for active pillar: ", minAngle.floatValue);
        EditorGUILayout.FloatField("Max angle for active pillar: ", maxAngle.floatValue);
        EditorGUI.EndDisabledGroup();

        AddSeparator();

        GUILayout.Label("Setup:", EditorStyles.miniBoldLabel);

        GUIContent pillarsLabel = new GUIContent("Pillars: ");
        EditorGUILayout.PropertyField(pillars, pillarsLabel);

        GUIContent channelLabel = new GUIContent("Channel: ");
        EditorGUILayout.PropertyField(channel, channelLabel);

        GUIContent dimensionLabel = new GUIContent("Dimension: ");
        EditorGUILayout.PropertyField(dimension, dimensionLabel);

        GUIContent treatChildrenAsOneObjectLabel = new GUIContent("Treat children as one object?");
        EditorGUILayout.PropertyField(treatChildrenAsOneObjectRecursively, treatChildrenAsOneObjectLabel);

        if (treatChildrenAsOneObjectRecursively.boolValue) {
            GUIContent ignoreChildrenWithDimensionObjectLabel = new GUIContent("Ignore children already with DimensionObject?");
            EditorGUILayout.PropertyField(ignoreChildrenWithDimensionObject, ignoreChildrenWithDimensionObjectLabel);
        }

        GUIContent reverseVisibilityStatesLabel = new GUIContent("Invert visibility states?");
        EditorGUILayout.PropertyField(reverseVisibilityStates, reverseVisibilityStatesLabel);

        GUIContent ignoreMaterialChangesLabel = new GUIContent("Ignore material changes?");
        EditorGUILayout.PropertyField(ignoreMaterialChanges, ignoreMaterialChangesLabel);

        GUIContent thisObjectMovesLabel = new GUIContent("This object moves: ");
        EditorGUILayout.PropertyField(thisObjectMoves, thisObjectMovesLabel);

        if (thisObjectMoves.boolValue) {
            GUIContent thisRigidbodyLabel = new GUIContent("Rigidbody: ");
            EditorGUILayout.PropertyField(thisRigidbody, thisRigidbodyLabel);
        }

        GUIContent useColliderOverrideLabel = new GUIContent("Use collider for bounds: ");
        EditorGUILayout.PropertyField(colliderBoundsOverride, useColliderOverrideLabel);

        serializedObject.ApplyModifiedProperties();
    }

    void AddSeparator() {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
}