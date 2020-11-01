using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
	public class UniversalDoorOpener : MonoBehaviour {
		public DoorOpenClose[] doorsInThisSet;

		DoorOpenClose thisDoor;

		void Start() {
			thisDoor = GetComponent<DoorOpenClose>();
			foreach (DoorOpenClose door in doorsInThisSet) {
				if (door != thisDoor) {
					door.OnDoorOpenStart += OpenThisDoor;
					door.OnDoorCloseStart += CloseThisDoor;
				}
			}
		}

		void OpenThisDoor() {
			thisDoor.OpenDoor();
		}

		void CloseThisDoor() {
			thisDoor.CloseDoor();
		}
	}
}