using System.Collections.Generic;
using System.Linq;
using Saving;
using UnityEditor;
using UnityEngine;

public class FindObjectByIdTool : EditorWindow {
    const string nameKey = "FindObjectByIdLastIdUsed";
    private readonly StringEditorSetting ID = new StringEditorSetting("FindObjectByIdLastIdUsed", "");

    private static bool windowOpen = false;

    [MenuItem("My Tools/Find Object By Id _%SPACE")]
    public static void ShowWindow() {
        var window = GetWindow<FindObjectByIdTool>("Find Object By Id (Ctrl+Space)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        } else {
            window.Show();
            windowOpen = true;
        }
    }

    private void OnDestroy() {
        windowOpen = false;
    }

    private void OnGUI() {
        EditorGUILayout.Separator();
        
        // Set the ID.Value to the user input
        GUILayout.BeginHorizontal();
        GUILayout.Label("Find:", GUILayout.Width(35)); // Small label
        ID.Value = GUILayout.TextField(ID.Value, GUILayout.ExpandWidth(true)); // Large text field
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();
        
        if (GUILayout.Button("Find Object By Id")) {
            FindObjectById();
            Close();
        }
    }

    public void FindObjectById() {
        List<MonoBehaviour> matches = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .OfType<ISaveableObject>()
            .Where(s => s.HasValidId() && s.ID.Contains(ID))
            .OfType<MonoBehaviour>()
            .ToList();

        if (matches.Count == 0)
            Debug.LogError($"No object with id {ID.Value} found! Maybe in a scene that's not loaded?");
        else {
            Selection.objects = matches.Select(s => s.gameObject).ToArray();
            if (Selection.objects.Length > 1) {
                Debug.LogWarning($"Multiple objects with id {ID.Value} found.");
            }
        }
    }
}