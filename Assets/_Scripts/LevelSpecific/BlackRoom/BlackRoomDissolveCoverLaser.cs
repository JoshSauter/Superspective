using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomDissolveCoverLaser : MonoBehaviour {
        
        // TODO: Set this up as the SaveableObject ID instead
        const string ID = "BlackRoomDissolveCoverLaser";
        
        enum LaserState {
            Idle,
            FiringAtCover
        }

        private StateMachine<LaserState> state;
        
        const float verticalOffsetFromCover = 0.15f;
        private const float timeBeforeDissolveBegin = 1f;
        private const float timeBeforeLaserParticlesStart = 0.15f;
        public ParticleSystem laser;
        private DissolveObject coverFiringAt;
        private Color startingEmission = Color.black;

        public Renderer puzzleIsSolvedIndicator;
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");

        private Vector3 targetParticleEndPosition => (coverFiringAt == null)
            ? Vector3.zero
            : coverFiringAt.transform.position + coverFiringAt.transform.up * verticalOffsetFromCover;

        private void Start() {
            state = new StateMachine<LaserState>(LaserState.Idle);
            
            state.AddStateTransition(LaserState.FiringAtCover, LaserState.Idle, laser.main.duration);
            state.AddTrigger(LaserState.FiringAtCover, timeBeforeDissolveBegin, DissolveCoverFiringAt);
            state.AddTrigger(LaserState.Idle, 0f, Stop);
            state.AddTrigger(LaserState.FiringAtCover, timeBeforeLaserParticlesStart, () => laser.Play());
            state.AddTrigger(
                LaserState.FiringAtCover, 
                timeBeforeLaserParticlesStart + laser.main.startLifetime.constant, 
                () => AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, coverFiringAt.transform.position));
        }

        public void FireAt(DissolveObject cover) {
            if (laser.isPlaying) return;

            coverFiringAt = cover;
            LookAtCover();
            
            state.Set(LaserState.FiringAtCover);

            AudioManager.instance.PlayAtLocation(AudioName.LaserBeamShort, ID, laser.transform.position);
            
            startingEmission = puzzleIsSolvedIndicator.material.GetColor(EmissionProperty);
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
            if (state.state != LaserState.FiringAtCover) return;
            
            float t = state.timeSinceStateChanged / laser.main.duration;
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
            float t = state.timeSinceStateChanged / main.duration;
            ParticleSystem.MinMaxGradient startGradient = main.startColor;
            Color startColor = startGradient.color;
            startColor.a = 1-t;
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
    }
}