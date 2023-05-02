using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class SelectAllChildrenRecursivelyTool : ScriptableWizard {
    const string nameKey = "SelectAllChildrenRecursivelyTypeName";
    public bool selectInactive = true;
    public string typeName;

    // Called when user clicks "Create" button (may be renamed)
    void OnWizardCreate() {
        PlayerPrefs.SetString(nameKey, typeName);
        PlayerPrefs.Save();
        Type type = GetTypeByName(typeName);
        if (type == null) return;

        MethodInfo method = GetType().GetMethod(nameof(SelectAllChildrenWithType))
            .MakeGenericMethod(type);
        method.Invoke(this, new object[] { });
    }

    [MenuItem("My Tools/Select All Children Recursively")]
    static void SelectAllChildren() {
        DisplayWizard<SelectAllChildrenRecursivelyTool>("Select All Children Recursively", "Select All of Type")
            .typeName = PlayerPrefs.GetString(nameKey, "");
    }

    public void SelectAllChildrenWithType<T>() where T : Component {
        List<GameObject> newSelection = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects) {
            SelectAllChildrenRecursively<T>(go, ref newSelection);
        }

        Selection.objects = newSelection.ToArray();
        Debug.Log($"{Selection.count} objects found of type {typeName}.");
    }

    public void SelectAllChildrenRecursively<T>(GameObject curNode, ref List<GameObject> selectionSoFar)
        where T : Component {
        if (curNode.GetComponent<T>() != null) selectionSoFar.Add(curNode);

        foreach (T child in curNode.transform.GetComponentsInChildren<T>(selectInactive)) {
            if (child.gameObject != curNode) SelectAllChildrenRecursively<T>(child.gameObject, ref selectionSoFar);
        }
    }

    /// <summary>
    ///     Gets a all Type instances matching the specified class name with just non-namespace qualified class name.
    /// </summary>
    /// <param name="className">Name of the class sought.</param>
    /// <returns>Types that have the class name specified. They may not be in the same namespace.</returns>
    public static Type GetTypeByName(string className) {
        List<Type> returnVal = new List<Type>();

        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
            Type[] assemblyTypes = a.GetTypes();
            for (int j = 0; j < assemblyTypes.Length; j++) {
                if (assemblyTypes[j].FullName.EndsWith(className)) returnVal.Add(assemblyTypes[j]);
            }
        }

        int typesFound = returnVal.Count;
        if (typesFound > 1) {
            List<Type> exactMatches = returnVal.FindAll(t => t.FullName.Split('.').Last() == className);
            if (exactMatches.Count > 0) returnVal = exactMatches;
        }

        typesFound = returnVal.Count;
        if (typesFound > 1) {
            // If there is an exact full match, prioritize that
            foreach (var type in returnVal.Where(type => type.FullName == className)) {
                return type;
            }
            
            string exceptionMsg = "Found " + typesFound + " types matching the name " + className +
                                  ". Try including a namespace to narrow the search.\n";
            foreach (Type found in returnVal) {
                exceptionMsg += found + "\n";
            }

            throw new Exception(exceptionMsg);
        }

        if (typesFound == 0) throw new Exception("Found no types matching the name " + className);

        return returnVal[0];
    }
}