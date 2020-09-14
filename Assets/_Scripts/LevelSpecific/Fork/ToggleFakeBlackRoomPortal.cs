using PortalMechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeBlackRoomPortal : MonoBehaviour {
        public Portal realBlackRoomPortal;
        public Portal fakeBlackRoomPortal;
        BladeEdgeDetection edgeDetection;

        void Start() {
            edgeDetection = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();
        }

        private void Update() {
            bool edgesAreBlack = false;
            switch (edgeDetection.edgeColorMode) {
                case BladeEdgeDetection.EdgeColorMode.simpleColor:
                    edgesAreBlack = edgeDetection.edgeColor.grayscale == 0;
                    break;
                case BladeEdgeDetection.EdgeColorMode.gradient:
                    edgesAreBlack = edgeDetection.edgeColorGradient.Evaluate(0.5f).grayscale == 0;
                    break;
                case BladeEdgeDetection.EdgeColorMode.colorRampTexture:
                    edgesAreBlack = false;
                    break;
            }
            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
        }
    }
}