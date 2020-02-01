using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WhiteRoomFakePortalRendering : MonoBehaviour {
	public PillarDimensionObject[] fakePortalSides;
	MeshRenderer thisRenderer;

    void Start() {
		thisRenderer = GetComponent<MeshRenderer>();
        foreach (var portalSide in fakePortalSides) {
			portalSide.OnStateChange += OnVisibilityStateChange;
		}
    }

	void OnVisibilityStateChange(VisibilityState unused) {
		bool allVisible = fakePortalSides.All(x => x.visibilityState == VisibilityState.visible);
		thisRenderer.enabled = allVisible;
	}
}
