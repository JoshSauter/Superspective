using Saving;
using System;
using UnityEngine;
using NaughtyAttributes;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomFakePortalRendering : MonoBehaviour, SaveableObject {
		public PillarDimensionObject fakePortalSides;
		public MeshRenderer thisRenderer;

		void Start() {
			fakePortalSides.OnStateChange += OnVisibilityStateChange;
		}

		void OnVisibilityStateChange(VisibilityState unused) {
			bool allVisible = fakePortalSides.visibilityState == VisibilityState.visible;
			thisRenderer.enabled = allVisible;
        }

        #region Saving
        public bool SkipSave { get; set; }

        public string ID => "WhiteRoomFakePortalRendering";

        [Serializable]
        class WhiteRoomFakePortalRenderingSave {
            bool rendererEnabled;

            public WhiteRoomFakePortalRenderingSave(WhiteRoomFakePortalRendering script) {
                this.rendererEnabled = script.thisRenderer.enabled;
            }

            public void LoadSave(WhiteRoomFakePortalRendering script) {
                script.thisRenderer.enabled = this.rendererEnabled;
            }
        }

        public object GetSaveObject() {
            return new WhiteRoomFakePortalRenderingSave(this);
        }

        public void LoadFromSavedObject(object savedObject) {
            WhiteRoomFakePortalRenderingSave save = savedObject as WhiteRoomFakePortalRenderingSave;

            save.LoadSave(this);
        }
        #endregion
    }
}