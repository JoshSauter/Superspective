using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CustomHierarchy : MonoBehaviour {
    private static Vector2 offset = new Vector2(0, 2);

    static CustomHierarchy() {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) {
        Color highlightedFontColor = Color.black;
        Color highlightedBackgroundColor = Color.yellow;

        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj != null)  {
            if (Selection.instanceIDs.Contains(instanceID)) {
                Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
                EditorGUI.DrawRect(selectionRect, highlightedBackgroundColor);
                EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle() {
                    normal = new GUIStyleState() { textColor = highlightedFontColor },
                    fontStyle = FontStyle.Bold
                });
            }
        }
    }
}
