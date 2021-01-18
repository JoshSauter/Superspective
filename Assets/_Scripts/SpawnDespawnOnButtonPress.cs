using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class SpawnDespawnOnButtonPress : MonoBehaviour {
    public Button button;
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;
    public MonoBehaviour[] scriptsToEnable;
    public MonoBehaviour[] scriptsToDisable;
    UniqueId _id;

    public UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    // Use this for initialization
    void Awake() {
        if (button == null) button = GetComponent<Button>();
    }

    void Start() {
        button.OnButtonPressBegin += ctx => EnableDisableObjects();
        button.OnButtonDepressBegin += ctx => ReverseEnableDisableObjects();
    }

    void EnableDisableObjects() {
        foreach (GameObject objectToEnable in objectsToEnable) {
            objectToEnable.SetActive(true);
        }

        foreach (GameObject objectToDisable in objectsToDisable) {
            objectToDisable.SetActive(false);
        }

        foreach (MonoBehaviour scriptToEnable in scriptsToEnable) {
            scriptToEnable.enabled = true;
        }

        foreach (MonoBehaviour scriptToDisable in scriptsToDisable) {
            scriptToDisable.enabled = false;
        }
    }

    void ReverseEnableDisableObjects() {
        foreach (GameObject objectToDisable in objectsToEnable) {
            objectToDisable.SetActive(false);
        }

        foreach (GameObject objectToEnable in objectsToDisable) {
            objectToEnable.SetActive(true);
        }

        foreach (MonoBehaviour scriptToDisable in scriptsToEnable) {
            scriptToDisable.enabled = false;
        }

        foreach (MonoBehaviour scriptToEnable in scriptsToDisable) {
            scriptToEnable.enabled = true;
        }
    }

#region Saving
    public bool SkipSave { get; set; }
    public string ID => $"SpawnDespawnOnButtonPress_{id.uniqueId}";

    [Serializable]
    class SpawnDespawnOnButtonPressSave {
        List<bool> gameObjectsToDisableState = new List<bool>();
        List<bool> gameObjectsToEnableState = new List<bool>();
        List<bool> scriptsToDisableState = new List<bool>();
        List<bool> scriptsToEnableState = new List<bool>();

        public SpawnDespawnOnButtonPressSave(SpawnDespawnOnButtonPress script) {
            if (script.objectsToEnable != null) {
                foreach (GameObject objToEnable in script.objectsToEnable) {
                    gameObjectsToEnableState.Add(objToEnable.activeSelf);
                }
            }

            if (script.objectsToDisable != null) {
                foreach (GameObject objToDisable in script.objectsToDisable) {
                    gameObjectsToDisableState.Add(objToDisable.activeSelf);
                }
            }

            if (script.scriptsToEnable != null) {
                foreach (MonoBehaviour scriptToEnable in script.scriptsToEnable) {
                    scriptsToEnableState.Add(scriptToEnable.enabled);
                }
            }

            if (script.scriptsToDisable != null) {
                foreach (MonoBehaviour scriptToDisable in script.scriptsToDisable) {
                    scriptsToDisableState.Add(scriptToDisable.enabled);
                }
            }
        }

        public void LoadSave(SpawnDespawnOnButtonPress script) {
            if (script.objectsToEnable != null) {
                for (int i = 0; i < script.objectsToEnable.Length; i++) {
                    script.objectsToEnable[i].SetActive(gameObjectsToEnableState[i]);
                }
            }

            if (script.objectsToDisable != null) {
                for (int i = 0; i < script.objectsToDisable.Length; i++) {
                    script.objectsToDisable[i].SetActive(gameObjectsToDisableState[i]);
                }
            }

            if (script.scriptsToEnable != null) {
                for (int i = 0; i < script.scriptsToEnable.Length; i++) {
                    script.scriptsToEnable[i].enabled = scriptsToEnableState[i];
                }
            }

            if (script.scriptsToDisable != null) {
                for (int i = 0; i < script.scriptsToDisable.Length; i++) {
                    script.scriptsToDisable[i].enabled = scriptsToDisableState[i];
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