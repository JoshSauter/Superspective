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
        public SpriteRenderer valueIcon;
        public PowerButton powerButton;
        public PowerTrail powerTrailFromPrevPuzzle;
        public PowerTrail powerTrail;
        public PowerTrail powerTrailToNextPuzzle;
        public List<PowerButton> powerButtonsToDisable;
        
        [ShowNativeProperty]
        public int value => int.Parse(valueIcon.sprite.name);
        
        // Animation tweaking
        private const float correctToDepowerDelay = 1.5f;
        private const int incorrectFlashTimes = 4;
        private const float incorrectFlashDuration = 2f;
        private const float valueIconAlphaLerpSpeed = 1f;
        private const float currentValueAlphaLerpSpeed = 3f;
        private readonly Color correctColor = new Color(.2f, .8f, .3f);
        private readonly Color incorrectColor = new Color(.8f, .2f, .3f);
        private readonly Color disabledPowerSourceButtonColor = new Color(0.04f, 0.04f, 0.04f);
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            floorPuzzle.powerTrailBottomMiddle.OnDepowerFinish += () => powerTrail.powerIsOn = false;
            if (powerTrailFromPrevPuzzle != null) {
                powerTrailFromPrevPuzzle.OnPowerFinish += () => {
                    if (state == PuzzleState.Off) state.Set(PuzzleState.Idle);
                };
            }

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
                    AudioManager.instance.PlayOnGameObject(AudioName.DisabledSound, "WrongAnswer", this, settingsOverride: AudioJobSettings));
            }

            // Depowering power trails
            state.AddTrigger(PuzzleState.Depowering, 0f, () => {
                powerButton.powerIsOn = false;
                floorPuzzle.powerTrailBottomMiddle.powerIsOn = false;
            });
            // Powering next puzzle panel
            state.AddTrigger(stateToCheck => stateToCheck != PuzzleState.Correct, 0f, () => powerTrailToNextPuzzle.powerIsOn = false);
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => powerTrailToNextPuzzle.powerIsOn = true);
            
            // Reset state for new puzzle panel
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => {
                foreach (var powerSource in floorPuzzle.powerSources) {
                    powerSource.powerTrail.powerIsOn = false;
                    if (powerSource.powerButton.powerIsOn) {
                        powerSource.powerButton.button.PressButton();
                    }
                }
                floorPuzzle.powerTrailBottomMiddle.powerIsOn = false;
            });
            
            // Turning off PowerButtons that are disabled for this puzzle
            state.AddTrigger(PuzzleState.Idle, 0f, () => {
                foreach (var button in powerButtonsToDisable) {
                    DisablePowerButton(button);
                }

                foreach (PowerButton button in floorPuzzle.powerSources.Select(ps => ps.powerButton).Except(powerButtonsToDisable)) {
                    EnablePowerButton(button);
                }
            });
        }

        void DisablePowerButton(PowerButton powerButton) {
            powerButton.button.interactableObject.SetAsDisabled();
            AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, powerButton.ID, powerButton.transform.position, settingsOverride: AudioJobSettings);
            powerButton.GetComponent<ButtonColorChange>().startColor = disabledPowerSourceButtonColor;
            foreach (SpriteRenderer sprite in powerButton.GetComponentsInChildren<SpriteRenderer>()) {
                sprite.color = sprite.color.WithAlpha(0f);
            }
        }

        void EnablePowerButton(PowerButton powerButton) {
            powerButton.button.interactableObject.SetAsInteractable();
            powerButton.GetComponent<ButtonColorChange>().startColor = Color.white;
            foreach (SpriteRenderer sprite in powerButton.GetComponentsInChildren<SpriteRenderer>()) {
                sprite.color = sprite.color.WithAlpha(1f);
            }
        }

        void AudioJobSettings(AudioManager.AudioJob audioJob) {
            audioJob.pitchRandomness = 0f;
            audioJob.basePitch = 0.75f;
        }

        PuzzleState NextState() {
            return Evaluate() ? PuzzleState.Correct : PuzzleState.Incorrect;
        }

        // Update is called once per frame
        void Update() {
            if (DebugInput.GetKeyDown("l")) {
                if (state == PuzzleState.Idle) {
                    state.Set(PuzzleState.Correct);
                }
            }
            
            float targetAlpha = state == PuzzleState.Off ? 0f : 1f;
            Color targetColor = Color.white;
            
            switch (state.state) {
                case PuzzleState.Off:
                    targetColor = Color.black;
                    break;
                case PuzzleState.Idle:
                    if (powerTrail.powerIsOn) {
                        state.Set(PuzzleState.PoweringUp);
                    }
                    break;
                case PuzzleState.PoweringUp:
                    break;
                case PuzzleState.Incorrect:
                    float t = 0.5f + 0.5f*Mathf.Cos(state.timeSinceStateChanged * incorrectFlashTimes * 2 * Mathf.PI/incorrectFlashDuration + Mathf.PI);
                    targetColor = Color.Lerp(targetColor, incorrectColor, t);
                    floorPuzzle.currentValue.color = Color.Lerp(Color.black, incorrectColor, t);
                    break;
                case PuzzleState.Depowering:
                    break;
                case PuzzleState.Correct:
                case PuzzleState.CorrectIdle:
                    targetColor = correctColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (state == PuzzleState.Off) {
                powerButton.button.interactableObject.SetAsDisabled();
            }
            else if (state == PuzzleState.Idle) {
                powerButton.button.interactableObject.SetAsInteractable();
            }
            else {
                powerButton.button.interactableObject.SetAsHidden();
            }
            
            // Value icon color
            if (state == PuzzleState.Incorrect) {
                valueIcon.color = targetColor.WithAlpha(targetAlpha);
            }
            else {
                valueIcon.color = Color.Lerp(valueIcon.color, targetColor.WithAlpha(targetAlpha), Time.deltaTime * valueIconAlphaLerpSpeed);
            }
            
            // CurrentValue color
            if (state == PuzzleState.Correct) {
                floorPuzzle.currentValue.color = Color.Lerp(floorPuzzle.currentValue.color, targetColor, Time.deltaTime * currentValueAlphaLerpSpeed);
            }
            // Resetting color when we switch to new puzzle
            else if (state == PuzzleState.Idle && floorPuzzle.currentValue.actualValue == 0) {
                floorPuzzle.currentValue.color = Color.black;
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