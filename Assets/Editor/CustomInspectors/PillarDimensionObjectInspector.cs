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
    SerializedProperty objectBoundsMin;
    SerializedProperty objectBoundsMax;
    SerializedProperty visibleRangeMin;
    SerializedProperty visibleRangeMax;
    SerializedProperty partiallyVisibleMin;
    SerializedProperty partiallyVisibleMax;
    SerializedProperty partiallyInvisibleMin;
    SerializedProperty partiallyInvisibleMax;
    SerializedProperty invisibleMin;
    SerializedProperty invisibleMax;
    SerializedProperty pillars;
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
        
        SerializedProperty objectBounds = serializedObject.FindProperty("objectBounds");
        objectBoundsMin = objectBounds.FindPropertyRelative("min");
        objectBoundsMax = objectBounds.FindPropertyRelative("max");
        
        SerializedProperty visibleRange = serializedObject.FindProperty("visibleRange");
        visibleRangeMin = visibleRange.FindPropertyRelative("min");
        visibleRangeMax = visibleRange.FindPropertyRelative("max");
        
        SerializedProperty partiallyVisible = serializedObject.FindProperty("partiallyVisibleRange");
        partiallyVisibleMin = partiallyVisible.FindPropertyRelative("min");
        partiallyVisibleMax = partiallyVisible.FindPropertyRelative("max");
        
        SerializedProperty partiallyInvisible = serializedObject.FindProperty("partiallyInvisibleRange");
        partiallyInvisibleMin = partiallyInvisible.FindPropertyRelative("min");
        partiallyInvisibleMax = partiallyInvisible.FindPropertyRelative("max");
        
        SerializedProperty invisible = serializedObject.FindProperty("invisibleRange");
        invisibleMin = invisible.FindPropertyRelative("min");
        invisibleMax = invisible.FindPropertyRelative("max");
        
        pillars = serializedObject.FindProperty("pillars");
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

        float ObjectSpan(float min, float max) {
            if (min < max) return max - min;
            float value = 128 - min + max;
            return Mathf.Abs(value - Mathf.Floor(value));
        }

        if (dimensionStateShown) {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Player Quadrant:", camQuadrant.enumDisplayNames[camQuadrant.intValue]);
            EditorGUILayout.LabelField(
                "Dimension Shift Quadrant:",
                dimensionShiftQuadrant.enumDisplayNames[dimensionShiftQuadrant.intValue]
            );
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector2Field("Angle range for active pillar: ", new Vector2(minAngle.floatValue, maxAngle.floatValue));
            float objBoundsMin = objectBoundsMin.floatValue;
            float objBoundsMax = objectBoundsMax.floatValue;
            EditorGUILayout.Vector2Field("Object bounds range for active pillar: ", new Vector2(objBoundsMin, objBoundsMax));
            float midpoint = (objBoundsMin + objBoundsMax) / 2;
            if (target is PillarDimensionObject pillarDimensionObject) {
                DimensionPillar activePillar = pillarDimensionObject.activePillar;
                if (activePillar != null && objBoundsMax < objBoundsMin) {
                    int maxDimension = activePillar.maxBaseDimension + 1;
                    midpoint = (objBoundsMin + objBoundsMax + maxDimension) / 2;
                    if (midpoint > maxDimension) {
                        midpoint -= maxDimension;
                    }
                }
            }
            EditorGUILayout.FloatField("Object bounds midpoint: ", midpoint);
            EditorGUILayout.FloatField("Object bounds span: ", ObjectSpan(objectBoundsMin.floatValue, objectBoundsMax.floatValue));
            EditorGUILayout.Vector2Field("Partially visible range for active pillar: ", new Vector2(partiallyVisibleMin.floatValue, partiallyVisibleMax.floatValue));
            EditorGUILayout.FloatField("Partially visible span: ", ObjectSpan(partiallyVisibleMin.floatValue, partiallyVisibleMax.floatValue));
            EditorGUILayout.Vector2Field("Visible range for active pillar: ", new Vector2(visibleRangeMin.floatValue, visibleRangeMax.floatValue));
            EditorGUILayout.FloatField("Visible span: ", ObjectSpan(visibleRangeMin.floatValue, visibleRangeMax.floatValue));
            EditorGUILayout.Vector2Field("Partially invisible range for active pillar: ", new Vector2(partiallyInvisibleMin.floatValue, partiallyInvisibleMax.floatValue));
            EditorGUILayout.FloatField("Partially invisible span: ", ObjectSpan(partiallyInvisibleMax.floatValue, partiallyInvisibleMin.floatValue));
            EditorGUILayout.Vector2Field("Invisible range for active pillar: ", new Vector2(invisibleMin.floatValue, invisibleMax.floatValue));
            EditorGUILayout.FloatField("Invisible span: ", ObjectSpan(invisibleMax.floatValue, invisibleMin.floatValue));
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
        
        AddSeparator();
        
        base.OnInspectorGUI();
    }
}