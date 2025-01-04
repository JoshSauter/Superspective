using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Audio;
using Saving;
using System;
using SerializableClasses;
using System.Linq;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoom3ExitBars : SuperspectiveObject<WhiteRoom3ExitBars, WhiteRoom3ExitBars.WhiteRoom3ExitBarsSave>, AudioJobOnGameObject {
        public CubeReceptacle[] puzzleReceptacles;
        public PowerTrail[] powerTrails;
        public Transform[] bars;
        public GameObject invisibleWall;

        public GameObject barRoot;

        public int numSolved = 0;
        private bool cheatSolved = false;
        bool wasSolvedLastFrame = false;
        bool Solved => numSolved == powerTrails.Length || cheatSolved;

        protected override void Start() {
            base.Start();
            foreach (var powerTrail in powerTrails) {
                powerTrail.pwr.OnPowerFinish += () => numSolved++;
                powerTrail.pwr.OnDepowerBegin += () => numSolved--;
            }
        }

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => barRoot.transform;

        void Update() {
            if (this.InstaSolvePuzzle()) {
                cheatSolved = !cheatSolved;
            }
            
            if (Solved && !wasSolvedLastFrame) {
                AudioManager.instance.Play(AudioName.CorrectAnswer);
                AudioManager.instance.PlayOnGameObject(AudioName.MetalCreak, ID, this);

                foreach (var receptacle in puzzleReceptacles) {
                    receptacle.lockCubeInPlace = true;
                    receptacle.makesCubeIrreplaceable = true;
                }
            }

            invisibleWall.SetActive(!Solved);
            foreach (var bar in bars) {
                Vector3 barPos = bar.transform.localPosition;
                Vector3 targetPos = new Vector3(barPos.x, barPos.y, Solved ? 7.1f : 0);
                bar.transform.localPosition = Vector3.Lerp(barPos, targetPos, Time.deltaTime * 3f);
            }

            wasSolvedLastFrame = Solved;
        }

#region Saving

        public override void LoadSave(WhiteRoom3ExitBarsSave save) {
            numSolved = save.numSolved;
            wasSolvedLastFrame = save.wasSolvedLastFrame;
            for (int i = 0; i < bars.Length; i++) {
                bars[i].position = save.barPositions[i];
            }
        }

        public override string ID => "WhiteRoom3ExitBars";

        [Serializable]
        public class WhiteRoom3ExitBarsSave : SaveObject<WhiteRoom3ExitBars> {
            public List<SerializableVector3> barPositions;
            public int numSolved;
            public bool wasSolvedLastFrame;

            public WhiteRoom3ExitBarsSave(WhiteRoom3ExitBars exitBars) : base(exitBars) {
                this.numSolved = exitBars.numSolved;
                this.wasSolvedLastFrame = exitBars.wasSolvedLastFrame;
                this.barPositions = exitBars.bars.Select<Transform, SerializableVector3>(b => b.position).ToList();
            }
        }
#endregion
    }
}