using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Transition2_3 {
	public class GoingSomewhereSwitch : MonoBehaviour {
		public GameObject[] objectsToEnable;
		public GameObject[] objectsToDisable;
		public TeleportEnter[] teleporters;

		bool hasDisplayedGoingSomewhereOnce = false;

		// Update is called once per frame
		void Awake() {
			foreach (var teleporter in teleporters) {
				teleporter.OnTeleport += TurnObjectsOnOff;
			}
		}

		void OnDisable() {
			foreach (var teleporter in teleporters) {
				teleporter.OnTeleport -= TurnObjectsOnOff;
			}
		}

		void TurnObjectsOnOff(Collider teleportEnter, Collider teleportExit, GameObject player) {
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
}