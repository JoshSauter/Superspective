using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class ToggleBlackRoomEnabled : MonoBehaviour {
        public GameObject blackRoomRoot;
        DoorOpenClose door;

        void Start() {
            door = GetComponent<DoorOpenClose>();
            door.OnDoorOpenStart += (door) => EnableBlackRoom();
            door.OnDoorCloseEnd += (door) => DisableBlackRoomIfInMainHallway();
        }

        void EnableBlackRoom() {
            blackRoomRoot.SetActive(true);
        }

        void DisableBlackRoomIfInMainHallway() {
            bool playerIsInMainHallway = Vector3.Dot(Player.instance.transform.position - door.transform.position, Vector3.forward) > 0f;
            if (playerIsInMainHallway) {
                blackRoomRoot.SetActive(false);
            }
        }
    }
}