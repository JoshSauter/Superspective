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
            backRoomDimensionWallLeft.SwitchVisibilityState(isActiveNow ? VisibilityState.partiallyVisible : VisibilityState.invisible);
            backRoomDimensionWallRight.SwitchVisibilityState(isActiveNow ? VisibilityState.partiallyVisible : VisibilityState.invisible);
            backRoomCullEverythingWallLeft.SwitchVisibilityState(isActiveNow ? VisibilityState.partiallyInvisible : VisibilityState.invisible);
            backRoomCullEverythingWallRight.SwitchVisibilityState(isActiveNow ? VisibilityState.partiallyInvisible : VisibilityState.invisible);
            backRoomMiddleGlassDimensionWall.enabled = isActiveNow;
            isActive = isActiveNow;
        }
    }
}
