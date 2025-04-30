using PortalMechanics;
using Saving;
using System;
using System.Collections;
using Audio;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeBlackRoomPortal : SuperspectiveObject<ToggleFakeBlackRoomPortal, ToggleFakeBlackRoomPortal.ToggleFakeBlackRoomPortalSave>, CustomAudioJob {
        public Portal realBlackRoomPortal;
        public Portal fakeBlackRoomPortal;
        BladeEdgeDetection edgeDetection;

        private bool bothPortalsDisabled = true;
        bool edgesAreBlack = true;
        bool playerIsInFakeBlackRoom = false;

        protected override void Start() {
            base.Start();
            edgeDetection = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            UpdatePortals();
        }

        protected override void Init() {
            base.Init();

            StartCoroutine(WaitForOtherPortalToBeAvailable());
        }

        IEnumerator WaitForOtherPortalToBeAvailable() {
            yield return new WaitWhile(() => fakeBlackRoomPortal.otherPortal == null);
            
            fakeBlackRoomPortal.OnPortalTeleportSimple += _ => {
                playerIsInFakeBlackRoom = true;
                AudioManager.instance.PlayWithUpdate(AudioName.EmptyVoid_8152358, ID, this, true);
            };
            fakeBlackRoomPortal.otherPortal.OnPortalTeleportSimple += _ => playerIsInFakeBlackRoom = false;
        }

        void Update() {
            edgesAreBlack = edgeDetection.EdgesAreBlack();
            UpdatePortals();
        }

        public void SetBothPortalsDisabled(bool value) {
            bothPortalsDisabled = value;
            UpdatePortals();
        }

        void UpdatePortals() {
            if (bothPortalsDisabled) {
                realBlackRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                fakeBlackRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                return;
            }
            
            if (edgesAreBlack) {
                realBlackRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
                fakeBlackRoomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
            }
            else {
                realBlackRoomPortal.SetPortalModes(PortalRenderMode.Normal, PortalPhysicsMode.Normal);
                fakeBlackRoomPortal.SetPortalModes(PortalRenderMode.Invisible, PortalPhysicsMode.None);
            }
        }
        
        public void UpdateAudioJob(AudioManager.AudioJob job) {
            const float TIME_TO_FADE_VOLUME = 2f;
            job.audio.loop = playerIsInFakeBlackRoom;
            if (!job.audio.loop) {
                job.audio.volume -= Time.deltaTime / TIME_TO_FADE_VOLUME;
            }
            else {
                job.audio.volume += Time.deltaTime / TIME_TO_FADE_VOLUME;
            }
            job.audio.panStereo = Mathf.Sin(Time.time);
        }

#region Saving

        public override void LoadSave(ToggleFakeBlackRoomPortalSave save) { }

        public override string ID => "ToggleFakeBlackRoomPortal";

        [Serializable]
        public class ToggleFakeBlackRoomPortalSave : SaveObject<ToggleFakeBlackRoomPortal> {
            public ToggleFakeBlackRoomPortalSave(ToggleFakeBlackRoomPortal toggle) : base(toggle) { }
        }
#endregion
    }
}
