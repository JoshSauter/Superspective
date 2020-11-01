using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeBlackRoomPortal : MonoBehaviour, SaveableObject {
        public Portal realBlackRoomPortal;
        public Portal fakeBlackRoomPortal;
        BladeEdgeDetection edgeDetection;

        bool edgesAreBlack = true;

        void Start() {
            edgeDetection = EpitaphScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
        }

        private void Update() {
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

        #region Saving
        public bool SkipSave { get; set; }

        public string ID => "ToggleFakeBlackRoomPortal";

        [Serializable]
        class ToggleFakeBlackRoomPortalSave {
            bool edgesAreBlack;

            public ToggleFakeBlackRoomPortalSave(ToggleFakeBlackRoomPortal toggle) {
                this.edgesAreBlack = toggle.edgesAreBlack;
            }

            public void LoadSave(ToggleFakeBlackRoomPortal toggle) {
                toggle.edgesAreBlack = this.edgesAreBlack;
            }
        }

        public object GetSaveObject() {
            return new ToggleFakeBlackRoomPortalSave(this);
        }

        public void LoadFromSavedObject(object savedObject) {
            ToggleFakeBlackRoomPortalSave save = savedObject as ToggleFakeBlackRoomPortalSave;

            save.LoadSave(this);
        }
        #endregion
    }

}