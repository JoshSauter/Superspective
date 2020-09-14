using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoomPuzzle2Solved : MonoBehaviour {
        public PowerTrail powerTrail;
        CubeReceptacle receptacle;

        void Start() {
            receptacle = GetComponent<CubeReceptacle>();

            receptacle.OnCubeHoldEndSimple += () => powerTrail.powerIsOn = true;
            receptacle.OnCubeReleaseStartSimple += () => powerTrail.powerIsOn = false;
        }
    }
}