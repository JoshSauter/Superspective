using PortalMechanics;
using PowerTrailMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class PortalToBlackHallway : MonoBehaviour {
        Portal portal;
        public PowerTrail powerTrail;
        public Collider[] blackHallwayLoopTeleporters;

        IEnumerator Start() {
            portal = GetComponent<Portal>();
            yield return new WaitUntil(() => portal.otherPortal != null);
            portal.changeCameraEdgeDetection = false;
            portal.otherPortal.changeCameraEdgeDetection = false;

            powerTrail.OnPowerFinish += () => HandlePowerTrail(true);
            powerTrail.OnDepowerBegin += () => HandlePowerTrail(false);

            HandlePowerTrail(powerTrail.fullyPowered);
        }

        void HandlePowerTrail(bool poweredNow) {
            SetEdgeColors(poweredNow);

            foreach (var teleporter in blackHallwayLoopTeleporters) {
                teleporter.gameObject.SetActive(!poweredNow);
            }
        }

        void SetEdgeColors(bool on) {
            portal.changeCameraEdgeDetection = on;
            portal.otherPortal.changeCameraEdgeDetection = on;
        }
    }
}