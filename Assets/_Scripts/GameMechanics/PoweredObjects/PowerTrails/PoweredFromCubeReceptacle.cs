using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerTrailMechanics {
    public class PoweredFromCubeReceptacle : MonoBehaviour {
        public CubeReceptacle receptacle;
        PowerTrail powerTrail;

        void Start() {
            powerTrail = GetComponent<PowerTrail>();
            receptacle.OnCubeHoldEndSimple += () => powerTrail.powerIsOn = true;
            receptacle.OnCubeReleaseStartSimple += () => powerTrail.powerIsOn = false;
        }
    }
}