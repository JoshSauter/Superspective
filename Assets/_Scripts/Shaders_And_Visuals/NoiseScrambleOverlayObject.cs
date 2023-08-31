using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Saving;
using StateUtils;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class NoiseScrambleOverlayObject : SaveableObject<NoiseScrambleOverlayObject, NoiseScrambleOverlayObject.NoiseScrambleOverlayObjectSave> {
	public enum ScramblerState {
		Off,
		On
	}

	public StateMachine<ScramblerState> scramblerState = new StateMachine<ScramblerState>(ScramblerState.Off);
	private InteractableObject interactableObject;

	private void RegisterScrambler() {
		if (!NoiseScrambleOverlay.scramblers.ContainsKey(ID)) {
			NoiseScrambleOverlay.scramblers.Add(ID, this);
		}
	}

	protected override void Awake() {
		base.Awake();
		// Temp for debug:
		interactableObject = gameObject.GetOrAddComponent<InteractableObject>();
		interactableObject.OnLeftMouseButtonDown += ToggleOnOff;
	}

	protected override void Init() {
		base.Init();
		
		RegisterScrambler();
	}

	void ToggleOnOff() {
		scramblerState.Set((ScramblerState)(1 - scramblerState));
	}
    
#region Saving
		[Serializable]
		public class NoiseScrambleOverlayObjectSave : SerializableSaveObject<NoiseScrambleOverlayObject> {
			public StateMachine<ScramblerState>.StateMachineSave stateSave;

			public NoiseScrambleOverlayObjectSave(NoiseScrambleOverlayObject script) : base(script) {
				stateSave = script.scramblerState.ToSave();
			}

			public override void LoadSave(NoiseScrambleOverlayObject script) {
				script.scramblerState.LoadFromSave(stateSave);
			}
		}
#endregion
}
