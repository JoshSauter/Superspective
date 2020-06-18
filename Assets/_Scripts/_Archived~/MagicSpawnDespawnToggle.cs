using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// For use when you want to enable some objects/scripts when triggered normally
// and to disable those objects/scripts when triggered backwards
public class MagicSpawnDespawnToggle : MagicSpawnDespawn {

	// Use this for initialization
	protected override void Start () {
		base.Start();
		OnNegativeMagicTriggerStayOneTime += ReverseEnableDisableObjects;
	}

	protected void ReverseEnableDisableObjects(Collider o) {
		foreach (var objectToDisable in objectsToEnable) {
			objectToDisable.SetActive(false);
		}
		foreach (var objectToEnable in objectsToDisable) {
			objectToEnable.SetActive(true);
		}
		foreach (var scriptToDisable in scriptsToEnable) {
			scriptToDisable.enabled = false;
		}
		foreach (var scriptToEnable in scriptsToDisable) {
			scriptToEnable.enabled = true;
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(MagicSpawnDespawnToggle))]
[CanEditMultipleObjects]
public class MagicSpawnDespawnToggleEditor : MagicSpawnDespawnEditor {
	public override void MoreOnInspectorGUI() {
		base.MoreOnInspectorGUI();
	}
}

#endif