using System;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TranslateMultipleObjectsTool : EditorWindow {
    public enum TranslateOperation {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    private static TranslateOperation op = TranslateOperation.Add;
    private static float magnitude;
    private static bool translateX = false;
    private static bool translateY = false;
    private static bool translateZ = false;
    
    [MenuItem("CONTEXT/Transform/Bulk Translate")]
    public static void TranslateMultipleObjects () {
        TranslateMultipleObjectsTool wnd = GetWindow<TranslateMultipleObjectsTool>();
        wnd.titleContent = new GUIContent("TranslateMultipleObjectsTool");
    }

    public static void CloseWindow() {
        TranslateMultipleObjectsTool wnd = GetWindow<TranslateMultipleObjectsTool>();
        wnd.Close();
    }

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy
        Label label = new Label("Enter your translation parameters");
        root.Add(label);
        
        var operation = new EnumField("Operation:", op);
        operation.RegisterValueChangedCallback(evt => op = (TranslateOperation)evt.newValue);
        root.Add(operation);

        FloatField input = new FloatField($"Magnitude:") {
            value = magnitude
        };
        root.Add(input);
        
        Toggle toggleX = new Toggle {
            name = "X",
            label = "Translate X value",
            value = translateX
        };
        Toggle toggleY = new Toggle {
            name = "Y",
            label = "Translate Y value",
            value = translateY
        };
        Toggle toggleZ = new Toggle {
            name = "Z",
            label = "Translate Z value",
            value = translateZ
        };
        // Create toggles
        root.Add(toggleX);
        root.Add(toggleY);
        root.Add(toggleZ);

        // Create button
        Button button = new Button {
            name = "Translate",
            text = "Translate"
        };
        button.clicked += () => {
            if (TranslateObjects(op, input.value, toggleX.value, toggleY.value, toggleZ.value)) {
                magnitude = input.value;
                translateX = toggleX.value;
                translateY = toggleY.value;
                translateZ = toggleZ.value;
                CloseWindow();
            }
        };
        root.Add(button);
    }

    // Needed because else we will invoke on same objects repeatedly
    private static HashSet<Transform> processedObjects = new HashSet<Transform>();
    [MenuItem("CONTEXT/Transform/Repeat Last Bulk Translation %#t")]
    public static void RepeatLastBulkTranslation() {
        TranslateObjects(op, magnitude, translateX, translateY, translateZ);
    }
    
    public static bool TranslateObjects(TranslateOperation op, float value, bool translateX, bool translateY, bool translateZ) {
        Debug.Log($"DEBUG: TranslateObjects: op: {op}, value: {value}, translateX: {translateX}, translateY: {translateY}, translateZ: {translateZ}");
        
        if (!translateX && !translateY && !translateZ) {
            Debug.LogError("No axis selected for translation");
            return false;
        }
        
        foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
            if (processedObjects.Contains(t)) continue;
            switch (op) {
                case TranslateOperation.Add:
                    t.localPosition += new Vector3(translateX ? value : 0, translateY ? value : 0, translateZ ? value : 0);
                    break;
                case TranslateOperation.Subtract:
                    t.localPosition -= new Vector3(translateX ? value : 0, translateY ? value : 0, translateZ ? value : 0);
                    break;
                case TranslateOperation.Multiply:
                    t.localPosition = t.localPosition.ScaledBy(translateX ? value : 1, translateY ? value : 1, translateZ ? value : 1);
                    break;
                case TranslateOperation.Divide:
                    t.localPosition = t.localPosition.ScaledBy(translateX ? 1f / value : 1, translateY ? 1f / value : 1, translateZ ? 1f / value : 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            processedObjects.Add(t);
        }

        EditorApplication.delayCall += () => processedObjects.Clear();
        return true;
    }
}
