using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(UniqueId))]
public class ControlPrompt : SaveableObject<ControlPrompt, ControlPrompt.ControlPromptSave> {
    public enum State {
        DelayedStart,
        NotYetDisplayed,
        Displaying,
        FinishedDisplaying
    }

    public StateMachine<State> state;

    private static float StartDelay => (Letterboxing.instance.LetterboxingEnabled ? Letterboxing.LETTERBOX_APPEAR_TIME : 0) +
                                       LevelChangeBanner.DISPLAY_TIME +
                                       LevelChangeBanner.FADE_TIME + 2f;

    public TMP_Text label;
    // TODO: Handle controller prompts separately?
    public List<KeyboardPrompt> keyboardPrompts;

    [ShowNonSerializedField]
    private float _imagesAlpha = 0.01f;
    private float imagesAlpha {
        get => _imagesAlpha;
        set {
            foreach (var keyboardPrompt in keyboardPrompts) {
                keyboardPrompt.alphaMultiplier = Mathf.Clamp01(value);
            }

            label.color = label.color.WithAlpha(value);

            _imagesAlpha = value;
        }
    }

    private const float minTimeAfterInputAccepted = 2f;
    private const float maxTimeAfterInputAccepted = 5f;
    private const float alphaLerpTimeIn = 1.25f;
    private const float alphaLerpTimeOut = .75f;

    [ShowNonSerializedField]
    private bool hasProvidedInput = false;

    protected override void OnValidate() {
        base.OnValidate();

        if (keyboardPrompts == null || keyboardPrompts.Count == 0) {
            keyboardPrompts = GetComponentsInChildren<KeyboardPrompt>().ToList();
        }
    }

    protected override void Awake() {
        base.Awake();
        
        state = this.StateMachine(State.NotYetDisplayed);
        
        if (keyboardPrompts == null || keyboardPrompts.Count == 0) {
            keyboardPrompts = GetComponentsInChildren<KeyboardPrompt>().ToList();
        }
        
        state.AddStateTransition(State.DelayedStart, State.Displaying, StartDelay);
    }

    private bool InputIsBeingProvided => keyboardPrompts.Any(prompt => prompt.keybind.Held);
    private bool MinTimeAfterInputHasElapsed => state.timeSinceStateChanged > (alphaLerpTimeIn + minTimeAfterInputAccepted);
    private bool MaxTimeAfterInputHasElapsed => state.timeSinceStateChanged > (alphaLerpTimeIn + maxTimeAfterInputAccepted);
    protected virtual bool CanStopDisplaying => hasProvidedInput && (MaxTimeAfterInputHasElapsed || (MinTimeAfterInputHasElapsed && !InputIsBeingProvided));

    protected virtual void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        switch (state.state) {
            case State.DelayedStart:
            case State.NotYetDisplayed:
                if (imagesAlpha != 0) {
                    imagesAlpha = 0;
                }
                break;
            case State.Displaying:
                if (!hasProvidedInput && InputIsBeingProvided) {
                    hasProvidedInput = true;
                }
                
                if (imagesAlpha < 1) {
                    imagesAlpha = Mathf.Lerp(0f, 1f, state.timeSinceStateChanged / alphaLerpTimeIn);
                }
                else if (CanStopDisplaying) {
                    state.Set(State.FinishedDisplaying);
                }
                break;
            case State.FinishedDisplaying:
                if (imagesAlpha > 0) {
                    imagesAlpha = Mathf.Lerp(1f, 0f, state.timeSinceStateChanged / alphaLerpTimeOut);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Display() {
        if (state == State.NotYetDisplayed) {
            state.Set(State.Displaying);
        }
    }

    public void Hide() {
        if (state == State.Displaying) {
            state.Set(State.FinishedDisplaying);
        }
    }
    
#region Saving
		[Serializable]
		public class ControlPromptSave : SerializableSaveObject<ControlPrompt> {
            private StateMachine<State>.StateMachineSave stateSave;
            
			public ControlPromptSave(ControlPrompt script) : base(script) {
                this.stateSave = script.state.ToSave();
			}

			public override void LoadSave(ControlPrompt script) {
                script.state.LoadFromSave(this.stateSave);
			}
		}
#endregion
}
