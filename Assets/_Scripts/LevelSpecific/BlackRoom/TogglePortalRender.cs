using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using MagicTriggerMechanics;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class TogglePortalRender : MonoBehaviour, SaveableObject {
		bool initialized = false;
		public DoorOpenClose enableDoor;
		public MagicTrigger disableTrigger;
		Portal portal;

		IEnumerator Start() {
			while (portal == null || portal.otherPortal == null) {
				portal = GetComponent<Portal>();
				yield return null;
			}

			if (!initialized) {
				initialized = true;
				PausePortalRendering();
			}
			enableDoor.OnDoorOpenStart += () => ResumePortalRendering();
			disableTrigger.OnMagicTriggerStayOneTime += ctx => PausePortalRendering();
		}

		void ResumePortalRendering() {
			portal.pauseRenderingAndLogic = false;
			portal.otherPortal.pauseRenderingAndLogic = false;
		}

		void PausePortalRendering() {
			portal.pauseRenderingAndLogic = true;
			portal.otherPortal.pauseRenderingAndLogic = true;
		}

		#region Saving
		public bool SkipSave { get; set; }

		public string ID => "TogglePortalRender";

		[Serializable]
		class TogglePortalRenderSave {
			bool initialized;

			public TogglePortalRenderSave(TogglePortalRender toggle) {
				this.initialized = toggle.initialized;
			}

			public void LoadSave(TogglePortalRender toggle) {
				toggle.initialized = this.initialized;
			}
		}

		public object GetSaveObject() {
			return new TogglePortalRenderSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			TogglePortalRenderSave save = savedObject as TogglePortalRenderSave;

			save.LoadSave(this);
		}
		#endregion
	}
}