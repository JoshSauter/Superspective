using Saving;
using SerializableClasses;
using System;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class Button : MonoBehaviour, SaveableObject {
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
		ButtonDepressed,
		ButtonPressing,
		ButtonPressed,
		ButtonDepressing
	}
	private State _state = State.ButtonDepressed;
	public State state {
		get { return _state; }
		set {
			if (_state == value) {
				return;
			}
			switch (value) {
				case State.ButtonDepressed:
					OnButtonDepressFinish?.Invoke(this);
					break;
				case State.ButtonPressing:
					OnButtonPressBegin?.Invoke(this);
					break;
				case State.ButtonPressed:
					OnButtonPressFinish?.Invoke(this);
					break;
				case State.ButtonDepressing:
					OnButtonDepressBegin?.Invoke(this);
					break;
			}
			timeSinceStateChange = 0f;
			_state = value;
		}
	}
	public float timeSinceStateChange = 0f;
	protected Vector3 depressedPos;
	protected Vector3 pressedPos;

	public InteractableObject interactableObject;
#region events
	public delegate void ButtonAction(Button button);
	public event ButtonAction OnButtonPressBegin;
	public event ButtonAction OnButtonPressFinish;
	public event ButtonAction OnButtonDepressBegin;
	public event ButtonAction OnButtonDepressFinish;
#endregion

	public AnimationCurve buttonPressCurve;
	public AnimationCurve buttonDepressCurve;
	public float timeToPressButton = 1f;
	public float timeToDepressButton = 0.5f;
	public float depressDistance = 1f;
	public bool buttonPressed => state == State.ButtonPressed;

	public bool depressAfterPress = false;
	public float timeBetweenPressEndDepressStart = 0.5f;

	public virtual void OnLeftMouseButton() { PressButton(); }

	public virtual void Awake() {
		interactableObject = GetComponent<InteractableObject>();
		if (interactableObject == null) {
			interactableObject = gameObject.AddComponent<InteractableObject>();
		}
		interactableObject.OnLeftMouseButton += OnLeftMouseButton;

		Vector3 startPos = transform.position;
		if (state == State.ButtonDepressed) {
			depressedPos = startPos;
			pressedPos = depressedPos + transform.up * depressDistance;
		}
		else if (state == State.ButtonPressed) {
			pressedPos = startPos;
			depressedPos = pressedPos - transform.up * depressDistance;
		}
	}

	private void Update() {
		UpdateButton();
	}

	protected virtual void UpdateButton() {
		timeSinceStateChange += Time.deltaTime;
		switch (state) {
			case State.ButtonDepressed:
				break;
			case State.ButtonPressed:
				if (depressAfterPress && timeSinceStateChange > timeBetweenPressEndDepressStart) {
					state = State.ButtonDepressing;
				}
				break;
			case State.ButtonPressing:
				if (timeSinceStateChange < timeToPressButton) {
					float t = timeSinceStateChange / timeToPressButton;

					transform.position = Vector3.Lerp(depressedPos, pressedPos, buttonPressCurve.Evaluate(t));
				}
				else {
					transform.position = pressedPos;
					state = State.ButtonPressed;
				}
				break;
			case State.ButtonDepressing:
				if (timeSinceStateChange < timeToDepressButton) {
					float t = timeSinceStateChange / timeToDepressButton;

					transform.position = Vector3.Lerp(pressedPos, depressedPos, buttonDepressCurve.Evaluate(t));
				}
				else {
					transform.position = pressedPos;
					state = State.ButtonDepressed;
				}
				break;
		}
	}

	public void PressButton() {
		if (state == State.ButtonPressed) {
			state = State.ButtonDepressing;
		}
		else if (state == State.ButtonDepressed) {
			state = State.ButtonPressing;
		}
	}

	protected void TriggerButtonPressBeginEvents() {
		if (OnButtonPressBegin != null) OnButtonPressBegin(this);
	}

	protected void TriggerButtonPressFinishEvents() {
		if (OnButtonPressFinish != null) OnButtonPressFinish(this);
	}

	protected void TriggerButtonDepressBeginEvents() {
		if (OnButtonDepressBegin != null) OnButtonDepressBegin(this);
	}

	protected void TriggerButtonDepressFinishEvents() {
		if (OnButtonDepressFinish != null) OnButtonDepressFinish(this);
	}

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"Button_{id.uniqueId}";

	[Serializable]
	class ButtonSave {
		int state;
		public float timeSinceStateChange;
		SerializableVector3 depressedPos;
		SerializableVector3 pressedPos;
		SerializableAnimationCurve buttonPressCurve;
		SerializableAnimationCurve buttonDepressCurve;
		float timeToPressButton;
		float timeToDepressButton;
		float depressDistance;

		bool depressAfterPress;
		float timeBetweenPressEndDepressStart;

		public ButtonSave(Button button) {
			this.state = (int)button.state;
			this.timeSinceStateChange = button.timeSinceStateChange;
			this.depressedPos = button.depressedPos;
			this.pressedPos = button.pressedPos;
			this.buttonPressCurve = button.buttonPressCurve;
			this.buttonDepressCurve = button.buttonDepressCurve;
			this.timeToPressButton = button.timeToPressButton;
			this.timeToDepressButton = button.timeToDepressButton;
			this.depressDistance = button.depressDistance;

			this.depressAfterPress = button.depressAfterPress;
			this.timeBetweenPressEndDepressStart = button.timeBetweenPressEndDepressStart;
		}

		public void LoadSave(Button button) {
			button.state = (State)this.state;
			button.timeSinceStateChange = this.timeSinceStateChange;
			button.depressedPos = this.depressedPos;
			button.pressedPos = this.pressedPos;
			button.buttonPressCurve = this.buttonPressCurve;
			button.buttonDepressCurve = this.buttonDepressCurve;
			button.timeToPressButton = this.timeToPressButton;
			button.timeToDepressButton = this.timeToDepressButton;
			button.depressDistance = this.depressDistance;

			button.depressAfterPress = this.depressAfterPress;
			button.timeBetweenPressEndDepressStart = this.timeBetweenPressEndDepressStart;
		}
	}

	public object GetSaveObject() {
		return new ButtonSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		ButtonSave save = savedObject as ButtonSave;

		save.LoadSave(this);
	}
	#endregion
}
