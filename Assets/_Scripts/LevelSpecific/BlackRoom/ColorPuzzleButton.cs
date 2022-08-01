using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using LevelSpecific.BlackRoom;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId), typeof(InteractableObject))]
public class ColorPuzzleButton : SaveableObject<ColorPuzzleButton, ColorPuzzleButton.ColorPuzzleButtonSave> {
	public ColorPuzzle colorPuzzle;
	private Renderer renderer;
	private InteractableObject interact;
	private BlackRoomMainConsole mainConsole => BlackRoomMainConsole.instance;
	private ColorPuzzleManager colorPuzzleManager => ColorPuzzleManager.instance;
	bool isLastPuzzle => colorPuzzleManager.activePuzzle == colorPuzzleManager.numPuzzles - 1;
	
	// Animation properties
	private const float lerpSpeed = 4f;
	private string emissionProp => SuperspectiveRenderer.emissionColor;
	private static readonly Color offColor = Color.gray;
	
	private static readonly Color onColor = Color.white;
	[ColorUsage(false, true)]
	private static readonly Color onEmission = new Color(1.2f, 1.2f, 1.2f);
	
	private static readonly Color incorrectColor = new Color(.8f, .2f, .3f);
	[ColorUsage(false, true)]
	private static readonly Color incorrectEmission = new Color(1.4f, .05f, .055f);
	
	public static readonly Color correctColor = new Color(.2f, .8f, .3f);
	[ColorUsage(false, true)]
	private static readonly Color correctEmission = new Color(.05f, 1.4f, .055f);
	
	private const int incorrectFlashTimes = 3;
	private const float incorrectFlashDuration = 2.4f;
	private const float correctTime = 1.5f;

	private bool hasBeenSolvedBefore = false;
	
    public enum State {
        Off,
        On,
        Incorrect,
        Correct
    }
    public StateMachine<State> state = new StateMachine<State>(State.Off);

    protected override void Awake() {
	    base.Awake();

	    renderer = GetComponent<Renderer>();
    }

    protected override void Start() {
        base.Start();

        interact = GetComponent<InteractableObject>();
        interact.SetAsHidden();
        interact.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
        
        state.AddStateTransition(State.Incorrect, State.On, incorrectFlashDuration);
        
        state.AddTrigger(State.Correct, () => {
	        AudioManager.instance.Play(AudioName.CorrectAnswer, "CorrectAnswer", true);
	        interact.SetAsHidden();
	        
	        if (isLastPuzzle) {
		        EndOfPlaytestMessage.instance.state.Set(EndOfPlaytestMessage.State.BackgroundFadingIn);
	        }
        });
        state.AddTrigger(State.On, () => interact.SetAsInteractable());
        state.AddTrigger(State.Incorrect, () => {
	        interact.SetAsHidden();
	        colorPuzzleManager.FlashIncorrect();
        });
        // Incorrect SFX
        for (int i = 0; i < incorrectFlashTimes; i++) {
	        state.AddTrigger(State.Incorrect, (incorrectFlashDuration/incorrectFlashTimes) * i, () =>
		        AudioManager.instance.Play(AudioName.IncorrectAnswer, "IncorrectAnswer"));
        }
        
        // After showing correct for a short time, fire the laser (or just move the active puzzle to next)
        state.AddTrigger(State.Correct, correctTime, () => {
	        if (!isLastPuzzle) {
		        if (!hasBeenSolvedBefore) {
			        hasBeenSolvedBefore = true;
			        mainConsole.dissolveLaser.FireAt(
				        mainConsole.puzzleSelectCovers[colorPuzzleManager.activePuzzle + 1]);
		        }
		        else {
			        mainConsole.puzzleSelectButtons[mainConsole.colorPuzzleManager.activePuzzle + 1].state
				        .Set(State.On);
		        }
	        }
	        else if (!hasBeenSolvedBefore) {
		        hasBeenSolvedBefore = true;
	        }
        });
        
        state.AddTrigger(State.Off, () => {
	        if (state.prevState == State.Correct) {
		        interact.SetAsInteractable();
	        }
        });
    }

    void OnLeftMouseButtonDown() {
	    if (state == State.Off) {
		    state.Set(State.On);
	    }
	    else if (state == State.On) {
		    state.Set(colorPuzzle.solved ? State.Correct : State.Incorrect);
	    }
    }
    
    void Update() {
	    Color curColor = renderer.GetColorFromRenderer();
	    Color curEmission = renderer.GetHDRColorFromRenderer(emissionProp);
	    
	    switch (state.state) {
		    case State.Off:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, offColor, Time.deltaTime * lerpSpeed));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, Color.clear, Time.deltaTime * lerpSpeed), emissionProp);
			    break;
		    case State.On:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, onColor, Time.deltaTime * lerpSpeed));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, onEmission, Time.deltaTime * lerpSpeed), emissionProp);
			    if (this.InstaSolvePuzzle()) {
				    state.Set(State.Correct);
			    }
			    break;
		    case State.Incorrect:
			    float t = 0.5f + 0.5f*Mathf.Cos(state.timeSinceStateChanged * incorrectFlashTimes * 2 * Mathf.PI/incorrectFlashDuration + Mathf.PI);
			    renderer.SetColorForRenderer(Color.Lerp(onColor, incorrectColor, t));
			    renderer.SetHDRColorForRenderer(Color.Lerp(onEmission, incorrectEmission, t), emissionProp);
			    break;
		    case State.Correct:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, correctColor, Time.deltaTime * lerpSpeed));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, correctEmission, Time.deltaTime * lerpSpeed), emissionProp);
			    break;
		    default:
			    throw new ArgumentOutOfRangeException();
	    }
    }
    
#region Saving
		[Serializable]
		public class ColorPuzzleButtonSave : SerializableSaveObject<ColorPuzzleButton> {
            private StateMachine<State>.StateMachineSave stateSave;
            private bool hasBeenSolved;
            
			public ColorPuzzleButtonSave(ColorPuzzleButton script) : base(script) {
                this.stateSave = script.state.ToSave();
                this.hasBeenSolved = script.hasBeenSolvedBefore;
			}

			public override void LoadSave(ColorPuzzleButton script) {
                script.state.FromSave(this.stateSave);
                script.hasBeenSolvedBefore = this.hasBeenSolved;
			}
		}
#endregion
}
