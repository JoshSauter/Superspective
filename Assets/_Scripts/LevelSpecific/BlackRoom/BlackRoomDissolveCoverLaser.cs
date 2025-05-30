using System;
using Audio;
using DissolveObjects;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveAttributes;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomDissolveCoverLaser : SuperspectiveObject<BlackRoomDissolveCoverLaser, BlackRoomDissolveCoverLaser.BlackRoomDissolveCoverLaserSave> {
        
        public enum LaserState : byte {
            Idle,
            FiringAtCover
        }

        public StateMachine<LaserState> state;
        
        private const float VERTICAL_OFFSET_FROM_COVER = 0.15f;
        private const float TIME_BEFORE_DISSOLVE_BEGIN = 1f;
        private const float TIME_BEFORE_LASER_PARTICLE_START = 0.15f;
        public static readonly int EMISSION_PROPERTY = Shader.PropertyToID("_EmissionColor");
        [SaveUnityObject]
        public ParticleSystem laser;
        [SaveUnityObject]
        private DissolveObject coverFiringAt;
        [ColorUsage(false, true)]
        public Color startingEmission = Color.black;

        public BlackRoomMainConsole mainConsole;
        public ColorPuzzleManager colorPuzzleManager;
        public SuperspectiveRenderer puzzleIsSolvedIndicator;

        private Vector3 TargetParticleEndPosition => (coverFiringAt == null)
            ? Vector3.zero
            : coverFiringAt.transform.position + coverFiringAt.transform.up * VERTICAL_OFFSET_FROM_COVER;

        protected override void Start() {
            base.Start();
            startingEmission = puzzleIsSolvedIndicator.GetColor(EMISSION_PROPERTY);
            
            state = this.StateMachine(LaserState.Idle);
            
            state.AddStateTransition(LaserState.FiringAtCover, LaserState.Idle, laser.main.duration);
            state.AddTrigger(LaserState.FiringAtCover, TIME_BEFORE_DISSOLVE_BEGIN, DissolveCoverFiringAt);
            state.AddTrigger(LaserState.Idle, 0f, Stop);
            state.AddTrigger(LaserState.FiringAtCover, TIME_BEFORE_LASER_PARTICLE_START, () => laser.Play());
            state.AddTrigger(
                LaserState.FiringAtCover, 
                TIME_BEFORE_LASER_PARTICLE_START + laser.main.startLifetime.constant, 
                () => AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, coverFiringAt.transform.position));
            state.AddTrigger(LaserState.Idle, 0f, () => {
                if (state.PrevState == LaserState.FiringAtCover) {
                    // Turn the next puzzle on and last puzzle off automatically
                    if (!colorPuzzleManager.IsLastPuzzle) {
                        mainConsole.puzzleSelectButtons[colorPuzzleManager.ActivePuzzle+1].state.Set(ColorPuzzleButton.State.On);
                    }
                    else if (!colorPuzzleManager.IsFirstPuzzle) {
                        mainConsole.puzzleSelectButtons[colorPuzzleManager.ActivePuzzle].state.Set(ColorPuzzleButton.State.Off);
                    }
                }
            });
        }

        public void FireAt(DissolveObject cover) {
            if (laser.isPlaying) return;

            coverFiringAt = cover;
            LookAtCover();
            
            state.Set(LaserState.FiringAtCover);

            AudioManager.instance.PlayAtLocation(AudioName.LaserBeamShort, ID, laser.transform.position);
        }

        public void Stop() {
            if (!laser.isPlaying) return;
            
            laser.Stop();
            coverFiringAt = null;
            state.Set(LaserState.Idle);
        }

        private void Update() {
            if (coverFiringAt == null) return;
            
            if (laser.isPlaying) {
                LookAtCover();
                UpdateParticles();
                UpdateEmission();
            }
        }

        // TODO: Replace all this with SuperspectiveRenderer using MaterialPropertyBlocks
        private void UpdateEmission() {
            if (state.State != LaserState.FiringAtCover) return;
            
            float t = state.Time / laser.main.duration;
            Color emission = Color.Lerp(startingEmission, Color.black, t);
            puzzleIsSolvedIndicator.SetColor(EMISSION_PROPERTY, emission);
        }

        // As the origin of the particle system bobs up and down, the distance the particles need to travel changes slightly
        // This will set the lifespan of the particles accordingly
        void UpdateParticles() {
            float distance = (TargetParticleEndPosition - laser.transform.position).magnitude;

            // Update lifetime to match slightly changing distances
            ParticleSystem.MainModule main = laser.main;
            main.startLifetime = distance / main.startSpeed.constant;
            
            // Fade out the laser over time
            float t = state.Time / main.duration;
            ParticleSystem.MinMaxGradient startGradient = main.startColor;
            Color startColor = startGradient.color;
            startColor.a = 1-(t*t);
            startGradient.color = startColor;
            main.startColor = startGradient;
        }

        void LookAtCover() {
            laser.transform.LookAt(TargetParticleEndPosition);
        }

        void DissolveCoverFiringAt() {
            if (coverFiringAt == null) return;

            coverFiringAt.Dematerialize();
        }

        public override void LoadSave(BlackRoomDissolveCoverLaserSave save) {
            if (laser.isPlaying) {
                LookAtCover();
                UpdateParticles();
                UpdateEmission();
            }
        }

        [Serializable]
        public class BlackRoomDissolveCoverLaserSave : SaveObject<BlackRoomDissolveCoverLaser> {
            public BlackRoomDissolveCoverLaserSave(BlackRoomDissolveCoverLaser script) : base(script) { }
        }
    }
}