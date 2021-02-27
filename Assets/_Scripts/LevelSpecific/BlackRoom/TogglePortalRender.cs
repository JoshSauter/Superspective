using System.Collections;
using PortalMechanics;
using MagicTriggerMechanics;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	public class TogglePortalRender : SaveableObject<TogglePortalRender, TogglePortalRender.TogglePortalRenderSave> {
		bool initialized = false;
		public DoorOpenClose enableDoor;
		public MagicTrigger disableTrigger;
		Portal portal;

		protected override void Start() {
			base.Start();
			StartCoroutine(Initialize());
		}

		IEnumerator Initialize() {
			while (portal == null || portal.otherPortal == null) {
				portal = GetComponent<Portal>();
				yield return null;
			}

			if (!initialized) {
				initialized = true;
				PausePortalRendering();
			}
			enableDoor.OnDoorOpenStart += ResumePortalRendering;
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
		public override string ID => "TogglePortalRender";

		[Serializable]
		public class TogglePortalRenderSave : SerializableSaveObject<TogglePortalRender> {
			bool initialized;

			public TogglePortalRenderSave(TogglePortalRender toggle) : base(toggle) {
				this.initialized = toggle.initialized;
			}

			public override void LoadSave(TogglePortalRender toggle) {
				toggle.initialized = this.initialized;
			}
		}
		#endregion
	}
}