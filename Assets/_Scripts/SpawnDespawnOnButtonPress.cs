using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnDespawnOnButtonPress : MonoBehaviour {
	public Button button;
	public GameObject[] objectsToEnable;
	public GameObject[] objectsToDisable;
	public MonoBehaviour[] scriptsToEnable;
	public MonoBehaviour[] scriptsToDisable;

	// Use this for initialization
	void Start () {
		if (button == null) {
			button = GetComponent<Button>();
		}

		button.OnButtonPressBegin += ctx => EnableDisableObjects();
		button.OnButtonDepressBegin += ctx => ReverseEnableDisableObjects();
	}

	void EnableDisableObjects() {
		foreach (var objectToEnable in objectsToEnable) {
			objectToEnable.SetActive(true);
		}
		foreach (var objectToDisable in objectsToDisable) {
			objectToDisable.SetActive(false);
		}
		foreach (var scriptToEnable in scriptsToEnable) {
			scriptToEnable.enabled = true;
		}
		foreach (var scriptToDisable in scriptsToDisable) {
			scriptToDisable.enabled = false;
		}
	}

	void ReverseEnableDisableObjects() {
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
