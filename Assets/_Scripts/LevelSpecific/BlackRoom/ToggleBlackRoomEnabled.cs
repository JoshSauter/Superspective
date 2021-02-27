using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
    public class ToggleBlackRoomEnabled : SaveableObject<ToggleBlackRoomEnabled, ToggleBlackRoomEnabled.ToggleBlackRoomEnabledSave> {
        public GameObject blackRoomRoot;
        DoorOpenClose door;

        protected override void Start() {
	        base.Start();
            door = GetComponent<DoorOpenClose>();
            door.OnDoorOpenStart += EnableBlackRoom;
            door.OnDoorCloseEnd += DisableBlackRoomIfInMainHallway;
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
		public override string ID => "ToggleBlackRoomEnabled";

		[Serializable]
		public class ToggleBlackRoomEnabledSave : SerializableSaveObject<ToggleBlackRoomEnabled> {
			bool blackRoomEnabled;

			public ToggleBlackRoomEnabledSave(ToggleBlackRoomEnabled toggle) : base(toggle) {
				this.blackRoomEnabled = toggle.blackRoomRoot.activeSelf;
			}

			public override void LoadSave(ToggleBlackRoomEnabled toggle) {
				toggle.blackRoomRoot.SetActive(this.blackRoomEnabled);
			}
		}
		#endregion
	}
}