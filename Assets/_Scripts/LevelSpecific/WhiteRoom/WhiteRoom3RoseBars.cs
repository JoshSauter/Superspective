using Audio;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoom3RoseBars : SaveableObject<WhiteRoom3RoseBars, WhiteRoom3RoseBars.WhiteRoom3RoseBarsSave>, AudioJobOnGameObject {
        public PowerTrail powerTrail;
        public Button powerButton;
        public GameObject invisibleWall;
        public GameObject[] bars;

        bool barsWereUpLastFrame = true;
        public bool barsAreUp = true;

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

        protected override void Start() {
            base.Start();
            powerButton.OnButtonPressBegin += (ctx) => powerTrail.powerIsOn = true;
            powerButton.OnButtonUnpressFinish += (ctx) => powerTrail.powerIsOn = false;

            powerTrail.OnPowerFinish += () => barsAreUp = false;
            powerTrail.OnDepowerBegin += () => barsAreUp = true;
        }

        void Update() {
            if (!barsAreUp && barsWereUpLastFrame) {
                AudioManager.instance.PlayOnGameObject(AudioName.MetalCreak, ID, this);
            }

            invisibleWall.SetActive(barsAreUp);
            foreach (var bar in bars) {
                Vector3 barPos = bar.transform.localPosition;
                Vector3 targetPos = new Vector3(barPos.x, barPos.y, barsAreUp ? 0f : 11.1f);
                bar.transform.localPosition = Vector3.Lerp(barPos, targetPos, Time.deltaTime * 3f);
            }

            barsWereUpLastFrame = barsAreUp;
        }

        #region Saving
        public override string ID => "WhiteRoom3RoseBars";

        [Serializable]
        public class WhiteRoom3RoseBarsSave : SerializableSaveObject<WhiteRoom3RoseBars> {
            bool barsWereUpLastFrame;
            bool barsAreUp;
            bool invisibleWallActive;
            List<SerializableVector3> barPositions;

            public WhiteRoom3RoseBarsSave(WhiteRoom3RoseBars roseBars) : base(roseBars) {
                this.barsWereUpLastFrame = roseBars.barsWereUpLastFrame;
                this.barsAreUp = roseBars.barsAreUp;
                this.invisibleWallActive = roseBars.invisibleWall.activeSelf;
                this.barPositions = roseBars.bars.Select<GameObject, SerializableVector3>(b => b.transform.position).ToList();
            }

            public override void LoadSave(WhiteRoom3RoseBars roseBars) {
                roseBars.barsWereUpLastFrame = this.barsWereUpLastFrame;
                roseBars.barsAreUp = this.barsAreUp;
                roseBars.invisibleWall.SetActive(this.invisibleWallActive);
                for (int i = 0; i < roseBars.bars.Length; i++) {
                    roseBars.bars[i].transform.position = this.barPositions[i];
                }
            }
        }
        #endregion
    }
}