using UnityEngine;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
    public class ToggleBlackRoomEnabled : SuperspectiveObject<ToggleBlackRoomEnabled, ToggleBlackRoomEnabled.ToggleBlackRoomEnabledSave> {
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

	    public override void LoadSave(ToggleBlackRoomEnabledSave save) {
		    blackRoomRoot.SetActive(save.blackRoomEnabled);
	    }

	    public override string ID => "ToggleBlackRoomEnabled";

		[Serializable]
		public class ToggleBlackRoomEnabledSave : SaveObject<ToggleBlackRoomEnabled> {
			public bool blackRoomEnabled;

			public ToggleBlackRoomEnabledSave(ToggleBlackRoomEnabled toggle) : base(toggle) {
				this.blackRoomEnabled = toggle.blackRoomRoot.activeSelf;
			}
		}
#endregion
	}
}