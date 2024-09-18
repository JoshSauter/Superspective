using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class SelectAllChildrenRecursivelyTool : EditorWindow {
    const string nameKey = "SelectAllChildrenRecursivelyTypeName";
    public bool selectInactive = true;
    public bool allowInheritance = true;
    public string typeName;
    private bool typeNameNullOrEmpty => string.IsNullOrEmpty(typeName);

    private string DisplayName(string name) => typeNameNullOrEmpty ? "<Type Name>" : name;
    
    // Cached selection data
    private static string prevTypeName;
    private static int goCount;
    private static GameObject[] prevSelection;
    private static bool prevAllowInheritance;
    private static bool prevSelectionCached;

    [MenuItem("My Tools/Selection/Select All Children Recursively")]
    public static void ShowWindow() {
        GetWindow(typeof(SelectAllChildrenRecursivelyTool));
    }

    void OnGUI() {
        if (typeNameNullOrEmpty && !string.IsNullOrEmpty(prevTypeName)) {
            typeName = prevTypeName;
        }

        allowInheritance = EditorGUILayout.Toggle("Allow inheritance?", allowInheritance);
        typeName = EditorGUILayout.TextField("Type Name:", typeName);
        if (GUILayout.Button($"Find {DisplayName(typeName)} in selected GameObjects")) {
            FindInSelected(Selection.gameObjects);
            Close();
        }

        if (!prevSelectionCached) return;
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField($"{goCount} GameObjects with {DisplayName(prevTypeName)} previously selected");
        EditorGUI.EndDisabledGroup();
        if (prevSelection != null) {
            if (GUILayout.Button("Reselect previous selection")) {
                ReselectPrevSelection();
                Close();
            }
        }
    }

    private void RememberSelection(GameObject[] selection) {
        prevTypeName = typeName;
        prevSelection = selection;
        goCount = selection.Length;
        prevAllowInheritance = allowInheritance;
        prevSelectionCached = true;
    }
    
    private void ReselectPrevSelection() {
        typeName = prevTypeName;
        allowInheritance = prevAllowInheritance;
        FindInSelected(prevSelection);
    }

    // Called when user clicks "Create" button (may be renamed)
    void FindInSelected(GameObject[] selection) {
        PlayerPrefs.SetString(nameKey, typeName);
        PlayerPrefs.Save();
        Type type = GetTypeByName(typeName);
        if (type == null) return;

        MethodInfo method = GetType().GetMethod(nameof(SelectAllChildrenWithType))
            .MakeGenericMethod(type);
        method.Invoke(this, new object[] { selection });
    }

    public void SelectAllChildrenWithType<T>(GameObject[] selection) where T : Component {
        // Helper function to recursively select all children of a given type
        void SelectAllChildrenRecursively(GameObject curNode, ref List<GameObject> selectionSoFar) {
            T curNodeComponent = curNode.GetComponent<T>();
            if (curNodeComponent != null && (allowInheritance || curNodeComponent.GetType().Name == typeof(T).Name)) selectionSoFar.Add(curNode);

            foreach (T child in curNode.transform.GetComponentsInChildren<T>(selectInactive)) {
                if (!allowInheritance && child.GetType().Name != typeof(T).Name) continue;
                
                if (child.gameObject != curNode) SelectAllChildrenRecursively(child.gameObject, ref selectionSoFar);
            }
        }
        
        List<GameObject> newSelection = new List<GameObject>();
        foreach (GameObject go in selection) {
            SelectAllChildrenRecursively(go, ref newSelection);
        }

        Selection.objects = newSelection.ToArray();
        RememberSelection(Selection.gameObjects);
        Debug.Log($"{Selection.count} objects found of type {typeName}.");
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