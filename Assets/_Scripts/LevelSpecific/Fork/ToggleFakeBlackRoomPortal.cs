using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeBlackRoomPortal : SaveableObject<ToggleFakeBlackRoomPortal, ToggleFakeBlackRoomPortal.ToggleFakeBlackRoomPortalSave> {
        public Portal realBlackRoomPortal;
        public Portal fakeBlackRoomPortal;
        BladeEdgeDetection edgeDetection;

        bool edgesAreBlack = true;

        protected override void Start() {
            base.Start();
            edgeDetection = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
        }

        void Update() {
            switch (edgeDetection.edgeColorMode) {
                case BladeEdgeDetection.EdgeColorMode.SimpleColor:
                    edgesAreBlack = edgeDetection.edgeColor.grayscale == 0;
                    break;
                case BladeEdgeDetection.EdgeColorMode.Gradient:
                    edgesAreBlack = edgeDetection.edgeColorGradient.Evaluate(0.5f).grayscale == 0;
                    break;
                case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
                    edgesAreBlack = false;
                    break;
            }
            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
        }

        #region Saving
        public override string ID => "ToggleFakeBlackRoomPortal";

        [Serializable]
        public class ToggleFakeBlackRoomPortalSave : SerializableSaveObject<ToggleFakeBlackRoomPortal> {
            bool edgesAreBlack;

            public ToggleFakeBlackRoomPortalSave(ToggleFakeBlackRoomPortal toggle) : base(toggle) {
                this.edgesAreBlack = toggle.edgesAreBlack;
            }

            public override void LoadSave(ToggleFakeBlackRoomPortal toggle) {
                toggle.edgesAreBlack = this.edgesAreBlack;
            }
        }
        #endregion
    }

}