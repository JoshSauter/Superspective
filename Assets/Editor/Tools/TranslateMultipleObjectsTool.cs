using System;
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
    
    [MenuItem("CONTEXT/Transform/Translate Multiple Objects")]
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

        FloatField input = new FloatField($"Magnitude:");
        root.Add(input);
        
        Toggle toggleX = new Toggle {
            name = "X",
            label = "Translate X value"
        };
        Toggle toggleY = new Toggle {
            name = "Y",
            label = "Translate Y value"
        };
        Toggle toggleZ = new Toggle {
            name = "Z",
            label = "Translate Z value"
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
                CloseWindow();
            }
        };
        root.Add(button);
    }
    
    public static bool TranslateObjects(TranslateOperation op, float value, bool translateX, bool translateY, bool translateZ) {
        Debug.Log($"DEBUG: TranslateObjects: op: {op}, value: {value}, translateX: {translateX}, translateY: {translateY}, translateZ: {translateZ}");
        
        if (!translateX && !translateY && !translateZ) {
            Debug.LogError("No axis selected for translation");
            return false;
        }
        
        foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
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
        }

        return true;
    }
}
