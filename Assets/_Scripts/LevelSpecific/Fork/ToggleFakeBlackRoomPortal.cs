using PortalMechanics;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;

namespace LevelSpecific.Fork {
    public class ToggleFakeBlackRoomPortal : SaveableObject<ToggleFakeBlackRoomPortal, ToggleFakeBlackRoomPortal.ToggleFakeBlackRoomPortalSave>, CustomAudioJob {
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
        
        public void UpdateAudioJob(AudioManager.AudioJob job) {
            const float timeToFadeVolume = 2f;
            job.audio.loop = playerIsInFakeBlackRoom;
            if (!job.audio.loop) {
                job.audio.volume -= Time.deltaTime / timeToFadeVolume;
            }
            else {
                job.audio.volume += Time.deltaTime / timeToFadeVolume;
            }
            job.audio.panStereo = Mathf.Sin(Time.time);
        }

        #region Saving
        public override string ID => "ToggleFakeBlackRoomPortal";

        [Serializable]
        public class ToggleFakeBlackRoomPortalSave : SerializableSaveObject<ToggleFakeBlackRoomPortal> {
            bool edgesAreBlack;
            bool playerIsInFakeBlackRoom;

            public ToggleFakeBlackRoomPortalSave(ToggleFakeBlackRoomPortal toggle) : base(toggle) {
                this.edgesAreBlack = toggle.edgesAreBlack;
                this.playerIsInFakeBlackRoom = toggle.playerIsInFakeBlackRoom;
            }

            public override void LoadSave(ToggleFakeBlackRoomPortal toggle) {
                toggle.edgesAreBlack = this.edgesAreBlack;
                toggle.playerIsInFakeBlackRoom = this.playerIsInFakeBlackRoom;
            }
        }
        #endregion
    }

}