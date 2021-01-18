using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class Button : MonoBehaviour, SaveableObject {
    public enum State {
        ButtonDepressed,
        ButtonPressing,
        ButtonPressed,
        ButtonDepressing
    }

    public float timeSinceStateChange;

    public InteractableObject interactableObject;

    public AnimationCurve buttonPressCurve;
    public AnimationCurve buttonDepressCurve;
    public float timeToPressButton = 1f;
    public float timeToDepressButton = 0.5f;
    public float depressDistance = 1f;

    public bool depressAfterPress;
    public float timeBetweenPressEndDepressStart = 0.5f;
    UniqueId _id;
    State _state = State.ButtonDepressed;
    protected Vector3 depressedPos;
    protected Vector3 pressedPos;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    public State state {
        get => _state;
        set {
            if (_state == value) return;
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

    public bool buttonPressed => state == State.ButtonPressed;

    public virtual void Awake() {
        interactableObject = GetComponent<InteractableObject>();
        if (interactableObject == null) interactableObject = gameObject.AddComponent<InteractableObject>();
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

    void Update() {
        UpdateButton();
    }

    public virtual void OnLeftMouseButton() {
        PressButton();
    }

    protected virtual void UpdateButton() {
        timeSinceStateChange += Time.deltaTime;
        switch (state) {
            case State.ButtonDepressed:
                break;
            case State.ButtonPressed:
                if (depressAfterPress && timeSinceStateChange > timeBetweenPressEndDepressStart)
                    state = State.ButtonDepressing;
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
        if (state == State.ButtonPressed)
            state = State.ButtonDepressing;
        else if (state == State.ButtonDepressed) state = State.ButtonPressing;
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

#region events
    public delegate void ButtonAction(Button button);

    public event ButtonAction OnButtonPressBegin;
    public event ButtonAction OnButtonPressFinish;
    public event ButtonAction OnButtonDepressBegin;
    public event ButtonAction OnButtonDepressFinish;
#endregion

#region Saving
    public bool SkipSave { get; set; }

    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public string ID => $"Button_{id.uniqueId}";

    [Serializable]
    class ButtonSave {
        public float timeSinceStateChange;
        SerializableAnimationCurve buttonDepressCurve;
        SerializableAnimationCurve buttonPressCurve;

        bool depressAfterPress;
        float depressDistance;
        SerializableVector3 depressedPos;
        SerializableVector3 pressedPos;
        int state;
        float timeBetweenPressEndDepressStart;
        float timeToDepressButton;
        float timeToPressButton;

        public ButtonSave(Button button) {
            state = (int) button.state;
            timeSinceStateChange = button.timeSinceStateChange;
            depressedPos = button.depressedPos;
            pressedPos = button.pressedPos;
            buttonPressCurve = button.buttonPressCurve;
            buttonDepressCurve = button.buttonDepressCurve;
            timeToPressButton = button.timeToPressButton;
            timeToDepressButton = button.timeToDepressButton;
            depressDistance = button.depressDistance;

            depressAfterPress = button.depressAfterPress;
            timeBetweenPressEndDepressStart = button.timeBetweenPressEndDepressStart;
        }

        public void LoadSave(Button button) {
            button.state = (State) state;
            button.timeSinceStateChange = timeSinceStateChange;
            button.depressedPos = depressedPos;
            button.pressedPos = pressedPos;
            button.buttonPressCurve = buttonPressCurve;
            button.buttonDepressCurve = buttonDepressCurve;
            button.timeToPressButton = timeToPressButton;
            button.timeToDepressButton = timeToDepressButton;
            button.depressDistance = depressDistance;

            button.depressAfterPress = depressAfterPress;
            button.timeBetweenPressEndDepressStart = timeBetweenPressEndDepressStart;
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