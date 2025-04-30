using PortalMechanics;
using Saving;
using System;
using SuperspectiveUtils;

namespace LevelSpecific.Fork {
    public class ToggleFakeWhiteRoomPortal : SuperspectiveObject<ToggleFakeWhiteRoomPortal, ToggleFakeWhiteRoomPortal.ToggleFakeWhiteRoomPortalSave> {
        public Portal realWhiteRoomPortal;
        public Portal fakeWhiteRoomPortal;
        BladeEdgeDetection edgeDetection;

        private bool bothPortalsDisabled = true;
        bool edgesAreWhite = false;

        protected override void Start() {
            base.Start();
            edgeDetection = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            UpdatePortals();
        }

        void Update() {
            edgesAreWhite = edgeDetection.EdgesAreWhite();
            UpdatePortals();
        }
        
        public void SetBothPortalsDisabled(bool value) {
            bothPortalsDisabled = value;
            UpdatePortals();
        }

        void UpdatePortals() {
            if (bothPortalsDisabled) {
                realWhiteRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                fakeWhiteRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                return;
            }
            if (edgesAreWhite) {
                realWhiteRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                fakeWhiteRoomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
            }
            else {
                realWhiteRoomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
                fakeWhiteRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
            }
        }

#region Saving

        public override void LoadSave(ToggleFakeWhiteRoomPortalSave save) {
            UpdatePortals();
        }

        public override string ID => "ToggleFakeWhiteRoomPortal";

        [Serializable]
        public class ToggleFakeWhiteRoomPortalSave : SaveObject<ToggleFakeWhiteRoomPortal> {
            public ToggleFakeWhiteRoomPortalSave(ToggleFakeWhiteRoomPortal toggle) : base(toggle) { }
        }
#endregion
    }
}
