using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class SpawnDespawnOnButtonPress : MonoBehaviour {
	UniqueId _id;
	public UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}

	public Button button;
	public GameObject[] objectsToEnable;
	public GameObject[] objectsToDisable;
	public MonoBehaviour[] scriptsToEnable;
	public MonoBehaviour[] scriptsToDisable;

	// Use this for initialization
	private void Awake() {
		if (button == null) {
			button = GetComponent<Button>();
		}
	}
	void Start () {
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

	#region Saving
	public bool SkipSave { get; set; }
	public string ID => $"SpawnDespawnOnButtonPress_{id.uniqueId}";

	[Serializable]
	class SpawnDespawnOnButtonPressSave {
		List<bool> gameObjectsToEnableState = new List<bool>();
		List<bool> gameObjectsToDisableState = new List<bool>();
		List<bool> scriptsToEnableState = new List<bool>();
		List<bool> scriptsToDisableState = new List<bool>();

		public SpawnDespawnOnButtonPressSave(SpawnDespawnOnButtonPress script) {
			if (script.objectsToEnable != null) {
				foreach (var objToEnable in script.objectsToEnable) {
					this.gameObjectsToEnableState.Add(objToEnable.activeSelf);
				}
			}
			if (script.objectsToDisable != null) {
				foreach (var objToDisable in script.objectsToDisable) {
					this.gameObjectsToDisableState.Add(objToDisable.activeSelf);
				}
			}
			if (script.scriptsToEnable != null) {
				foreach (var scriptToEnable in script.scriptsToEnable) {
					this.scriptsToEnableState.Add(scriptToEnable.enabled);
				}
			}
			if (script.scriptsToDisable != null) {
				foreach (var scriptToDisable in script.scriptsToDisable) {
					this.scriptsToDisableState.Add(scriptToDisable.enabled);
				}
			}
		}

		public void LoadSave(SpawnDespawnOnButtonPress script) {
			if (script.objectsToEnable != null) {
				for (int i = 0; i < script.objectsToEnable.Length; i++) {
					script.objectsToEnable[i].SetActive(this.gameObjectsToEnableState[i]);
				}
			}
			if (script.objectsToDisable != null) {
				for (int i = 0; i < script.objectsToDisable.Length; i++) {
					script.objectsToDisable[i].SetActive(this.gameObjectsToDisableState[i]);
				}
			}
			if (script.scriptsToEnable != null) {
				for (int i = 0; i < script.scriptsToEnable.Length; i++) {
					script.scriptsToEnable[i].enabled = this.scriptsToEnableState[i];
				}
			}
			if (script.scriptsToDisable != null) {
				for (int i = 0; i < script.scriptsToDisable.Length; i++) {
					script.scriptsToDisable[i].enabled = this.scriptsToDisableState[i];
				}
			}
		}
	}

	public object GetSaveObject() {
		return new SpawnDespawnOnButtonPressSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		SpawnDespawnOnButtonPressSave save = savedObject as SpawnDespawnOnButtonPressSave;

		save.LoadSave(this);
	}
	#endregion
}
