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
public class ControlPrompt : SuperspectiveObject<ControlPrompt, ControlPrompt.ControlPromptSave> {
    public enum State : byte {
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
    private float ImagesAlpha {
        get => _imagesAlpha;
        set {
            foreach (var keyboardPrompt in keyboardPrompts) {
                keyboardPrompt.alphaMultiplier = Mathf.Clamp01(value);
            }

            label.color = label.color.WithAlpha(value);

            _imagesAlpha = value;
        }
    }

    private const float MIN_TIME_AFTER_INPUT_ACCEPTED = 2f;
    private const float MAX_TIME_AFTER_INPUT_ACCEPTED = 5f;
    private const float ALPHA_LERP_TIME_IN = 1.25f;
    private const float ALHPA_LERP_TIME_OUT = .75f;

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
    private bool MinTimeAfterInputHasElapsed => state.Time > (ALPHA_LERP_TIME_IN + MIN_TIME_AFTER_INPUT_ACCEPTED);
    private bool MaxTimeAfterInputHasElapsed => state.Time > (ALPHA_LERP_TIME_IN + MAX_TIME_AFTER_INPUT_ACCEPTED);
    protected virtual bool CanStopDisplaying => hasProvidedInput && (MaxTimeAfterInputHasElapsed || (MinTimeAfterInputHasElapsed && !InputIsBeingProvided));

    protected virtual void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        switch (state.State) {
            case State.DelayedStart:
            case State.NotYetDisplayed:
                if (ImagesAlpha != 0) {
                    ImagesAlpha = 0;
                }
                break;
            case State.Displaying:
                if (!hasProvidedInput && InputIsBeingProvided) {
                    hasProvidedInput = true;
                }
                
                if (ImagesAlpha < 1) {
                    ImagesAlpha = Mathf.Lerp(0f, 1f, state.Time / ALPHA_LERP_TIME_IN);
                }
                else if (CanStopDisplaying) {
                    state.Set(State.FinishedDisplaying);
                }
                break;
            case State.FinishedDisplaying:
                if (ImagesAlpha > 0) {
                    ImagesAlpha = Mathf.Lerp(1f, 0f, state.Time / ALHPA_LERP_TIME_OUT);
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

    public override void LoadSave(ControlPromptSave save) { }

    [Serializable]
	public class ControlPromptSave : SaveObject<ControlPrompt> {
		public ControlPromptSave(ControlPrompt script) : base(script) { }
	}
#endregion
}
