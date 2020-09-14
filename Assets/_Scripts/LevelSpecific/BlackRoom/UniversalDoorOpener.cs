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

		void OpenThisDoor(DoorOpenClose doorOpened) {
			thisDoor.OpenDoor();
		}

		void CloseThisDoor(DoorOpenClose doorClosed) {
			thisDoor.CloseDoor();
		}
	}
}