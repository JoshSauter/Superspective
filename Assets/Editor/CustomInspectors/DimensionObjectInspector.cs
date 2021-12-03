using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DimensionObject))]
[CanEditMultipleObjects]
public class DimensionObjectInspector : UnityEditor.Editor {
    bool DEBUGShown = false;
    protected bool collisionMatrixValid = true;
    bool collisionMatrixHelp = false;
    static bool collisionMatrixShown = true;
    static bool channelSettingsShown = true;
    static bool otherOptionsShown = true;
    static bool renderersAndCollidersShown = true;
    static bool visibilityStateShown = true;
    static readonly Dictionary<int, string> visibilityStateLabels = new Dictionary<int, string>() {
        { 0, "I" },
        { 1, "PV" },
        { 2, "V" },
        { 3, "PI" },
        { 4, "Player" },
        { 5, "Other" }
    };
    VisibilityState cachedVisibilityState;

    SerializedProperty id;
    SerializedProperty DEBUG;
    SerializedProperty channel;
    SerializedProperty useAdvancedChannelLogic;
    SerializedProperty channelLogic;
    SerializedProperty reverseVisibilityStates;
    
    SerializedProperty treatChildrenAsOneObjectRecursively;
    SerializedProperty ignoreChildrenWithDimensionObject;
    SerializedProperty ignorePartiallyVisibleLayerChanges;
    SerializedProperty disableColliderWhileInvisible;
    
    SerializedProperty renderers;
    SerializedProperty colliders;
    
    SerializedProperty startingVisibilityState;
    SerializedProperty visibilityState;
    
    protected virtual void OnEnable() {
        id = serializedObject.FindProperty("_id");
        DEBUG = serializedObject.FindProperty("DEBUG");
        channel = serializedObject.FindProperty("channel");
        useAdvancedChannelLogic = serializedObject.FindProperty("useAdvancedChannelLogic");
        channelLogic = serializedObject.FindProperty("channelLogic");
        reverseVisibilityStates = serializedObject.FindProperty("reverseVisibilityStates");
        
        treatChildrenAsOneObjectRecursively = serializedObject.FindProperty("treatChildrenAsOneObjectRecursively");
        ignoreChildrenWithDimensionObject = serializedObject.FindProperty("ignoreChildrenWithDimensionObject");
        ignorePartiallyVisibleLayerChanges = serializedObject.FindProperty("ignorePartiallyVisibleLayerChanges");
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
        
        DebugField();

        channelSettingsShown = EditorGUILayout.Foldout(channelSettingsShown, "Channel settings:");
        if (channelSettingsShown) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useAdvancedChannelLogic, new GUIContent("Use advanced channel logic"));
            if (useAdvancedChannelLogic.boolValue) {
                EditorGUILayout.PropertyField(channelLogic, new GUIContent("Channel logic: "));
                if (GUILayout.Button("Apply")) {
                    dimensionObject.ValidateAndApplyChannelLogic();
                }
            }
            else {
                EditorGUILayout.IntSlider(channel, 0, DimensionObject.NUM_CHANNELS - 1, new GUIContent("Channel: "));
            }

            EditorGUILayout.PropertyField(reverseVisibilityStates);
            EditorGUI.indentLevel--;
        }

        AddSeparator();

        if (collisionMatrixValid) {
            CollisionMatrixWithHelpButton(ref collisionMatrixHelp);
            OptionalHelpBox(
                collisionMatrixHelp,
                "The row indicates the effective visibility state of this object, the column the effective visibility state of the other DimensionObject (other for non-DimensionObjects)." +
                "\n\nNOTE: If reverseVisibilityStates is enabled, the effective VisibilityState is flipped to match."
            );
            if (collisionMatrixShown) {
                EditorGUI.indentLevel++;
                const int cols = DimensionObject.COLLISION_MATRIX_COLS;
                const int rows = DimensionObject.COLLISION_MATRIX_ROWS;
                for (int i = 0; i < rows; i++) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(visibilityStateLabels[i], GUILayout.MaxWidth(80));
                    for (int j = 0; j < cols; j++) {
                        GUILayout.Label(visibilityStateLabels[j]);
                        dimensionObject.collisionMatrix[i * cols + j] =
                            EditorGUILayout.Toggle(dimensionObject.collisionMatrix[i * cols + j]);
                        if (j < rows) {
                            // Fix & reset collision matrices that are of the wrong size
                            if (Mathf.Max(j * cols + i, i * cols + j) >= dimensionObject.collisionMatrix.Length) {
                                dimensionObject.collisionMatrix = new bool[] {
                                    true, false, false, false, false, false,
                                    false,  true, false, false, false, false,
                                    false, false,  true, false,  true,  true,
                                    false, false, false,  true,  true,  true
                                };
                                EditorGUI.indentLevel--;
                                return;
                            }

                            dimensionObject.collisionMatrix[j * cols + i] =
                                dimensionObject.collisionMatrix[i * cols + j];
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            AddSeparator();
        }

        otherOptionsShown = EditorGUILayout.Foldout(otherOptionsShown, "Other options:");
        if (otherOptionsShown) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(treatChildrenAsOneObjectRecursively);
            EditorGUILayout.PropertyField(ignoreChildrenWithDimensionObject);
            EditorGUILayout.PropertyField(ignorePartiallyVisibleLayerChanges);
            EditorGUILayout.PropertyField(disableColliderWhileInvisible);
            EditorGUI.indentLevel--;
        }

        AddSeparator();
        
        renderersAndCollidersShown = EditorGUILayout.Foldout(renderersAndCollidersShown, "Renderers and Colliders:");

        if (renderersAndCollidersShown) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(renderers);
            EditorGUILayout.PropertyField(colliders);
            EditorGUI.indentLevel--;
        }

        AddSeparator();

        visibilityStateShown = EditorGUILayout.Foldout(visibilityStateShown, "Visibility State:");
        if (visibilityStateShown) {
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginDisabledGroup(true);
            VisibilityState effectiveVisibilityState = (VisibilityState) visibilityState.enumValueIndex;
            effectiveVisibilityState = reverseVisibilityStates.boolValue
                ? effectiveVisibilityState.Opposite()
                : effectiveVisibilityState;
            EditorGUILayout.EnumPopup("Effective Visibility:", effectiveVisibilityState);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.PropertyField(startingVisibilityState);
            EditorGUILayout.PropertyField(visibilityState);
            if ((VisibilityState) visibilityState.enumValueIndex != cachedVisibilityState) {
                cachedVisibilityState = (VisibilityState) visibilityState.enumValueIndex;
                if (Application.IsPlaying(target)) {
                    dimensionObject.SwitchVisibilityState(cachedVisibilityState, true);
                }
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        DEBUGShown = false;
    }

    protected void DebugField() {
        EditorGUILayout.PropertyField(id, new GUIContent("UniqueId"));
        
        if (!DEBUGShown) {
            EditorGUILayout.PropertyField(DEBUG, new GUIContent("Debug?"));

            AddSeparator();

            DEBUGShown = true;
        }
    }
    
    protected void AddSeparator() {
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
