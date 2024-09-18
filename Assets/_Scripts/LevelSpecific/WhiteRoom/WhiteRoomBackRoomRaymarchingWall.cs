using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

public class WhiteRoomBackRoomRaymarchingWall : MonoBehaviour {
    bool isActive = false;
    public DimensionObject backRoomDimensionWallLeft;
    public DimensionObject backRoomDimensionWallRight;
    public DimensionObject backRoomCullEverythingWallLeft;
    public DimensionObject backRoomCullEverythingWallRight;
    public Renderer backRoomMiddleGlassDimensionWall;

    void Update() {
        bool isActiveNow = gameObject.IsInActiveScene();

        if (isActive != isActiveNow) {
            backRoomDimensionWallLeft.SwitchVisibilityState(isActiveNow ? VisibilityState.PartiallyVisible : VisibilityState.Invisible);
            backRoomDimensionWallRight.SwitchVisibilityState(isActiveNow ? VisibilityState.PartiallyVisible : VisibilityState.Invisible);
            backRoomCullEverythingWallLeft.SwitchVisibilityState(isActiveNow ? VisibilityState.PartiallyInvisible : VisibilityState.Invisible);
            backRoomCullEverythingWallRight.SwitchVisibilityState(isActiveNow ? VisibilityState.PartiallyInvisible : VisibilityState.Invisible);
            backRoomMiddleGlassDimensionWall.enabled = isActiveNow;
            isActive = isActiveNow;
        }
    }
}
