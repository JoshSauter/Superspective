using PortalMechanics;
using PowerTrailMechanics;
using System.Collections;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class EnableEDChangePortal : MonoBehaviour {
        public PowerTrail powerTrail;
        Portal portal;

        IEnumerator Start() {
            portal = GetComponent<Portal>();
            powerTrail.pwr.OnPowerFinish += Enable;
            powerTrail.pwr.OnDepowerBegin += Disable;

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