using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoingSomewhereSwitch : MonoBehaviour {
	public GameObject[] objectsToEnable;
	public GameObject[] objectsToDisable;
	public TeleportEnter teleporter;

	bool hasDisplayedGoingSomewhereOnce = false;

	// Update is called once per frame
	void Awake() {
		teleporter.OnTeleport += TurnObjectsOnOff;
	}

	void OnDisable() {
		teleporter.OnTeleport -= TurnObjectsOnOff;
	}

	void TurnObjectsOnOff(Teleport teleporter, Collider player) {
		foreach (GameObject enableObject in objectsToEnable) {
			if (enableObject.name.Contains("GS?") && hasDisplayedGoingSomewhereOnce)
				continue;
			enableObject.SetActive(true);
		}
		foreach (GameObject disableObject in objectsToDisable) {
			if (disableObject.name.Contains("GS?") && hasDisplayedGoingSomewhereOnce)
				continue;
			disableObject.SetActive(false);
		}

		hasDisplayedGoingSomewhereOnce = true;
	}
}
