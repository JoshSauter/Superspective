using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using EpitaphUtils;

public class SelectAllChildrenWithNameRecursivelyTool : ScriptableWizard {
	public string nameToMatch;

	[MenuItem("My Tools/Select All Children With Name Recursively")]
	static void SelectAllChildren() {
		DisplayWizard<SelectAllChildrenWithNameRecursivelyTool>("Select All Children Recursively", "Select All Containing Name");
	}

	private void OnWizardCreate() {
		List<GameObject> newSelection = new List<GameObject>();
		foreach (GameObject go in Selection.gameObjects) {
			SelectAllChildrenRecusivelyWithName(go, ref newSelection);
		}
		Selection.objects = newSelection.ToArray();
	}

	public void SelectAllChildrenRecusivelyWithName(GameObject curNode, ref List<GameObject> selectionSoFar) {
		if (curNode.name.Contains(nameToMatch)) {
			selectionSoFar.Add(curNode);
		}

		foreach (UnityEngine.Transform child in curNode.transform) {
			if (child.gameObject != curNode) {
				SelectAllChildrenRecusivelyWithName(child.gameObject, ref selectionSoFar);
			}
		}
	}
}
