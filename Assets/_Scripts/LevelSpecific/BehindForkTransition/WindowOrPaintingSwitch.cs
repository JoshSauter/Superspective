using System;
using System.Collections;
using System.Collections.Generic;
using PowerTrailMechanics;
using UnityEngine;

namespace LevelSpecific.BehindForkTransition {
    public class WindowOrPaintingSwitch : MonoBehaviour {
        public GameObject window;
        public GameObject painting;
        public PowerTrail powerTrail;

        bool paintingActive = false;

        void Update() {
            if (paintingActive != powerTrail.IsFullyPowered) {
                paintingActive = powerTrail.IsFullyPowered;
                window.SetActive(!paintingActive);
                painting.SetActive(paintingActive);
            }
        }
    }
}
