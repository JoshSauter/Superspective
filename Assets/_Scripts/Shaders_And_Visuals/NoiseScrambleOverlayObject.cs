using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class NoiseScrambleOverlayObject : SuperspectiveObject<NoiseScrambleOverlayObject, NoiseScrambleOverlayObject.NoiseScrambleOverlayObjectSave> {
	public enum ScramblerState : byte {
		Off,
		On
	}

	public StateMachine<ScramblerState> scramblerState;
	private InteractableObject interactableObject;

	private void RegisterScrambler() {
		if (!NoiseScrambleOverlay.scramblers.ContainsKey(ID)) {
			NoiseScrambleOverlay.scramblers.Add(ID, this);
		}
	}

	protected override void Awake() {
		base.Awake();

		scramblerState = this.StateMachine(ScramblerState.Off);
		
		// Temp for debug:
		interactableObject = gameObject.GetOrAddComponent<InteractableObject>();
		interactableObject.OnLeftMouseButtonDown += ToggleOnOff;
	}

	protected override void Init() {
		base.Init();
		
		RegisterScrambler();
	}

	void Update() {
		bool shouldBeInteractable = DEBUG && DebugInput.IsDebugBuild;
		if (interactableObject.state == InteractableObject.InteractableState.Interactable != shouldBeInteractable) {
			if (shouldBeInteractable) {
				interactableObject.SetAsInteractable();
			}
			else {
				interactableObject.SetAsHidden();
			}
		}
	}

	void ToggleOnOff() {
		scramblerState.Set((ScramblerState)(1 - scramblerState));
	}

	public void TurnOn() {
		scramblerState.Set(ScramblerState.On);
	}
	
	public void TurnOff() {
		scramblerState.Set(ScramblerState.Off);
	}
    
#region Saving

	public override void LoadSave(NoiseScrambleOverlayObjectSave save) {
		scramblerState.LoadFromSave(save.stateSave);
		
		RegisterScrambler();
	}

	[Serializable]
	public class NoiseScrambleOverlayObjectSave : SaveObject<NoiseScrambleOverlayObject> {
		public StateMachine<ScramblerState>.StateMachineSave stateSave;

		public NoiseScrambleOverlayObjectSave(NoiseScrambleOverlayObject script) : base(script) {
			stateSave = script.scramblerState.ToSave();
		}
	}
#endregion
}
