using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteRoomPuzzle2Start : MonoBehaviour {
    public PowerTrail[] powerTrailsToObelisk;
    CubeReceptacle receptacle;

    void Start() {
        receptacle = GetComponent<CubeReceptacle>();
        receptacle.OnCubeHoldEndSimple += () => SetPowerTrailState(true);
        receptacle.OnCubeReleaseEndSimple += () => SetPowerTrailState(false);
    }

    void SetPowerTrailState(bool powered) {
        foreach (var powerTrail in powerTrailsToObelisk) {
            powerTrail.powerIsOn = powered;
        }
    }
}
