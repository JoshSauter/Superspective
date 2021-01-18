using System.Collections.Generic;
using System.Linq;
using Saving;
using UnityEditor;
using UnityEngine;

public class FindObjectByIdTool : ScriptableWizard {
    const string nameKey = "FindObjectByIdLastIdUsed";
    public string id;

    // Called when user clicks "Create" button (may be renamed)
    void OnWizardCreate() {
        PlayerPrefs.SetString(nameKey, id);
        PlayerPrefs.Save();

        FindObjectById();
    }

    [MenuItem("My Tools/Find Object By Id _%SPACE")]
    static void FindObjectByIdWizard() {
        DisplayWizard<FindObjectByIdTool>("Find Object By Id", "Find").id = PlayerPrefs.GetString(nameKey, "");
    }

    public void FindObjectById() {
        List<MonoBehaviour> matches = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .OfType<SaveableObject>()
            .Where(s => HasValidId(s) && s.ID == id)
            .OfType<MonoBehaviour>()
            .ToList();

        if (matches.Count == 0)
            Debug.LogError($"No object with id {id} found! Maybe in a scene that's not loaded?");
        else if (matches.Count > 1) {
            Debug.LogWarning($"Multiple objects with id {id} found.");
            Selection.objects = matches.Select(s => s.gameObject).ToArray();
        }
        else
            Selection.objects = matches.Select(s => s.gameObject).ToArray();
    }

    bool HasValidId(SaveableObject obj) {
        try {
            string s = obj.ID;

            return s != null && s != "";
        }
        catch {
            return false;
        }
    }
}