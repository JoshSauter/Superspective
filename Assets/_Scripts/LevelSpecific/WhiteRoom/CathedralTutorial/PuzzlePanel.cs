using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using NaughtyAttributes;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public enum PuzzleState {
        Off, // Not powered up
        Idle, // Idle, not correct
        PoweringUp, // Powering up, until moment of checking answer
        Incorrect, // Displaying incorrect feedback
        Depowering, // Depowering back to Idle
        Correct, // Displaying correct feedback
        CorrectIdle // Idle, correct
    }
    
    [RequireComponent(typeof(UniqueId))]
    public class PuzzlePanel : SaveableObject<PuzzlePanel, PuzzlePanel.PuzzlePanelSave>, AudioJobOnGameObject {

        public FloorPuzzle floorPuzzle;
        
        public StateMachine<PuzzleState> state = new StateMachine<PuzzleState>(PuzzleState.Off);
        public ValueDisplay valueIcon;
        public PowerButton powerButton;
        public PowerTrail powerTrailFromPrevPuzzle;
        public PowerTrail powerTrail;
        public PowerTrail powerTrailToNextPuzzle;
        public List<PowerButton> powerButtonsToDisable;
        
        [ShowNativeProperty]
        public int value => valueIcon.actualValue;
        
        // Animation tweaking
        private const float correctToDepowerDelay = 1.5f;
        public const int incorrectFlashTimes = 4;
        public const float incorrectFlashDuration = 2f;
        private const float valueIconAlphaLerpSpeed = 1f;
        public const float currentValueAlphaLerpSpeed = 3f;
        public static readonly Color correctColor = new Color(.2f, .8f, .3f);
        public static readonly Color incorrectColor = new Color(.8f, .2f, .3f);
        private readonly Color disabledPowerSourceButtonColor = new Color(0.04f, 0.04f, 0.04f);

        private bool isFirstPuzzle => powerTrailFromPrevPuzzle == null;
        private bool isLastPuzzle;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            floorPuzzle.powerTrailBottomMiddle.OnDepowerFinish += () => powerTrail.powerIsOn = false;
            if (powerTrailFromPrevPuzzle != null) {
                powerTrailFromPrevPuzzle.OnPowerFinish += () => {
                    if (state == PuzzleState.Off) state.Set(PuzzleState.Idle);
                };
            }
            
            isLastPuzzle = powerTrailToNextPuzzle.name.Contains("PowerTrailToPortalDoor");

            Invoke(nameof(InitStateMachine), 0.1f);
        }
        
        void InitStateMachine() {
            state.AddStateTransition(PuzzleState.PoweringUp, NextState, powerTrail.duration + floorPuzzle.powerTrailBottomMiddle.duration);
            state.AddStateTransition(PuzzleState.Incorrect, PuzzleState.Depowering, incorrectFlashDuration);
            state.AddStateTransition(PuzzleState.Depowering, PuzzleState.Idle, powerTrail.durationOff + floorPuzzle.powerTrailBottomMiddle.durationOff);
            state.AddStateTransition(PuzzleState.Correct, PuzzleState.CorrectIdle, correctToDepowerDelay);

            // Incorrect SFX
            for (int i = 0; i < incorrectFlashTimes; i++) {
                state.AddTrigger(PuzzleState.Incorrect, (incorrectFlashDuration/incorrectFlashTimes) * i, () =>
                    AudioManager.instance.PlayOnGameObject(AudioName.IncorrectAnswer, "IncorrectAnswer", this));
            }

            // Depowering power trails
            state.AddTrigger(PuzzleState.Depowering, 0f, () => {
                powerButton.powerIsOn = false;
                floorPuzzle.powerTrailBottomMiddle.powerIsOn = false;
            });
            // Powering next puzzle panel
            state.AddTrigger(stateToCheck => stateToCheck is not (PuzzleState.Correct or PuzzleState.CorrectIdle), 0f, () => powerTrailToNextPuzzle.powerIsOn = false);
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => {
                if (isLastPuzzle) {
                    floorPuzzle.currentValueShutter.isSetToOpen = true;
                    floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
                }
                powerTrailToNextPuzzle.powerIsOn = true;
            });
            
            // Reset state for new puzzle panel
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => {
                FloorManager.instance.TurnOffAllPowerSources();
                floorPuzzle.powerTrailBottomMiddle.powerIsOn = false;
            });
            
            // Turning off PowerButtons that are disabled for this puzzle
            state.AddTrigger(PuzzleState.Idle, 0f, () => {
                if (state.prevState == PuzzleState.Off) {
                    valueIcon.spriteAlpha = 1f;
                    valueIcon.desiredColor = Color.white;
                }
                
                foreach (var button in powerButtonsToDisable) {
                    DisablePowerButton(button);
                }

                foreach (PowerButton button in floorPuzzle.powerSources.Select(ps => ps.powerButton).Except(powerButtonsToDisable)) {
                    EnablePowerButton(button);
                }
            });

            // Colors of value display
            state.AddTrigger(PuzzleState.Correct, 0f, () => {
                AudioManager.instance.Play(AudioName.CorrectAnswer, "CorrectAnswer");
                valueIcon.desiredColor = correctColor;
                floorPuzzle.currentValue.desiredColor = correctColor;
            });
            if (isLastPuzzle) {
                powerTrailToNextPuzzle.OnPowerFinish += () => floorPuzzle.currentValue.desiredColor = floorPuzzle.currentValue.defaultColor;
            }
            state.AddTrigger(PuzzleState.Idle, floorPuzzle.powerTrailTopMiddle.duration + .125f, () => {
                if (floorPuzzle.currentValue.actualValue == 0) {
                    floorPuzzle.currentValue.desiredColor = floorPuzzle.currentValue.defaultColor;
                }
            });
            
            // Floor 2 has opening and closing shutters
            if (floorPuzzle.floor == FloorManager.Floor.Floor2) {
                powerTrail.OnPowerBegin += OpenShutters;
                powerTrail.OnDepowerBegin += CloseShutters;
            }
        }

        void OpenShutters() {
            floorPuzzle.currentValueShutter.isSetToOpen = true;
            floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
        }

        void CloseShutters() {
            if ((isLastPuzzle && (state == PuzzleState.Correct || state == PuzzleState.CorrectIdle)) || (isFirstPuzzle && state == PuzzleState.Depowering)) return;
            
            floorPuzzle.currentValueShutter.isSetToOpen = false;
            floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
        }

        void DisablePowerButton(PowerButton powerButton) {
            if (powerButton == null || powerButton.button == null || powerButton.button.interactableObject == null) return;
            
            powerButton.button.interactableObject.SetAsDisabled("(Disabled)");
            AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, powerButton.ID, powerButton.transform.position);
            powerButton.GetComponent<ButtonColorChange>().startColor = disabledPowerSourceButtonColor;
            foreach (ValueDisplay vd in powerButton.GetComponentsInChildren<ValueDisplay>()) {
                vd.spriteAlpha = 0;
            }
        }

        void EnablePowerButton(PowerButton powerButton) {
            if (powerButton == null || powerButton.button == null || powerButton.button.interactableObject == null) return;

            powerButton.button.interactableObject.SetAsInteractable();
            powerButton.GetComponent<ButtonColorChange>().startColor = Color.white;
            foreach (ValueDisplay vd in powerButton.GetComponentsInChildren<ValueDisplay>()) {
                vd.spriteAlpha = 1;
            }
        }

        PuzzleState NextState() {
            return Evaluate() ? PuzzleState.Correct : PuzzleState.Incorrect;
        }

        // Update is called once per frame
        void Update() {
            if (this.InstaSolvePuzzle()) {
                if (state == PuzzleState.Idle && FloorManager.instance.floor == floorPuzzle.floor) {
                    state.Set(PuzzleState.Correct);
                }
            }
            
            float targetAlpha = state == PuzzleState.Off ? 0f : 1f;
            valueIcon.spriteAlpha = targetAlpha;
            switch (state.state) {
                case PuzzleState.Off:
                    valueIcon.SetColorImmediately(Color.black);
                    break;
                case PuzzleState.Idle:
                    if (powerTrail.powerIsOn) {
                        state.Set(PuzzleState.PoweringUp);
                        return;
                    }
                    break;
                case PuzzleState.PoweringUp:
                    break;
                case PuzzleState.Incorrect:
                    if (Evaluate()) {
                        AudioManager.instance.GetAudioJob(AudioName.IncorrectAnswer, "IncorrectAnswer").Stop();
                        state.Set(PuzzleState.Correct);
                        break;
                    }
                    
                    float t = 0.5f + 0.5f*Mathf.Cos(state.timeSinceStateChanged * incorrectFlashTimes * 2 * Mathf.PI/incorrectFlashDuration + Mathf.PI);
                    floorPuzzle.currentValue.SetColorImmediately(Color.Lerp(floorPuzzle.currentValue.defaultColor, incorrectColor, t));
                    valueIcon.SetColorImmediately(Color.Lerp(valueIcon.defaultColor, incorrectColor, t).WithAlpha(targetAlpha));
                    break;
                case PuzzleState.Depowering:
                case PuzzleState.Correct:
                case PuzzleState.CorrectIdle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (state == PuzzleState.Off) {
                powerButton.button.interactableObject.SetAsDisabled("(Missing power)");
            }
            else if (state == PuzzleState.Idle) {
                powerButton.button.interactableObject.SetAsInteractable("Check solution");
            }
            else {
                powerButton.button.interactableObject.SetAsHidden();
            }
        }
        
        bool Evaluate() {
            return value == floorPuzzle.currentValue.actualValue;
        }

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) {
            return transform;
        }

        #region Saving


        [Serializable]
        public class PuzzlePanelSave : SerializableSaveObject<PuzzlePanel> {
            private StateMachine<PuzzleState>.StateMachineSave stateSave;
            
            public PuzzlePanelSave(PuzzlePanel script) : base(script) {
                this.stateSave = script.state.ToSave();
            }
            
            public override void LoadSave(PuzzlePanel script) {
                script.state.FromSave(this.stateSave);
            }
        }

        #endregion
    }
}