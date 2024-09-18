using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using DissolveObjects;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomDissolveCoverLaser : SaveableObject<BlackRoomDissolveCoverLaser, BlackRoomDissolveCoverLaser.BlackRoomDissolveCoverLaserSave> {
        
        public enum LaserState {
            Idle,
            FiringAtCover
        }

        public StateMachine<LaserState> state;
        
        private const float verticalOffsetFromCover = 0.15f;
        private const float timeBeforeDissolveBegin = 1f;
        private const float timeBeforeLaserParticlesStart = 0.15f;
        public ParticleSystem laser;
        private DissolveObject coverFiringAt;
        [ColorUsage(false, true)]
        public Color startingEmission = Color.black;

        public BlackRoomMainConsole mainConsole;
        public ColorPuzzleManager colorPuzzleManager;
        public Renderer puzzleIsSolvedIndicator;
        public static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");

        private Vector3 targetParticleEndPosition => (coverFiringAt == null)
            ? Vector3.zero
            : coverFiringAt.transform.position + coverFiringAt.transform.up * verticalOffsetFromCover;

        protected override void Start() {
            base.Start();
            state = this.StateMachine(LaserState.Idle);
            
            state.AddStateTransition(LaserState.FiringAtCover, LaserState.Idle, laser.main.duration);
            state.AddTrigger(LaserState.FiringAtCover, timeBeforeDissolveBegin, DissolveCoverFiringAt);
            state.AddTrigger(LaserState.Idle, 0f, Stop);
            state.AddTrigger(LaserState.FiringAtCover, timeBeforeLaserParticlesStart, () => laser.Play());
            state.AddTrigger(
                LaserState.FiringAtCover, 
                timeBeforeLaserParticlesStart + laser.main.startLifetime.constant, 
                () => AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, coverFiringAt.transform.position));
            state.AddTrigger(LaserState.Idle, 0f, () => {
                if (state.PrevState == LaserState.FiringAtCover) {
                    // Turn the next puzzle on and last puzzle off automatically
                    if (!colorPuzzleManager.isLastPuzzle) {
                        mainConsole.puzzleSelectButtons[colorPuzzleManager.activePuzzle+1].state.Set(ColorPuzzleButton.State.On);
                    }
                    else if (!colorPuzzleManager.isFirstPuzzle) {
                        mainConsole.puzzleSelectButtons[colorPuzzleManager.activePuzzle].state.Set(ColorPuzzleButton.State.Off);
                    }
                }
            });

            // Getting the sharedMaterial value because the actual Material may have been dimmed by now
            startingEmission = puzzleIsSolvedIndicator.sharedMaterial.GetColor(EmissionProperty);
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

        private void UpdateEmission() {
            if (state.State != LaserState.FiringAtCover) return;
            
            float t = state.Time / laser.main.duration;
            Color emission = Color.Lerp(startingEmission, Color.black, t);
            puzzleIsSolvedIndicator.material.SetColor(EmissionProperty, emission);
        }

        // As the origin of the particle system bobs up and down, the distance the particles need to travel changes slightly
        // This will set the lifespan of the particles accordingly
        void UpdateParticles() {
            float distance = (targetParticleEndPosition - laser.transform.position).magnitude;

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
            laser.transform.LookAt(targetParticleEndPosition);
        }

        void DissolveCoverFiringAt() {
            if (coverFiringAt == null) return;

            coverFiringAt.Dematerialize();
        }

        [Serializable]
        public class BlackRoomDissolveCoverLaserSave : SerializableSaveObject<BlackRoomDissolveCoverLaser> {
            private StateMachine<LaserState>.StateMachineSave stateSave;
            SerializableReference<DissolveObject, DissolveObject.DissolveObjectSave> coverFiringAt;
            SerializableParticleSystem laser;

            public BlackRoomDissolveCoverLaserSave(BlackRoomDissolveCoverLaser script) : base(script) {
                stateSave = script.state.ToSave();
                coverFiringAt = script.coverFiringAt;
                laser = script.laser;
            }
            
            public override void LoadSave(BlackRoomDissolveCoverLaser script) {
                script.state.LoadFromSave(this.stateSave);
                script.coverFiringAt = this.coverFiringAt?.GetOrNull();
                this.laser.ApplyToParticleSystem(script.laser);
            }
        }
    }
}