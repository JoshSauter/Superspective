using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using DissolveObjects;
using LevelSpecific.BlackRoom;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId), typeof(InteractableObject))]
public class ColorPuzzleButton : SuperspectiveObject<ColorPuzzleButton, ColorPuzzleButton.ColorPuzzleButtonSave> {
	public ColorPuzzle colorPuzzle;
	private new Renderer renderer;
	private InteractableObject interact;
	private BlackRoomMainConsole MainConsole => BlackRoomMainConsole.instance;
	private ColorPuzzleManager ColorPuzzleManager => ColorPuzzleManager.instance;
	bool IsLastPuzzle => ColorPuzzleManager.ActivePuzzle == ColorPuzzleManager.NumPuzzles - 1;
	
	// Animation properties
	private const float LERP_SPEED = 4f;
	private const int INCORRECT_FLASH_TIMES = 3;
	private const float INCORRECT_FLASH_DURATION = 2.4f;
	private const float CORRECT_TIME = 1.5f;
	private string EmissionProp => SuperspectiveRenderer.emissionColor;
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
	
    public enum State : byte {
        Off,
        On,
        Incorrect,
        Correct
    }
    public StateMachine<State> state;

    protected override void Awake() {
	    base.Awake();
	    
	    state = this.StateMachine(State.Off);

	    renderer = GetComponent<Renderer>();
    }

    protected override void Start() {
        base.Start();

        interact = GetComponent<InteractableObject>();
        interact.SetAsHidden();
        interact.OnLeftMouseButtonDown += OnLeftMouseButtonDown;
        
        state.AddStateTransition(State.Incorrect, State.On, INCORRECT_FLASH_DURATION);
        
        state.AddTrigger(State.Correct, () => {
	        AudioManager.instance.Play(AudioName.CorrectAnswer);
	        interact.SetAsHidden();
	        
	        if (IsLastPuzzle) {
		        EndOfPlaytestMessage.instance.state.Set(EndOfPlaytestMessage.State.BackgroundFadingIn);
	        }
        });
        state.AddTrigger(State.On, () => interact.SetAsInteractable("Check solution"));
        state.AddTrigger(State.Incorrect, () => {
	        interact.SetAsHidden();
	        ColorPuzzleManager.FlashIncorrect();
        });
        // Incorrect SFX
        for (int i = 0; i < INCORRECT_FLASH_TIMES; i++) {
	        state.AddTrigger(State.Incorrect, (INCORRECT_FLASH_DURATION/INCORRECT_FLASH_TIMES) * i, () =>
		        AudioManager.instance.Play(AudioName.IncorrectAnswer));
        }
        
        // After showing correct for a short time, fire the laser (or just move the active puzzle to next)
        state.AddTrigger(State.Correct, CORRECT_TIME, () => {
	        if (!IsLastPuzzle) {
		        if (MainConsole.puzzleSelectCovers[ColorPuzzleManager.ActivePuzzle + 1].stateMachine == DissolveObject.State.Materialized) {
			        MainConsole.dissolveLaser.FireAt(
				        MainConsole.puzzleSelectCovers[ColorPuzzleManager.ActivePuzzle + 1]);
		        }
		        else {
			        MainConsole.puzzleSelectButtons[MainConsole.colorPuzzleManager.ActivePuzzle + 1].state
				        .Set(State.On);
		        }
	        }
        });
        
        state.AddTrigger(State.Off, () => {
	        if (state.PrevState == State.Correct) {
		        interact.SetAsInteractable("Switch puzzle");
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
	    Color curEmission = renderer.GetHDRColorFromRenderer(EmissionProp);
	    
	    switch (state.State) {
		    case State.Off:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, offColor, Time.deltaTime * LERP_SPEED));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, Color.clear, Time.deltaTime * LERP_SPEED), EmissionProp);
			    break;
		    case State.On:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, onColor, Time.deltaTime * LERP_SPEED));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, onEmission, Time.deltaTime * LERP_SPEED), EmissionProp);
			    if (this.InstaSolvePuzzle()) {
				    state.Set(State.Correct);
			    }
			    break;
		    case State.Incorrect:
			    float t = 0.5f + 0.5f*Mathf.Cos(state.Time * INCORRECT_FLASH_TIMES * 2 * Mathf.PI/INCORRECT_FLASH_DURATION + Mathf.PI);
			    renderer.SetColorForRenderer(Color.Lerp(onColor, incorrectColor, t));
			    renderer.SetHDRColorForRenderer(Color.Lerp(onEmission, incorrectEmission, t), EmissionProp);
			    break;
		    case State.Correct:
			    renderer.SetColorForRenderer(Color.Lerp(curColor, correctColor, Time.deltaTime * LERP_SPEED));
			    renderer.SetHDRColorForRenderer(Color.Lerp(curEmission, correctEmission, Time.deltaTime * LERP_SPEED), EmissionProp);
			    break;
		    default:
			    throw new ArgumentOutOfRangeException();
	    }
    }
    
#region Saving

	public override void LoadSave(ColorPuzzleButtonSave save) {
		state.LoadFromSave(save.stateSave);
	}

	[Serializable]
	public class ColorPuzzleButtonSave : SaveObject<ColorPuzzleButton> {
        public StateMachine<State>.StateMachineSave stateSave;
        
		public ColorPuzzleButtonSave(ColorPuzzleButton script) : base(script) {
            this.stateSave = script.state.ToSave();
		}
	}
#endregion
}
