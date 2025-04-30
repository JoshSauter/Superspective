using System;
using DissolveObjects;
using Interactables;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomMainConsole : SingletonSuperspectiveObject<BlackRoomMainConsole, BlackRoomMainConsole.BlackRoomMainConsoleSave> {
        public enum State : byte {
            Depowered,
            Powering,
            Powered
        }
        public StateMachine<State> state;
        public PowerTrail mainPower;
        
        // Mini Spotlights
        public BlackRoomMiniSpotlight[] miniSpotlights;
        private const float TIME_BETWEEN_MINI_SPOTLIGHTS = 2f;
        
        // Puzzle is solved indicator
        public SuperspectiveRenderer puzzleIsSolvedIndicator;
        public Button puzzleIsSolvedButton;
        
        // Main grid
        public SuperspectiveRenderer smallPuzzleGrid, mainPuzzleGrid;
        
        // Puzzle select buttons
        public ColorPuzzleManager colorPuzzleManager;
        public ColorPuzzleButton[] puzzleSelectButtons;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            state = this.StateMachine(State.Depowered);
            foreach (BlackRoomMiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOff(true);
            }

            for (var i = 0; i < puzzleSelectButtons.Length; i++) {
                int index = i;
                puzzleSelectButtons[i].state.OnStateChangeSimple += () => {
                    if (puzzleSelectButtons[index].state == ColorPuzzleButton.State.On) {
                        colorPuzzleManager.ActivePuzzle = index;
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
                Color curColor = puzzleIsSolvedIndicator.GetMainColor();
                curColor.a = 1;
                puzzleIsSolvedIndicator.SetMainColor(curColor);
            });
        }

        // Update is called once per frame
        void Update() {
            if (GameManager.instance.IsCurrentlyLoading) return;
            
            if (state == State.Depowered && mainPower.pwr.FullyPowered) {
                state.Set(State.Powering);
            }
            
            if (DebugInput.GetKeyDown(KeyCode.Alpha0)) {
                state.Set((State) (2 - (int) state.State));
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
            switch (state.State) {
                case State.Depowered:
                    break;
                case State.Powering:
                    float t = state.Time / totalTimeToTurnOnSpotlights;
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

        float totalTimeToTurnOnSpotlights => miniSpotlights.Length * TIME_BETWEEN_MINI_SPOTLIGHTS;
        
        void UpdateMiniSpotlights() {
            switch (state.State) {
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
                state.AddTrigger(State.Powering, i * TIME_BETWEEN_MINI_SPOTLIGHTS, () => miniSpotlights[index].TurnOn());
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
            const float LERP_SPEED = .5f;
            switch (state.State) {
                case State.Depowered: {
                    Color curColor = puzzleIsSolvedIndicator.GetMainColor();
                    curColor.a = Mathf.Lerp(curColor.a, 1f/255f, Time.deltaTime * LERP_SPEED);
                    puzzleIsSolvedIndicator.SetMainColor(curColor);
                    break;
                }
                case State.Powering: {
                    float timeSinceFirstLightOn = state.Time;
                    float t = (timeSinceFirstLightOn % TIME_BETWEEN_MINI_SPOTLIGHTS) / TIME_BETWEEN_MINI_SPOTLIGHTS;

                    float[] keyframes = new[] {7f / 255f, 43f / 255f, 1};
                    Color curColor = puzzleIsSolvedIndicator.GetMainColor();
                    // One light
                    if (timeSinceFirstLightOn < TIME_BETWEEN_MINI_SPOTLIGHTS) {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[0], Time.deltaTime * LERP_SPEED);
                    }
                    // Two lights
                    else if (timeSinceFirstLightOn < 2 * TIME_BETWEEN_MINI_SPOTLIGHTS) {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[1], Time.deltaTime * LERP_SPEED);
                    }
                    // All three lights
                    else {
                        curColor.a = Mathf.Lerp(curColor.a, keyframes[2], Time.deltaTime * LERP_SPEED);
                    }

                    puzzleIsSolvedIndicator.SetMainColor(curColor);
                    break;
                }
                case State.Powered: {
                    if (dissolveLaser.state == BlackRoomDissolveCoverLaser.LaserState.Idle) {
                        if (colorPuzzleManager.ActivePuzzle >= 0) {
                            ColorPuzzle curPuzzle = colorPuzzleManager.puzzles[colorPuzzleManager.ActivePuzzle];
                            float t = (float)curPuzzle.NumSolved / curPuzzle.NumPuzzles;
                            Color curEmission = puzzleIsSolvedIndicator.GetColor(BlackRoomDissolveCoverLaser.EMISSION_PROPERTY);
                            Color emission = Color.Lerp(curEmission, t * dissolveLaser.startingEmission, Time.deltaTime * LERP_SPEED);
                            puzzleIsSolvedIndicator.SetColor(BlackRoomDissolveCoverLaser.EMISSION_PROPERTY, emission);
                            Color curColor = puzzleIsSolvedIndicator.GetMainColor();
                            puzzleIsSolvedIndicator.SetMainColor(Color.Lerp(curColor, curColor.WithAlpha(0.25f + 0.75f*t), Time.deltaTime * LERP_SPEED));
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
            switch (state.State) {
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

        public override void LoadSave(BlackRoomMainConsoleSave save) {
            UpdateMiniSpotlights();
            UpdatePuzzleIsSolvedIndicator();
            UpdateDissolveLaser();
            UpdateGridColors();
        }

        [Serializable]
        public class BlackRoomMainConsoleSave : SaveObject<BlackRoomMainConsole> {
            public BlackRoomMainConsoleSave(BlackRoomMainConsole script) : base(script) { }
        }
    }
}