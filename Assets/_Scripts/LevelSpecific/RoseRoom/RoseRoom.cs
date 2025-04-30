using UnityEngine;
using PowerTrailMechanics;
using Audio;
using Saving;
using System;
using SerializableClasses;
using SuperspectiveAttributes;

namespace LevelSpecific.WhiteRoom {
    public class RoseRoom : SuperspectiveObject<RoseRoom, RoseRoom.RoseRoomSave> {
        public CubeReceptacle[] puzzleReceptacles;
        public PowerTrail[] powerTrails;

        [SaveUnityObject]
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
                foreach (var powerTrail in powerTrails) {
                    powerTrail.pwr.PowerIsOn = cheatSolved;
                }
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
                obeliskConnectingBackdrop.localScale = new Vector3(1f, 0.5f*t, 1f);
            }
            else {
                obeliskConnectingBackdrop.localScale = new Vector3(1f, 0, 1f);
            }

            wasSolvedLastFrame = Solved;
        }

#region Saving

        public override void LoadSave(RoseRoomSave save) {
            obeliskConnectingBackdrop.localScale = save.obeliskConnectorBackgroundScale;
        }

        public override string ID => "RoseRoom";

        [Serializable]
        public class RoseRoomSave : SaveObject<RoseRoom> {
            public SerializableVector3 obeliskConnectorBackgroundScale;

            public RoseRoomSave(RoseRoom exit) : base(exit) {
                this.obeliskConnectorBackgroundScale = exit.obeliskConnectingBackdrop.localScale;
            }
        }
#endregion
    }
}