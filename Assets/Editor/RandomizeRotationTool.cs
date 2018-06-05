using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class RandomizeRotationTool : ScriptableWizard {
	public float range = 180;

	[MenuItem("My Tools/Randomize Y Rotation")]
	static void RandomizeYRotationWizard() {
		DisplayWizard<RandomizeRotationTool>("Randomize Y Rotation", "Randomize");
	}

	// TODO: Learn how to make this undo-able
	private void OnWizardCreate() {
		foreach (GameObject go in Selection.gameObjects) {
			float angle = Random.Range(-range, range);
			go.transform.Rotate(0, angle, 0);

			EditorSceneManager.MarkSceneDirty(go.scene);
		}
	}
}
