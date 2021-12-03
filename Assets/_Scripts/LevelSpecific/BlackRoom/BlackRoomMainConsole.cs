using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using PowerTrailMechanics;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.BlackRoom {
    public class BlackRoomMainConsole : MonoBehaviour {
        public enum State {
            Depowered,
            Powering,
            Powered
        }
        public StateMachine<State> state;
        public PowerTrail mainPower;
        
        // Mini Spotlights
        public MiniSpotlight[] miniSpotlights;
        private const float timeBetweenMiniSpotlights = 2f;
        
        // Puzzle is solved indicator
        public Renderer puzzleIsSolvedIndicator;
        public Button puzzleIsSolvedButton;
        
        // Main grid
        public SuperspectiveRenderer smallPuzzleGrid, mainPuzzleGrid;
        
        // Puzzle select buttons
        public ColorPuzzleManager colorPuzzleManager;
        public Button[] puzzleSelectButtons;
        
        // Start is called before the first frame update
        void Start() {
            state.Set(State.Depowered);
            foreach (MiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOff();
            }

            puzzleIsSolvedButton.interactableObject.SetAsDisabled();
            puzzleIsSolvedButton.OnButtonPressBegin += (_) => {
                if (colorPuzzleManager.activePuzzle == colorPuzzleManager.numPuzzles - 1) return;

                if (colorPuzzleManager.CheckSolution(true)) {
                    dissolveLaser.FireAt(puzzleSelectCovers[colorPuzzleManager.activePuzzle + 1]);
                }
                else {
                    colorPuzzleManager.FlashIncorrect();
                }
            };
            
            for (var i = 0; i < puzzleSelectButtons.Length; i++) {
                int index = i;
                puzzleSelectButtons[i].OnButtonPressFinish += (buttonPressed) => {
                    colorPuzzleManager.activePuzzle = index;

                    foreach (var buttonToTurnOff in puzzleSelectButtons) {
                        if (buttonToTurnOff == buttonPressed ||
                            buttonToTurnOff.state == Button.State.ButtonUnpressed ||
                            buttonToTurnOff.state == Button.State.ButtonUnpressing) continue;

                        buttonToTurnOff.state = Button.State.ButtonUnpressing;
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
            if (state == State.Depowered && mainPower.state == PowerTrail.PowerTrailState.powered) {
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
        
        void AddMiniSpotlightEvents() {
            // Gradually turn on the spotlights
            for (int i = 0; i < miniSpotlights.Length; i++) {
                int index = i;
                state.AddTrigger(State.Powering, i * timeBetweenMiniSpotlights, () => miniSpotlights[index].TurnOn());
            }
            
            state.AddStateTransition(State.Powering, State.Powered, totalTimeToTurnOnSpotlights);
        }
        
        [Serializable]
        public class MiniSpotlight {
            public Renderer lightSource;
            public Renderer lightBeam;
            
            public bool isOn = true;

            public void TurnOff() {
                if (isOn) {
                    lightSource.GetOrAddComponent<SuperspectiveRenderer>().SetInt("_EmissionEnabled", 0);
                    lightBeam.enabled = false;
                    isOn = false;
                }
            }

            public void TurnOn() {
                if (!isOn) {
                    lightSource.GetOrAddComponent<SuperspectiveRenderer>().SetInt("_EmissionEnabled", 1);
                    lightBeam.enabled = true;
                    isOn = true;
                    AudioManager.instance.PlayAtLocation(AudioName.LightSwitch, "BlackRoom_MainConsole", lightSource.transform.position);
                }
            }
        }
        
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

        void TurnLightsOff() {
            foreach (MiniSpotlight miniSpotlight in miniSpotlights) {
                miniSpotlight.TurnOff();
            }
        }
        
        void TurnLightsOn() {
            foreach (MiniSpotlight miniSpotlight in miniSpotlights) {
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
            state.AddTrigger(State.Powered, timeAfterPoweredBeforeLaser, () => puzzleIsSolvedButton.interactableObject.SetAsInteractable());
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
    }
}