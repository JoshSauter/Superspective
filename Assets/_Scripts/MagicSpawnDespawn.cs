using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicSpawnDespawn : MagicTrigger {
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

	// Use this for initialization
	void Start () {
        OnMagicTrigger += EnableDisableObjects;
	}

    private void EnableDisableObjects(Collider o) {
        foreach (var objectToEnable in objectsToEnable) {
            objectToEnable.SetActive(true);
        }
        foreach (var objectToDisable in objectsToDisable) {
            objectToDisable.SetActive(false);
        }
    }
}
