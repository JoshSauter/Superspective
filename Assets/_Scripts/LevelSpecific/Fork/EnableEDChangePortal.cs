using PortalMechanics;
using PowerTrailMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class EnableEDChangePortal : MonoBehaviour {
        public PowerTrail powerTrail;
        Portal portal;

        IEnumerator Start() {
            portal = GetComponent<Portal>();
            powerTrail.OnPowerFinish += Enable;
            powerTrail.OnDepowerBegin += Disable;

            yield return new WaitUntil(() => portal.otherPortal != null);
        }

        void Enable() {
            portal.changeCameraEdgeDetection = true;
            portal.otherPortal.changeCameraEdgeDetection = true;
        }

        void Disable() {
            portal.changeCameraEdgeDetection = false;
            portal.otherPortal.changeCameraEdgeDetection = false;
        }
    }
}