using System.Collections;
using PortalMechanics;
using MagicTriggerMechanics;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	// TODO: Replace with MagicTrigger
	public class TogglePortalRender : SuperspectiveObject<TogglePortalRender, TogglePortalRender.TogglePortalRenderSave> {
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
			disableTrigger.OnMagicTriggerStayOneTime += PausePortalRendering;
		}

		void ResumePortalRendering() {
			portal.RenderMode = PortalRenderMode.Normal;
			portal.otherPortal.RenderMode = PortalRenderMode.Normal;
		}

		void PausePortalRendering() {
			portal.RenderMode = PortalRenderMode.Invisible;
			portal.otherPortal.RenderMode = PortalRenderMode.Invisible;
		}

#region Saving

		public override void LoadSave(TogglePortalRenderSave save) {
			initialized = save.initialized;
		}

		public override string ID => "TogglePortalRender";

		[Serializable]
		public class TogglePortalRenderSave : SaveObject<TogglePortalRender> {
			public bool initialized;

			public TogglePortalRenderSave(TogglePortalRender toggle) : base(toggle) {
				this.initialized = toggle.initialized;
			}
		}
#endregion
	}
}