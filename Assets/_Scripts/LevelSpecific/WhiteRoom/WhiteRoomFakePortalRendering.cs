using Saving;
using System;
using UnityEngine;
using NaughtyAttributes;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomFakePortalRendering : SaveableObject<WhiteRoomFakePortalRendering, WhiteRoomFakePortalRendering.WhiteRoomFakePortalRenderingSave> {
		public PillarDimensionObject fakePortalSides;
		public MeshRenderer thisRenderer;

		protected override void Start() {
			base.Start();
			fakePortalSides.OnStateChangeSimple += OnVisibilityStateChange;
		}

		void OnVisibilityStateChange() {
			bool allVisible = fakePortalSides.visibilityState == VisibilityState.visible;
			thisRenderer.enabled = allVisible;
        }

        #region Saving
        public override string ID => "WhiteRoomFakePortalRendering";

        [Serializable]
        public class WhiteRoomFakePortalRenderingSave : SerializableSaveObject<WhiteRoomFakePortalRendering> {
            bool rendererEnabled;

            public WhiteRoomFakePortalRenderingSave(WhiteRoomFakePortalRendering script) : base(script) {
                this.rendererEnabled = script.thisRenderer.enabled;
            }

            public override void LoadSave(WhiteRoomFakePortalRendering script) {
                script.thisRenderer.enabled = this.rendererEnabled;
            }
        }
        #endregion
    }
}