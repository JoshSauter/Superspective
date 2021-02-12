using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Audio;
using Saving;
using System;
using SerializableClasses;
using System.Linq;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoom3ExitBars : SaveableObject<WhiteRoom3ExitBars, WhiteRoom3ExitBars.WhiteRoom3ExitBarsSave> {
        public PowerTrail[] powerTrails;
        public Transform[] bars;
        public GameObject invisibleWall;

        public GameObject barRoot;

        public int numSolved = 0;
        bool wasSolvedLastFrame = false;
        bool solved => numSolved == powerTrails.Length;

        protected override void Start() {
            base.Start();
            foreach (var powerTrail in powerTrails) {
                powerTrail.OnPowerFinish += () => numSolved++;
                powerTrail.OnDepowerBegin += () => numSolved--;
            }
        }

        void Update() {
            if (solved && !wasSolvedLastFrame) {
                AudioManager.instance.PlayOnGameObject(AudioName.MetalCreak, ID, barRoot);
            }

            invisibleWall.SetActive(!solved);
            foreach (var bar in bars) {
                Vector3 barPos = bar.transform.localPosition;
                Vector3 targetPos = new Vector3(barPos.x, barPos.y, solved ? 7.1f : 0);
                bar.transform.localPosition = Vector3.Lerp(barPos, targetPos, Time.deltaTime * 3f);
            }

            wasSolvedLastFrame = solved;
        }

        #region Saving
        public override string ID => "WhiteRoom3ExitBars";

        [Serializable]
        public class WhiteRoom3ExitBarsSave : SerializableSaveObject<WhiteRoom3ExitBars> {
            int numSolved;
            bool wasSolvedLastFrame;
            List<SerializableVector3> barPositions;

            public WhiteRoom3ExitBarsSave(WhiteRoom3ExitBars exitBars) {
                this.numSolved = exitBars.numSolved;
                this.wasSolvedLastFrame = exitBars.wasSolvedLastFrame;
                this.barPositions = exitBars.bars.Select<Transform, SerializableVector3>(b => b.position).ToList();
            }

            public override void LoadSave(WhiteRoom3ExitBars exitBars) {
                exitBars.numSolved = this.numSolved;
                exitBars.wasSolvedLastFrame = this.wasSolvedLastFrame;
                for (int i = 0; i < exitBars.bars.Length; i++) {
                    exitBars.bars[i].position = this.barPositions[i];
				}
            }
        }
        #endregion
    }
}