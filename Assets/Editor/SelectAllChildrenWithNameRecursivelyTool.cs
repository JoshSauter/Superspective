using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectAllChildrenWithNameRecursivelyTool : ScriptableWizard {
    public bool selectInactive = true;
    public string nameToMatch;

    void OnWizardCreate() {
        List<GameObject> newSelection = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects) {
            SelectAllChildrenRecusivelyWithName(go, ref newSelection);
        }

        Selection.objects = newSelection.ToArray();
        Debug.Log($"{Selection.count} objects found containing {nameToMatch}.");
    }

    [MenuItem("My Tools/Selection/Select All Children With Name Recursively")]
    static void SelectAllChildren() {
        DisplayWizard<SelectAllChildrenWithNameRecursivelyTool>(
            "Select All Children Recursively",
            "Select All Containing Name"
        );
    }

    public void SelectAllChildrenRecusivelyWithName(GameObject curNode, ref List<GameObject> selectionSoFar) {
        if (curNode.name.Contains(nameToMatch)) selectionSoFar.Add(curNode);

        foreach (Transform child in curNode.transform.GetComponentsInChildren<Transform>(selectInactive)) {
            if (child.gameObject != curNode) SelectAllChildrenRecusivelyWithName(child.gameObject, ref selectionSoFar);
        }
    }
}