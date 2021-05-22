using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeWhiteRoomPortal : SaveableObject<ToggleFakeWhiteRoomPortal, ToggleFakeWhiteRoomPortal.ToggleFakeWhiteRoomPortalSave> {
        public Portal realWhiteRoomPortal;
        public Portal fakeWhiteRoomPortal;
        BladeEdgeDetection edgeDetection;

        bool edgesAreWhite = false;

        protected override void Start() {
            base.Start();
            edgeDetection = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            realWhiteRoomPortal.gameObject.SetActive(!edgesAreWhite);
            fakeWhiteRoomPortal.gameObject.SetActive(edgesAreWhite);
        }

        void Update() {
            switch (edgeDetection.edgeColorMode) {
                case BladeEdgeDetection.EdgeColorMode.SimpleColor:
                    edgesAreWhite = edgeDetection.edgeColor.grayscale > .5f;
                    break;
                case BladeEdgeDetection.EdgeColorMode.Gradient:
                    edgesAreWhite = edgeDetection.edgeColorGradient.Evaluate(0).grayscale > .5f;
                    break;
                case BladeEdgeDetection.EdgeColorMode.ColorRampTexture:
                    edgesAreWhite = false;
                    break;
            }
            realWhiteRoomPortal.gameObject.SetActive(!edgesAreWhite);
            fakeWhiteRoomPortal.gameObject.SetActive(edgesAreWhite);
        }

        #region Saving
        public override string ID => "ToggleFakeWhiteRoomPortal";

        [Serializable]
        public class ToggleFakeWhiteRoomPortalSave : SerializableSaveObject<ToggleFakeWhiteRoomPortal> {
            bool edgesAreWhite;

            public ToggleFakeWhiteRoomPortalSave(ToggleFakeWhiteRoomPortal toggle) : base(toggle) {
                this.edgesAreWhite = toggle.edgesAreWhite;
            }

            public override void LoadSave(ToggleFakeWhiteRoomPortal toggle) {
                toggle.edgesAreWhite = this.edgesAreWhite;
            }
        }
        #endregion
    }

}