using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
    public class ToggleBlackRoomEnabled : MonoBehaviour, SaveableObject {
        public GameObject blackRoomRoot;
        DoorOpenClose door;

        void Start() {
            door = GetComponent<DoorOpenClose>();
            door.OnDoorOpenStart += () => EnableBlackRoom();
            door.OnDoorCloseEnd += () => DisableBlackRoomIfInMainHallway();
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

		#region Saving
		public bool SkipSave { get; set; }

		public string ID => "ToggleBlackRoomEnabled";

		[Serializable]
		class ToggleBlackRoomEnabledSave {
			bool blackRoomEnabled;

			public ToggleBlackRoomEnabledSave(ToggleBlackRoomEnabled toggle) {
				this.blackRoomEnabled = toggle.blackRoomRoot.activeSelf;
			}

			public void LoadSave(ToggleBlackRoomEnabled toggle) {
				toggle.blackRoomRoot.SetActive(this.blackRoomEnabled);
			}
		}

		public object GetSaveObject() {
			return new ToggleBlackRoomEnabledSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			ToggleBlackRoomEnabledSave save = savedObject as ToggleBlackRoomEnabledSave;

			save.LoadSave(this);
		}
		#endregion
	}
}