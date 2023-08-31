using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomMainConsole : SingletonSaveableObject<BlackRoomMainConsole, BlackRoomMainConsole.BlackRoomMainConsoleSave> {
        public enum State {
            Depowered,
            Powering,
            Powered
        }
        public StateMachine<State> state;
        public PowerTrail mainPower;
        
        // Mini Spotlights
        public BlackRoomMiniSpotlight[] miniSpotlights;
        private const float timeBetweenMiniSpotlights = 2f;
        
        // Puzzle is solved indicator
        public Renderer puzzleIsSolvedIndicator;
        public Button puzzleIsSolvedButton;
        
        // Main grid
        public SuperspectiveRenderer smallPuzzleGrid, mainPuzzleGrid;
        
        // Puzzle select buttons
        public ColorPuzzleManager colorPuzzleManager;
        public ColorPuzzleButton[] puzzleSelectButtons;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            state.Set(State.Depowered);
            foreach (BlackRoomMiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOff(true);
            }

            for (var i = 0; i < puzzleSelectButtons.Length; i++) {
                int index = i;
                puzzleSelectButtons[i].state.OnStateChangeSimple += () => {
                    if (puzzleSelectButtons[index].state == ColorPuzzleButton.State.On) {
                        colorPuzzleManager.activePuzzle = index;
                        for (int j = 0; j < puzzleSelectButtons.Length; j++) {
                            if (index == j) continue;

                            puzzleSelectButtons[j].state.Set(ColorPuzzleButton.State.Off);
                        }
                    }
                };
            }
            
            // Add events and state transitions to state machine
            AddMiniSpotlightEvents();
            AddPuzzleIndicatorEvents();
            AddPuzzleSelectCoverEvents();
        }

        private void AddPuzzleIndicatorEvents() {
            state.AddTrigger(State.Powered, 0f, () => {
                Color curColor = puzzleIsSolvedIndicator.material.color;
                curColor.a = 1;
                puzzleIsSolvedIndicator.material.color = curColor;
            });
        }

        // Update is called once per frame
        void Update() {
            if (state == State.Depowered && mainPower.state == PowerTrailState.Powered) {
                state.Set(State.Powering);
            }
            
            if (DebugInput.GetKeyDown(KeyCode.Alpha0)) {
                state.Set((State) (2 - (int) state.state));
            }

            UpdateMiniSpotlights();
            UpdatePuzzleIsSolvedIndicator();
            UpdateDissolveLaser();
            UpdateGridColors();
        }

        #region Puzzle Grids
        private void UpdateGridColors() {
            Color color = Color.clear;
            Color endColor = Color.white;
            switch (state.state) {
                case State.Depowered:
                    break;
                case State.Powering:
                    float t = state.timeSinceStateChanged / totalTimeToTurnOnSpotlights;
                    color = Color.Lerp(Color.clear, endColor, t);
                    break;
                case State.Powered:
                    color = endColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            smallPuzzleGrid.SetMainColor(color);
            mainPuzzleGrid.SetMainColor(color);
        }
        #endregion

        #region Mini Spotlights

        float totalTimeToTurnOnSpotlights => miniSpotlights.Length * timeBetweenMiniSpotlights;
        
        void UpdateMiniSpotlights() {
            switch (state.state) {
                case State.Depowered:
                    TurnLightsOff();
                    break;
                case State.Powering:
                    break;
                case State.Powered:
                    TurnLightsOn();
                    break;
            }
        }
        
        void AddMiniSpotlightEvents() {
            // Gradually turn on the spotlights
            for (int i = 0; i < miniSpotlights.Length; i++) {
                int index = i;
                state.AddTrigger(State.Powering, i * timeBetweenMiniSpotlights, () => miniSpotlights[index].TurnOn());
            }
            
            state.AddStateTransition(State.Powering, State.Powered, totalTimeToTurnOnSpotlights);
        }

        void TurnLightsOff() {
            foreach (BlackRoomMiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOff();
            }
        }
        
        void TurnLightsOn() {
            foreach (BlackRoomMiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOn();
            }
        }
        #endregion

        #region Puzzle Is Solved Indicator

        void UpdatePuzzleIsSolvedIndicator() {
            const float lerpSpeed = .5f;
            switch (state.state) {
                case State.Depowered: {
                    Color curColor = puzzleIsSolvedIndicator.material.color;
                    curColor.a = Mathf.Lerp(curColor.a, 1f/255f, Time.deltaTime * lerpSpeed);
                    puzzleIsSolvedIndicator.material.color = curColor;
                    break;
                }
                case State.Powering: {
                    float timeSinceFirstLightOn = state.timeSinceStateChanged;
                    float t = (timeSinceFirstLightOn % timeBetweenMiniSpotlights) / timeBetweenMiniSpotlights;

                    float[] keyframes = new[] {7f / 255f, 43f / 255f, 1};
                    Color curColor = puzzleIsSolvedIndicator.material.color;
                    // One light
                    if (timeSinceFirstLightOn < timeBetweenMiniSpotlights) {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[0], Time.deltaTime * lerpSpeed);
                    }
                    // Two lights
                    else if (timeSinceFirstLightOn < 2 * timeBetweenMiniSpotlights) {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[1], Time.deltaTime * lerpSpeed);
                    }
                    // All three lights
                    else {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[2], Time.deltaTime * lerpSpeed);
                    }

                    puzzleIsSolvedIndicator.material.color = curColor;
                    break;
                }
                case State.Powered: {
                    if (dissolveLaser.state == BlackRoomDissolveCoverLaser.LaserState.Idle) {
                        if (colorPuzzleManager.activePuzzle >= 0) {
                            ColorPuzzle curPuzzle = colorPuzzleManager.puzzles[colorPuzzleManager.activePuzzle];
                            float t = (float)curPuzzle.numSolved / curPuzzle.numPuzzles;
                            Color curEmission = puzzleIsSolvedIndicator.material.GetColor(BlackRoomDissolveCoverLaser.EmissionProperty);
                            Color emission = Color.Lerp(curEmission, t * dissolveLaser.startingEmission, Time.deltaTime * lerpSpeed);
                            puzzleIsSolvedIndicator.material.SetColor(BlackRoomDissolveCoverLaser.EmissionProperty, emission);
                            puzzleIsSolvedIndicator.material.color = Color.Lerp(puzzleIsSolvedIndicator.material.color, puzzleIsSolvedIndicator.material.color.WithAlpha(0.25f + 0.75f*t), Time.deltaTime * lerpSpeed);
                        }
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region Puzzle Select Covers

        public BlackRoomDissolveCoverLaser dissolveLaser;
        public DissolveObject[] puzzleSelectCovers;

        private const float timeAfterPoweredBeforeLaser = 0.5f;

        void AddPuzzleSelectCoverEvents() {
            state.AddTrigger(State.Powered, () => dissolveLaser.FireAt(puzzleSelectCovers[0]));
        }
            
        void UpdateDissolveLaser() {
            switch (state.state) {
                case State.Depowered:
                case State.Powering:
                    dissolveLaser.Stop();
                    break;
                case State.Powered:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        public override string ID => "BlackRoomMainConsole";

        [Serializable]
        public class BlackRoomMainConsoleSave : SerializableSaveObject<BlackRoomMainConsole> {
            private StateMachine<State>.StateMachineSave stateSave;

            public BlackRoomMainConsoleSave(BlackRoomMainConsole script) : base(script) {
                stateSave = script.state.ToSave();
            }
            
            public override void LoadSave(BlackRoomMainConsole script) {
                script.state.LoadFromSave(stateSave);
            }
        }
    }
}