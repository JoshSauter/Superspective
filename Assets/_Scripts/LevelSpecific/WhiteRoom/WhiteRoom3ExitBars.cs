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
    public class WhiteRoom3ExitBars : MonoBehaviour, SaveableObject {
        public PowerTrail[] powerTrails;
        public Transform[] bars;
        public GameObject invisibleWall;

        public SoundEffect barsOpenSfx;

        public int numSolved = 0;
        bool wasSolvedLastFrame = false;
        bool solved => numSolved == powerTrails.Length;

        void Start() {
            foreach (var powerTrail in powerTrails) {
                powerTrail.OnPowerFinish += () => numSolved++;
                powerTrail.OnDepowerBegin += () => numSolved--;
            }
        }

        void Update() {
            if (solved && !wasSolvedLastFrame) {
                barsOpenSfx.Play();
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
        public bool SkipSave { get; set; }

        public string ID => "WhiteRoom3ExitBars";

        [Serializable]
        class WhiteRoom3ExitBarsSave {
            int numSolved;
            bool wasSolvedLastFrame;
            List<SerializableVector3> barPositions;

            public WhiteRoom3ExitBarsSave(WhiteRoom3ExitBars exitBars) {
                this.numSolved = exitBars.numSolved;
                this.wasSolvedLastFrame = exitBars.wasSolvedLastFrame;
                this.barPositions = exitBars.bars.Select<Transform, SerializableVector3>(b => b.position).ToList();
            }

            public void LoadSave(WhiteRoom3ExitBars exitBars) {
                exitBars.numSolved = this.numSolved;
                exitBars.wasSolvedLastFrame = this.wasSolvedLastFrame;
                for (int i = 0; i < exitBars.bars.Length; i++) {
                    exitBars.bars[i].position = this.barPositions[i];
				}
            }
        }

        public object GetSaveObject() {
            return new WhiteRoom3ExitBarsSave(this);
        }

        public void LoadFromSavedObject(object savedObject) {
            WhiteRoom3ExitBarsSave save = savedObject as WhiteRoom3ExitBarsSave;

            save.LoadSave(this);
        }
        #endregion
    }
}