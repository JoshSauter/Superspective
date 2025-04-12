using Audio;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using Interactables;
using UnityEngine;

namespace LevelSpecific.WhiteRoom {
    public class RoseRoomPaintingBars : SuperspectiveObject<RoseRoomPaintingBars, RoseRoomPaintingBars.RoseRoomPaintingBarsSave>, AudioJobOnGameObject {
        public PowerTrail powerTrail;
        public Button powerButton;
        public GameObject invisibleWall;
        public GameObject[] bars;

        bool barsWereUpLastFrame = true;
        public bool barsAreUp = true;

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

        protected override void Start() {
            base.Start();
            powerButton.OnButtonPressBegin += (ctx) => powerTrail.pwr.PowerIsOn = true;
            powerButton.OnButtonUnpressFinish += (ctx) => powerTrail.pwr.PowerIsOn = false;

            powerTrail.pwr.OnPowerFinish += () => barsAreUp = false;
            powerTrail.pwr.OnDepowerBegin += () => barsAreUp = true;
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

        public override void LoadSave(RoseRoomPaintingBarsSave save) {
            barsWereUpLastFrame = save.barsWereUpLastFrame;
            barsAreUp = save.barsAreUp;
            invisibleWall.SetActive(save.invisibleWallActive);
            for (int i = 0; i < bars.Length; i++) {
                bars[i].transform.position = save.barPositions[i];
            }
        }

        public override string ID => "WhiteRoom3RoseBars";

        [Serializable]
        public class RoseRoomPaintingBarsSave : SaveObject<RoseRoomPaintingBars> {
            public List<SerializableVector3> barPositions;
            public bool barsWereUpLastFrame;
            public bool barsAreUp;
            public bool invisibleWallActive;

            public RoseRoomPaintingBarsSave(RoseRoomPaintingBars roseRoomPaintingBars) : base(roseRoomPaintingBars) {
                this.barsWereUpLastFrame = roseRoomPaintingBars.barsWereUpLastFrame;
                this.barsAreUp = roseRoomPaintingBars.barsAreUp;
                this.invisibleWallActive = roseRoomPaintingBars.invisibleWall.activeSelf;
                this.barPositions = roseRoomPaintingBars.bars.Select<GameObject, SerializableVector3>(b => b.transform.position).ToList();
            }
        }
#endregion
    }
}