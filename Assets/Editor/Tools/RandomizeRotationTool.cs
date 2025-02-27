using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RandomizeRotationTool : ScriptableWizard {
    public float range = 180;

    // TODO: Learn how to make this undo-able
    void OnWizardCreate() {
        foreach (GameObject go in Selection.gameObjects) {
            float angle = Random.Range(-range, range);
            go.transform.Rotate(0, angle, 0);

            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }

    [MenuItem("My Tools/Randomize Y Rotation")]
    static void RandomizeYRotationWizard() {
        DisplayWizard<RandomizeRotationTool>("Randomize Y Rotation", "Randomize");
    }
}