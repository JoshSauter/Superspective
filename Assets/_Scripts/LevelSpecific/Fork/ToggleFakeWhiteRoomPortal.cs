using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
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
            edgesAreWhite = edgeDetection.EdgesAreWhite();
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