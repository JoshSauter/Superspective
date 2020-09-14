using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomFakePortalRendering : MonoBehaviour {
		public PillarDimensionObject fakePortalSides;
		MeshRenderer thisRenderer;

		void Start() {
			thisRenderer = GetComponent<MeshRenderer>();
			fakePortalSides.OnStateChange += OnVisibilityStateChange;
		}

		void OnVisibilityStateChange(VisibilityState unused) {
			bool allVisible = fakePortalSides.visibilityState == VisibilityState.visible;
			thisRenderer.enabled = allVisible;
		}
	}
}