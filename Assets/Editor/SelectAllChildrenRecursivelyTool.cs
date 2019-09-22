using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using EpitaphUtils;

public class SelectAllChildrenRecursivelyTool : ScriptableWizard {
	public string typeName;

	[MenuItem("My Tools/Select All Children Recursively")]
	static void SelectAllChildren() {
		DisplayWizard<SelectAllChildrenRecursivelyTool>("Select All Children Recursively", "Select All of Type");
	}

	private void OnWizardCreate() {
		var type = GetTypeByName(typeName);
		if (type == null) {
			return;
		}

		MethodInfo method = GetType().GetMethod("SelectAllChildrenWithType")
							 .MakeGenericMethod(new Type[] { type });
		method.Invoke(this, new object[] { });
	}

	public void SelectAllChildrenWithType<T>() where T : Component {
		List<GameObject> newSelection = new List<GameObject>();
		foreach (GameObject go in Selection.gameObjects) {
			SelectAllChildrenRecusively<T>(go, ref newSelection);
		}
		Selection.objects = newSelection.ToArray();
	}

	public void SelectAllChildrenRecusively<T>(GameObject curNode, ref List<GameObject> selectionSoFar) where T : Component {
		if (curNode.GetComponent<T>() != null) {
			selectionSoFar.Add(curNode);
		}

		foreach (T child in curNode.transform.GetComponentsInChildren<T>()) {
			if (child.gameObject != curNode) {
				SelectAllChildrenRecusively<T>(child.gameObject, ref selectionSoFar);
			}
		}
	}

	/// <summary>
	/// Gets a all Type instances matching the specified class name with just non-namespace qualified class name.
	/// </summary>
	/// <param name="className">Name of the class sought.</param>
	/// <returns>Types that have the class name specified. They may not be in the same namespace.</returns>
	public static Type GetTypeByName(string className) {
		List<Type> returnVal = new List<Type>();

		foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
			Type[] assemblyTypes = a.GetTypes();
			for (int j = 0; j < assemblyTypes.Length; j++) {
				if (assemblyTypes[j].FullName.EndsWith(className)) {
					returnVal.Add(assemblyTypes[j]);
				}
			}
		}

		int typesFound = returnVal.Count;
		if (typesFound > 1) {
			string exceptionMsg = "Found " + typesFound + " types matching the name " + className + ". Try including a namespace to narrow the search.\n";
			foreach (Type found in returnVal) {
				exceptionMsg += found.ToString() + "\n";
			}
			throw new Exception(exceptionMsg);
		}
		if (typesFound == 0) {
			throw new Exception("Found no types matching the name " + className);
		}

		return returnVal[0];
	}
}
