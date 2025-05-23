using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Interactables;
using NaughtyAttributes;
using PowerTrailMechanics;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace LevelSpecific.WhiteRoom.CathedralTutorial {
    public enum PuzzleState : byte {
        Off, // Not powered up
        Idle, // Idle, not correct
        PoweringUp, // Powering up, until moment of checking answer
        Incorrect, // Displaying incorrect feedback
        Depowering, // Depowering back to Idle
        Correct, // Displaying correct feedback
        CorrectIdle // Idle, correct
    }
    
    [RequireComponent(typeof(UniqueId))]
    public class PuzzlePanel : SuperspectiveObject<PuzzlePanel, PuzzlePanel.PuzzlePanelSave>, AudioJobOnGameObject {

        public FloorPuzzle floorPuzzle;

        public PuzzleState startingState = PuzzleState.Off;
        public StateMachine<PuzzleState> state;
        public ValueDisplay valueIcon;
        [FormerlySerializedAs("powerButton2")]
        public Button powerButton;
        public PowerTrail powerTrailFromPrevPuzzle;
        public PowerTrail powerTrail;
        public PowerTrail powerTrailToNextPuzzle;
        public List<Button> powerButtonsToDisable;
        
        [ShowNativeProperty]
        public int Value => valueIcon.actualValue;
        
        // Animation tweaking
        private const float CORRECT_TO_DEPOWER_DELAY = 1.5f;
        public const int INCORRECT_FLASH_TIMES = 4;
        public const float INCORRECT_FLASH_DURATION = 2f;
        private const float VALUE_ICON_ALPHA_LERP_SPEED = 1f;
        public const float CURRENT_VALUE_ALPHA_LERP_SPEED = 3f;
        public static readonly Color correctColor = new Color(.2f, .8f, .3f);
        public static readonly Color incorrectColor = new Color(.8f, .2f, .3f);
        private readonly Color disabledPowerSourceButtonColor = new Color(0.04f, 0.04f, 0.04f);

        private bool IsFirstPuzzle => powerTrailFromPrevPuzzle == null;
        private bool isLastPuzzle;
        
        // Start is called before the first frame update
        protected override void Start() {
            base.Start();
            floorPuzzle.powerTrailBottomMiddle.pwr.OnDepowerFinish += () => powerTrail.pwr.PowerIsOn = false;
            if (powerTrailFromPrevPuzzle != null) {
                powerTrailFromPrevPuzzle.pwr.OnPowerFinish += () => {
                    if (state == PuzzleState.Off) state.Set(PuzzleState.Idle);
                };
            }
            
            isLastPuzzle = powerTrailToNextPuzzle.name.Contains("PowerTrailToPortalDoor");

            state = this.StateMachine(startingState);
            Invoke(nameof(InitStateMachine), 0.1f);
        }
        
        void InitStateMachine() {
            state.AddStateTransition(PuzzleState.PoweringUp, NextState, powerTrail.Duration + floorPuzzle.powerTrailBottomMiddle.Duration);
            state.AddStateTransition(PuzzleState.Incorrect, PuzzleState.Depowering, INCORRECT_FLASH_DURATION);
            state.AddStateTransition(PuzzleState.Depowering, PuzzleState.Idle, powerTrail.DurationOff + floorPuzzle.powerTrailBottomMiddle.DurationOff);
            state.AddStateTransition(PuzzleState.Correct, PuzzleState.CorrectIdle, CORRECT_TO_DEPOWER_DELAY);

            // Incorrect SFX
            for (int i = 0; i < INCORRECT_FLASH_TIMES; i++) {
                state.AddTrigger(PuzzleState.Incorrect, (INCORRECT_FLASH_DURATION/INCORRECT_FLASH_TIMES) * i, () =>
                    AudioManager.instance.PlayOnGameObject(AudioName.IncorrectAnswer, "", this));
            }

            // Depowering power trails
            state.AddTrigger(PuzzleState.Depowering, 0f, () => {
                powerButton.pwr.PowerIsOn = false;
                floorPuzzle.powerTrailBottomMiddle.pwr.PowerIsOn = false;
            });
            // Powering next puzzle panel
            state.AddTrigger(stateToCheck => stateToCheck is not (PuzzleState.Correct or PuzzleState.CorrectIdle), 0f, () => powerTrailToNextPuzzle.pwr.PowerIsOn = false);
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => {
                if (isLastPuzzle) {
                    floorPuzzle.currentValueShutter.isSetToOpen = true;
                    floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
                }
                powerTrailToNextPuzzle.pwr.PowerIsOn = true;
            });
            
            // Reset state for new puzzle panel
            state.AddTrigger(PuzzleState.CorrectIdle, 0f, () => {
                FloorManager.instance.TurnOffAllPowerSources();
                floorPuzzle.powerTrailBottomMiddle.pwr.PowerIsOn = false;
            });
            
            // Turning off PowerButtons that are disabled for this puzzle
            state.AddTrigger(PuzzleState.Idle, 0f, () => {
                if (state.PrevState == PuzzleState.Off) {
                    valueIcon.SpriteAlpha = 1f;
                    valueIcon.desiredColor = Color.white;
                }
                
                foreach (var button in powerButtonsToDisable) {
                    DisablePowerButton(button);
                }

                foreach (Button button in floorPuzzle.powerSources.Select(ps => ps.powerButton).Except(powerButtonsToDisable)) {
                    EnablePowerButton(button);
                }
            });

            // Colors of value display
            state.AddTrigger(PuzzleState.Correct, 0f, () => {
                AudioManager.instance.Play(AudioName.CorrectAnswer);
                valueIcon.desiredColor = correctColor;
                floorPuzzle.currentValue.desiredColor = correctColor;
            });
            if (isLastPuzzle) {
                powerTrailToNextPuzzle.pwr.OnPowerFinish += () => floorPuzzle.currentValue.desiredColor = floorPuzzle.currentValue.defaultColor;
            }
            state.AddTrigger(PuzzleState.Idle, floorPuzzle.powerTrailTopMiddle.Duration + .125f, () => {
                if (floorPuzzle.currentValue.actualValue == 0) {
                    floorPuzzle.currentValue.desiredColor = floorPuzzle.currentValue.defaultColor;
                }
            });
            
            // Floor 2 has opening and closing shutters
            if (floorPuzzle.floor == FloorManager.Floor.Floor2) {
                powerTrail.pwr.OnPowerBegin += OpenShutters;
                powerTrail.pwr.OnDepowerBegin += CloseShutters;
            }
        }

        void OpenShutters() {
            floorPuzzle.currentValueShutter.isSetToOpen = true;
            floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
        }

        void CloseShutters() {
            if ((isLastPuzzle && (state == PuzzleState.Correct || state == PuzzleState.CorrectIdle)) || (IsFirstPuzzle && state == PuzzleState.Depowering)) return;
            
            floorPuzzle.currentValueShutter.isSetToOpen = false;
            floorPuzzle.currentValueShutter.state.Set(CurrentValueShutter.State.Moving);
        }

        void DisablePowerButton(Button powerButton) {
            if (powerButton == null || powerButton == null || powerButton.interactableObject == null) return;
            
            powerButton.interactableObject.SetAsDisabled("(Disabled)");
            AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, powerButton.ID, powerButton.transform.position);
            powerButton.GetComponent<ButtonColorChange>().startColor = disabledPowerSourceButtonColor;
            foreach (ValueDisplay vd in powerButton.GetComponentsInChildren<ValueDisplay>()) {
                vd.SpriteAlpha = 0;
            }
        }

        void EnablePowerButton(Button powerButton) {
            if (powerButton == null || powerButton.interactableObject == null) return;

            powerButton.interactableObject.SetAsInteractable();
            powerButton.GetComponent<ButtonColorChange>().startColor = Color.white;
            foreach (ValueDisplay vd in powerButton.GetComponentsInChildren<ValueDisplay>()) {
                vd.SpriteAlpha = 1;
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
            valueIcon.SpriteAlpha = targetAlpha;
            switch (state.State) {
                case PuzzleState.Off:
                    valueIcon.SetColorImmediately(Color.black);
                    break;
                case PuzzleState.Idle:
                    if (powerTrail.pwr.PowerIsOn) {
                        state.Set(PuzzleState.PoweringUp);
                        return;
                    }
                    break;
                case PuzzleState.PoweringUp:
                    break;
                case PuzzleState.Incorrect:
                    if (Evaluate()) {
                        AudioManager.instance.GetAudioJob(AudioName.IncorrectAnswer).Stop();
                        state.Set(PuzzleState.Correct);
                        break;
                    }
                    
                    float t = 0.5f + 0.5f*Mathf.Cos(state.Time * INCORRECT_FLASH_TIMES * 2 * Mathf.PI/INCORRECT_FLASH_DURATION + Mathf.PI);
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
                powerButton.interactableObject.SetAsDisabled("(Missing power)");
            }
            else if (state == PuzzleState.Idle) {
                powerButton.interactableObject.SetAsInteractable("Check solution");
            }
            else {
                powerButton.interactableObject.SetAsHidden();
            }
        }
        
        bool Evaluate() {
            return Value == floorPuzzle.currentValue.actualValue;
        }

        public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob audioJob) {
            return transform;
        }

#region Saving

        public override void LoadSave(PuzzlePanelSave save) {
            state.LoadFromSave(save.stateSave);
        }

        [Serializable]
        public class PuzzlePanelSave : SaveObject<PuzzlePanel> {
            public StateMachineSave<PuzzleState> stateSave;
            
            public PuzzlePanelSave(PuzzlePanel script) : base(script) {
                this.stateSave = script.state.ToSave();
            }
        }

#endregion
    }
}