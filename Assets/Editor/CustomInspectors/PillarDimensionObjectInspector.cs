using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PillarDimensionObject))]
[CanEditMultipleObjects]
public class PillarDimensionObjectInspector : DimensionObjectInspector {
    static bool pillarDimensionObjSetupShown = true;
    static bool dimensionStateShown = true;
    
    // PillarDimensionObject properties
    SerializedProperty dimension;
    SerializedProperty camQuadrant;
    SerializedProperty dimensionShiftQuadrant;
    SerializedProperty minAngle;
    SerializedProperty maxAngle;
    SerializedProperty pillars;
    SerializedProperty collideWithPlayerWhileInvisible;
    SerializedProperty thisObjectMoves;
    SerializedProperty thisRigidbody;
    SerializedProperty colliderBoundsOverride;

    protected override void OnEnable() {
        base.OnEnable();
        
        dimension = serializedObject.FindProperty("_dimension");
        camQuadrant = serializedObject.FindProperty("camQuadrant");
        dimensionShiftQuadrant = serializedObject.FindProperty("dimensionShiftQuadrant");
        minAngle = serializedObject.FindProperty("minAngle");
        maxAngle = serializedObject.FindProperty("maxAngle");
        pillars = serializedObject.FindProperty("pillars");
        collideWithPlayerWhileInvisible = serializedObject.FindProperty("collideWithPlayerWhileInvisible");
        thisObjectMoves = serializedObject.FindProperty("thisObjectMoves");
        thisRigidbody = serializedObject.FindProperty("thisRigidbody");
        colliderBoundsOverride = serializedObject.FindProperty("colliderBoundsOverride");
    }

    public override void OnInspectorGUI() {
        collisionMatrixValid = false;
        
        serializedObject.Update();
        
        DebugField();

        pillarDimensionObjSetupShown = EditorGUILayout.Foldout(pillarDimensionObjSetupShown, "Pillar dimension object setup:");

        if (pillarDimensionObjSetupShown) {
            EditorGUI.indentLevel++;
            GUIContent pillarsLabel = new GUIContent("Pillars: ");
            EditorGUILayout.PropertyField(pillars, pillarsLabel);

            GUIContent dimensionLabel = new GUIContent("Dimension: ");
            EditorGUILayout.PropertyField(dimension, dimensionLabel);

            GUIContent collideWithPlayerWhileInvisibleLabel = new GUIContent("Collide with Player while invisible: ");
            EditorGUILayout.PropertyField(collideWithPlayerWhileInvisible, collideWithPlayerWhileInvisibleLabel);
            
            GUIContent thisObjectMovesLabel = new GUIContent("This object moves: ");
            EditorGUILayout.PropertyField(thisObjectMoves, thisObjectMovesLabel);

            if (thisObjectMoves.boolValue) {
                GUIContent thisRigidbodyLabel = new GUIContent("Rigidbody: ");
                EditorGUILayout.PropertyField(thisRigidbody, thisRigidbodyLabel);
            }

            GUIContent useColliderOverrideLabel = new GUIContent("Use collider for bounds: ");
            EditorGUILayout.PropertyField(colliderBoundsOverride, useColliderOverrideLabel);
            EditorGUI.indentLevel--;
        }

        AddSeparator();
        
        dimensionStateShown = EditorGUILayout.Foldout(dimensionStateShown, "Dimension state:");

        if (dimensionStateShown) {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Player Quadrant:", camQuadrant.enumDisplayNames[camQuadrant.intValue]);
            EditorGUILayout.LabelField(
                "Dimension Shift Quadrant:",
                dimensionShiftQuadrant.enumDisplayNames[dimensionShiftQuadrant.intValue]
            );
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Min angle for active pillar: ", minAngle.floatValue);
            EditorGUILayout.FloatField("Max angle for active pillar: ", maxAngle.floatValue);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
        
        AddSeparator();
        
        base.OnInspectorGUI();
    }
}