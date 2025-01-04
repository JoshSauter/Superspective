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

        bool edgesAreBlack = true;
        bool playerIsInFakeBlackRoom = false;

        protected override void Start() {
            base.Start();
            edgeDetection = SuperspectiveScreen.instance.playerCamera.GetComponent<BladeEdgeDetection>();

            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
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
            realBlackRoomPortal.gameObject.SetActive(!edgesAreBlack);
            fakeBlackRoomPortal.gameObject.SetActive(edgesAreBlack);
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

        public override void LoadSave(ToggleFakeBlackRoomPortalSave save) {
            edgesAreBlack = save.edgesAreBlack;
            playerIsInFakeBlackRoom = save.playerIsInFakeBlackRoom;
        }

        public override string ID => "ToggleFakeBlackRoomPortal";

        [Serializable]
        public class ToggleFakeBlackRoomPortalSave : SaveObject<ToggleFakeBlackRoomPortal> {
            public bool edgesAreBlack;
            public bool playerIsInFakeBlackRoom;

            public ToggleFakeBlackRoomPortalSave(ToggleFakeBlackRoomPortal toggle) : base(toggle) {
                this.edgesAreBlack = toggle.edgesAreBlack;
                this.playerIsInFakeBlackRoom = toggle.playerIsInFakeBlackRoom;
            }
        }
#endregion
    }
}
