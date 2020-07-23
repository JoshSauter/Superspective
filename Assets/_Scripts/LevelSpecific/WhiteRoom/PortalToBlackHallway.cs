using PortalMechanics;
using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalToBlackHallway : MonoBehaviour {
    Portal portal;
    public PowerTrail powerTrail;
    public Collider[] blackHallwayLoopTeleporters;

    IEnumerator Start() {
        yield return null;
        portal = GetComponent<Portal>();
        portal.changeCameraEdgeDetection = false;
        portal.otherPortal.changeCameraEdgeDetection = false;

        powerTrail.OnPowerFinish += () => HandlePowerTrail(true);
        powerTrail.OnDepowerBegin += () => HandlePowerTrail(false);
    }

    void HandlePowerTrail(bool powered) {
        SetEdgeColors(powered);

        foreach (var teleporter in blackHallwayLoopTeleporters) {
            teleporter.gameObject.SetActive(!powered);
        }
    }

    void SetEdgeColors(bool on) {
        portal.changeCameraEdgeDetection = on;
        portal.otherPortal.changeCameraEdgeDetection = on;
    }
}
