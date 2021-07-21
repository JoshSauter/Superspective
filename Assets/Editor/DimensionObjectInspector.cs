using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DimensionObject))]
[CanEditMultipleObjects]
public class DimensionObjectInspector : UnityEditor.Editor {
    bool collisionMatrixHelp = false;
    static bool collisionMatrixShown = true;
    static readonly Dictionary<int, string> visibilityStateLabels = new Dictionary<int, string>() {
        { 0, "I" },
        { 1, "PV" },
        { 2, "V" },
        { 3, "PI" },
        { 4, "Player" },
        { 5, "Other" }
    };
    VisibilityState cachedVisibilityState;
    
    SerializedProperty DEBUG;
    SerializedProperty channel;
    SerializedProperty useAdvancedChannelLogic;
    SerializedProperty channelLogic;
    SerializedProperty reverseVisibilityStates;
    
    SerializedProperty treatChildrenAsOneObjectRecursively;
    SerializedProperty ignoreChildrenWithDimensionObject;
    SerializedProperty ignoreMaterialChanges;
    SerializedProperty disableColliderWhileInvisible;
    
    SerializedProperty renderers;
    SerializedProperty colliders;
    
    SerializedProperty startingVisibilityState;
    SerializedProperty visibilityState;
    
    protected virtual void OnEnable() {
        DEBUG = serializedObject.FindProperty("DEBUG");
        channel = serializedObject.FindProperty("channel");
        useAdvancedChannelLogic = serializedObject.FindProperty("useAdvancedChannelLogic");
        channelLogic = serializedObject.FindProperty("channelLogic");
        reverseVisibilityStates = serializedObject.FindProperty("reverseVisibilityStates");
        
        treatChildrenAsOneObjectRecursively = serializedObject.FindProperty("treatChildrenAsOneObjectRecursively");
        ignoreChildrenWithDimensionObject = serializedObject.FindProperty("ignoreChildrenWithDimensionObject");
        ignoreMaterialChanges = serializedObject.FindProperty("ignoreMaterialChanges");
        disableColliderWhileInvisible = serializedObject.FindProperty("disableColliderWhileInvisible");
        
        renderers = serializedObject.FindProperty("renderers");
        colliders = serializedObject.FindProperty("colliders");
        
        startingVisibilityState = serializedObject.FindProperty("startingVisibilityState");
        visibilityState = serializedObject.FindProperty("visibilityState");
        cachedVisibilityState = (VisibilityState)visibilityState.enumValueIndex;
    }

    public override void OnInspectorGUI() {
        DimensionObject dimensionObject = (DimensionObject) target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(DEBUG, new GUIContent("Debug?"));

        AddSeparator();

        GUILayout.Label("Channel settings:", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(useAdvancedChannelLogic, new GUIContent("Use advanced channel logic"));
        if (useAdvancedChannelLogic.boolValue) {
            EditorGUILayout.PropertyField(channelLogic, new GUIContent("Channel logic: "));
            if (GUILayout.Button("Apply")) {
                dimensionObject.ValidateAndApplyChannelLogic();
            }
        }
        else {
            EditorGUILayout.IntSlider(channel, 0, DimensionObject.NUM_CHANNELS-1, new GUIContent("Channel: "));
        }

        EditorGUILayout.PropertyField(reverseVisibilityStates);
        
        AddSeparator();

        CollisionMatrixWithHelpButton(ref collisionMatrixHelp);
        OptionalHelpBox(collisionMatrixHelp, "The row indicates the effective visibility state of this object, the column the effective visibility state of the other DimensionObject (other for non-DimensionObjects)." +
                                             "\n\nNOTE: If reverseVisibilityStates is enabled, the effective VisibilityState is flipped to match.");
        if (collisionMatrixShown) {
            const int cols = DimensionObject.COLLISION_MATRIX_COLS;
            const int rows = DimensionObject.COLLISION_MATRIX_ROWS;
            for (int i = 0; i < rows; i++) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(visibilityStateLabels[i], GUILayout.MaxWidth(80));
                for (int j = 0; j < cols; j++) {
                    GUILayout.Label(visibilityStateLabels[j]);
                    dimensionObject.collisionMatrix[i * cols + j] = EditorGUILayout.Toggle(dimensionObject.collisionMatrix[i * cols + j]);
                    if (j < rows) {
                        dimensionObject.collisionMatrix[j * cols + i] = dimensionObject.collisionMatrix[i * cols + j];
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        AddSeparator();

        GUILayout.Label("Other options:", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(treatChildrenAsOneObjectRecursively);
        EditorGUILayout.PropertyField(ignoreChildrenWithDimensionObject);
        EditorGUILayout.PropertyField(ignoreMaterialChanges);
        EditorGUILayout.PropertyField(disableColliderWhileInvisible);

        AddSeparator();

        EditorGUILayout.PropertyField(renderers);
        EditorGUILayout.PropertyField(colliders);

        AddSeparator();

        EditorGUILayout.PropertyField(startingVisibilityState);
        EditorGUILayout.PropertyField(visibilityState);
        if ((VisibilityState)visibilityState.enumValueIndex != cachedVisibilityState) {
            cachedVisibilityState = (VisibilityState)visibilityState.enumValueIndex;
            if (Application.IsPlaying(target)) {
                dimensionObject.SwitchVisibilityState(cachedVisibilityState, true);
            }
        }
        
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

    void CollisionMatrixWithHelpButton(ref bool helpButtonPressed) {
        GUILayout.BeginHorizontal();
        collisionMatrixShown = EditorGUILayout.Foldout(collisionMatrixShown, "Collision Matrix:");
        helpButtonPressed = GUILayout.Toggle(helpButtonPressed, "?", "Button", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();
    }
}
