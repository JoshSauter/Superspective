using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Audio;
using Saving;
using System;
using SerializableClasses;
using System.Linq;

namespace LevelSpecific.WhiteRoom {
    public class WhiteRoom3Exit : SuperspectiveObject<WhiteRoom3Exit, WhiteRoom3Exit.WhiteRoom3ExitSave> {
        public CubeReceptacle[] puzzleReceptacles;
        public PowerTrail[] powerTrails;

        public ParticleSystem obeliskConnectingParticles;
        public Transform obeliskConnectingBackdrop;

        public int numSolved = 0;
        private bool cheatSolved = false;
        bool wasSolvedLastFrame = false;
        bool Solved => numSolved == powerTrails.Length || cheatSolved;

        private const float PARTICLE_DISTANCE = 22f;
        private float ParticleAnimateTime => PARTICLE_DISTANCE / obeliskConnectingParticles.main.startSpeed.constant;

        protected override void Start() {
            base.Start();
            foreach (var powerTrail in powerTrails) {
                powerTrail.pwr.OnPowerFinish += () => numSolved++;
                powerTrail.pwr.OnDepowerBegin += () => numSolved--;
            }
        }

        void Update() {
            if (this.InstaSolvePuzzle()) {
                cheatSolved = !cheatSolved;
            }
            
            if (Solved && !wasSolvedLastFrame) {
                AudioManager.instance.Play(AudioName.CorrectAnswer);

                foreach (var receptacle in puzzleReceptacles) {
                    receptacle.lockCubeInPlace = true;
                    receptacle.makesCubeIrreplaceable = true;
                }
            }
            
            if (obeliskConnectingParticles.isPlaying) {
                float t = Mathf.Clamp01(obeliskConnectingParticles.totalTime / ParticleAnimateTime);
                obeliskConnectingBackdrop.localScale = new Vector3(1f, t, 1f);
            }
            else {
                obeliskConnectingBackdrop.localScale = new Vector3(1f, 0, 1f);
            }

            wasSolvedLastFrame = Solved;
        }

#region Saving

        public override void LoadSave(WhiteRoom3ExitSave save) {
            numSolved = save.numSolved;
            wasSolvedLastFrame = save.wasSolvedLastFrame;
            obeliskConnectingBackdrop.localScale = save.obeliskConnectorBackgroundScale;
        }

        public override string ID => "WhiteRoom3Exit";

        [Serializable]
        public class WhiteRoom3ExitSave : SaveObject<WhiteRoom3Exit> {
            public SerializableVector3 obeliskConnectorBackgroundScale;
            public int numSolved;
            public bool wasSolvedLastFrame;

            public WhiteRoom3ExitSave(WhiteRoom3Exit exit) : base(exit) {
                this.numSolved = exit.numSolved;
                this.wasSolvedLastFrame = exit.wasSolvedLastFrame;
                this.obeliskConnectorBackgroundScale = exit.obeliskConnectingBackdrop.localScale;
            }
        }
#endregion
    }
}