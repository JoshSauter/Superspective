using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectAllChildrenOnLayersRecursivelyTool : ScriptableWizard {
    public bool selectInactive = true;
    public LayerMask layers;

    void OnWizardCreate() {
        List<GameObject> newSelection = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects) {
            SelectAllChildrenOnLayers(go, ref newSelection);
        }

        Selection.objects = newSelection.ToArray();
        Debug.Log($"{Selection.count} objects found on layer mask.");
    }

    [MenuItem("My Tools/Selection/Select All Children On Layer Recursively")]
    static void SelectAllChildren() {
        DisplayWizard<SelectAllChildrenOnLayersRecursivelyTool>(
            "Select All Children Recursively",
            "Select All On Layers"
        );
    }

    public void SelectAllChildrenOnLayers(GameObject curNode, ref List<GameObject> selectionSoFar) {
        if (layers == (layers | (1 << curNode.layer))) selectionSoFar.Add(curNode);

        foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
            if (child.gameObject != curNode) SelectAllChildrenOnLayers(child.gameObject, ref selectionSoFar);
        }
    }
}