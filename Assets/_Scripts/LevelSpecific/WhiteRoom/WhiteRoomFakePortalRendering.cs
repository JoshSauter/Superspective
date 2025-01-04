using Saving;
using System;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomFakePortalRendering : SuperspectiveObject<WhiteRoomFakePortalRendering, WhiteRoomFakePortalRendering.WhiteRoomFakePortalRenderingSave> {
		public PillarDimensionObject fakePortalSides;
		public MeshRenderer thisRenderer;

		protected override void Start() {
			base.Start();
			fakePortalSides.OnStateChangeSimple += OnVisibilityStateChange;
		}

		void OnVisibilityStateChange() {
			bool allVisible = fakePortalSides.visibilityState == VisibilityState.Visible;
			thisRenderer.enabled = allVisible;
        }

#region Saving

		public override void LoadSave(WhiteRoomFakePortalRenderingSave save) {
			thisRenderer.enabled = save.rendererEnabled;
		}

		public override string ID => "WhiteRoomFakePortalRendering";

        [Serializable]
        public class WhiteRoomFakePortalRenderingSave : SaveObject<WhiteRoomFakePortalRendering> {
            public bool rendererEnabled;

            public WhiteRoomFakePortalRenderingSave(WhiteRoomFakePortalRendering script) : base(script) {
                this.rendererEnabled = script.thisRenderer.enabled;
            }
        }
#endregion
    }
}