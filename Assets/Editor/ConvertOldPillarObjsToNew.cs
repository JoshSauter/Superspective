using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MagicTriggerMechanics;

public class ConvertOldPillarObjsToNew : ScriptableWizard {
	[MenuItem("My Tools/Convert Old PillarDimensionObject to New PillarDimensionObject")]
	static void ConvertOldMagicTriggerToNewWizard() {
		DisplayWizard<ConvertOldPillarObjsToNew>("Convert old PillarDimensionObjects to New PillarDimensionObject", "Convert All Selected");
	}

	// TODO: Learn how to make this undo-able
	private void OnWizardCreate() {
		int counter = 0;
		foreach (GameObject go in Selection.gameObjects) {
			if (go.GetComponents<PillarDimensionObject>().Length > 1) {
				Debug.LogError($"Multiple PillarDimensionObjects on {go.name}, handle manually.", go);
				continue;
			}
			PillarDimensionObject oldPillarDimensionObject = go.GetComponent<PillarDimensionObject>();
			if (oldPillarDimensionObject != null) {
				ConvertPillarDimensionObject(oldPillarDimensionObject);
				counter++;

				EditorSceneManager.MarkSceneDirty(go.scene);
			}
		}

		Debug.Log($"Successfully converted {counter} MagicTriggers into MagicTriggerNew");
	}

	static void ConvertPillarDimensionObject(PillarDimensionObject oldDimensionObj) {
		// Get or add PillarDimensionObject2
		PillarDimensionObject2 newDimensionObj = oldDimensionObj.gameObject.GetComponent<PillarDimensionObject2>();
		if (newDimensionObj == null) {
			newDimensionObj = oldDimensionObj.gameObject.AddComponent<PillarDimensionObject2>();
		}

		//oldDimensionObj.FindRelevantPillars();
		newDimensionObj.pillars = oldDimensionObj.pillarsFound.ToArray();

		oldDimensionObj.enabled = false;
	}

	static T[] DeepListCopy<T>(T[] original) {
		List<T> returnList = new List<T>();

		foreach (T i in original) {
			returnList.Add(i);
		}

		return returnList.ToArray();
	}
}
