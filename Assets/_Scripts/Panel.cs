using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Audio;
using Saving;
using System;
using SerializableClasses;

[RequireComponent(typeof(UniqueId))]
public class Panel : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public enum State {
		Deactivated,
		Activating,
		Activated,
		Deactivating
	}
	private State _state;
	public State state {
		get { return _state; }
		set {
			if (_state == value) {
				return;
			}
			timeSinceStateChange = 0f;
			switch (value) {
				case State.Deactivated:
					OnPanelDeactivateFinish?.Invoke();
					break;
				case State.Activating:
					OnPanelActivateBegin?.Invoke();
					startColor = thisRenderer.GetMainColor();
					endColor = gemColor;
					break;
				case State.Activated:
					OnPanelActivateFinish?.Invoke();
					break;
				case State.Deactivating:
					OnPanelDeactivateBegin?.Invoke();
					startColor = gemColor;
					endColor = thisRenderer.GetMainColor();
					break;
				default:
					break;
			}

			_state = value;
		}
	}
	float timeSinceStateChange = 0f;
	EpitaphRenderer thisRenderer;
	public Color gemColor;
	public Button gemButton;

	Color startColor, endColor;
	public float colorLerpTime = .75f;

	public bool activated => state == State.Activated || state == State.Activating;

	// Sound settings
	bool soundActivated = false;
	readonly float minPitch = 0.5f;
	readonly float maxPitch = 1f;
	readonly float minVolume = 0.25f;
	readonly float maxVolume = 1f;
	public SoundEffect electricalHumSound;

#region events
	public delegate void PanelAction();
	public event PanelAction OnPanelActivateBegin;
	public event PanelAction OnPanelActivateFinish;
	public event PanelAction OnPanelDeactivateBegin;
	public event PanelAction OnPanelDeactivateFinish;
#endregion

	virtual protected void Awake() {
		// Set up references
		thisRenderer = gameObject.GetComponent<EpitaphRenderer>();
		if (thisRenderer == null) {
			thisRenderer = gameObject.AddComponent<EpitaphRenderer>();
		}

		gemButton = GetComponentInChildren<Button>();
		EpitaphRenderer gemButtonRenderer = gemButton.GetComponent<EpitaphRenderer>();
		if (gemButtonRenderer == null) {
			gemButtonRenderer = gemButton.gameObject.AddComponent<EpitaphRenderer>();
		}
		gemColor = gemButtonRenderer.GetMainColor();
	}

	// Use this for initialization
	virtual protected void Start () {
		gemButton.OnButtonPressFinish += (ctx) => PanelActivate();
		gemButton.OnButtonDepressBegin += (ctx) => PanelDeactivate();

		gemButton.OnButtonPressBegin += (ctx) => TurnOnSounds();
		gemButton.OnButtonDepressBegin += (ctx) => TurnOffSounds();

		electricalHumSound.audioSource.pitch = minPitch;
		electricalHumSound.audioSource.volume = minVolume;
	}

	private void Update() {
		UpdatePanel();
		UpdateSound();
	}

	void UpdatePanel() {
		timeSinceStateChange += Time.deltaTime;
		float t = timeSinceStateChange / colorLerpTime;
		Color curColor = Color.Lerp(startColor, endColor, t);
		switch (state) {
			case State.Deactivated:
				break;
			case State.Activating:
				if (timeSinceStateChange < colorLerpTime) {
					thisRenderer.SetMainColor(curColor);
				}
				else {
					thisRenderer.SetMainColor(endColor);
					state = State.Activated;
				}
				break;
			case State.Activated:
				break;
			case State.Deactivating:
				if (timeSinceStateChange < colorLerpTime) {
					thisRenderer.SetMainColor(curColor);
				}
				else {
					thisRenderer.SetMainColor(endColor);
					state = State.Deactivated;
				}
				break;
			default:
				break;
		}
	}

	void TurnOnSounds() {
		soundActivated = true;
	}

	void TurnOffSounds() {
		soundActivated = false;
	}

	void UpdateSound() {
		if (soundActivated && electricalHumSound.audioSource.volume < maxVolume) {
			float soundLerpSpeedOn = 1f;
			float newPitch = Mathf.Clamp(electricalHumSound.audioSource.pitch + Time.deltaTime * soundLerpSpeedOn, minPitch, maxPitch);
			float newVolume = Mathf.Clamp(electricalHumSound.audioSource.volume + Time.deltaTime * soundLerpSpeedOn, minVolume, maxVolume);

			electricalHumSound.audioSource.pitch = newPitch;
			electricalHumSound.audioSource.volume = newVolume;
		}

		if (!soundActivated && electricalHumSound.audioSource.volume > minVolume) {
			float soundLerpSpeedOff = .333f;
			float newPitch = Mathf.Clamp(electricalHumSound.audioSource.pitch - Time.deltaTime * soundLerpSpeedOff, minPitch, maxPitch);
			float newVolume = Mathf.Clamp(electricalHumSound.audioSource.volume - Time.deltaTime * soundLerpSpeedOff, minVolume, maxVolume);

			electricalHumSound.audioSource.pitch = newPitch;
			electricalHumSound.audioSource.volume = newVolume;
		}
	}

	virtual protected void PanelActivate() {
		if (state == State.Deactivated) {
			state = State.Activating;
		}
	}

	virtual protected void PanelDeactivate() {
		if (state == State.Activated) {
			state = State.Deactivating;
		}
	}

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"Panel_{id.uniqueId}";

	[Serializable]
	class PanelSave {
		State state;
		float timeSinceStateChange;
		SerializableColor gemColor;
		SerializableColor startColor;
		SerializableColor endColor;
		float colorLerpTime;
		bool soundActivated;

		public PanelSave(Panel script) {
			this.state = script.state;
			this.timeSinceStateChange = script.timeSinceStateChange;
			this.gemColor = script.gemColor;
			this.startColor = script.startColor;
			this.endColor = script.endColor;
			this.colorLerpTime = script.colorLerpTime;
			this.soundActivated = script.soundActivated;
		}

		public void LoadSave(Panel script) {
			script.state = this.state;
			script.timeSinceStateChange = this.timeSinceStateChange;
			script.gemColor = this.gemColor;
			script.startColor = this.startColor;
			script.endColor = this.endColor;
			script.colorLerpTime = this.colorLerpTime;
			script.soundActivated = this.soundActivated;
		}
	}

	public object GetSaveObject() {
		return new PanelSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		PanelSave save = savedObject as PanelSave;

		save.LoadSave(this);
	}
	#endregion
}
