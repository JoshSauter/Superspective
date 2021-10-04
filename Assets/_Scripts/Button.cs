using System;
using Saving;
using SerializableClasses;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
public class Button : SaveableObject<Button, Button.ButtonSave> {
    public enum State {
        ButtonUnpressed,
        ButtonPressing,
        ButtonPressed,
        ButtonUnpressing
    }

    public float timeSinceStateChange;

    public bool oneTimeButton = false;
    public InteractableObject interactableObject;

    public AnimationCurve buttonPressCurve;
    [FormerlySerializedAs("buttonDepressCurve")]
    public AnimationCurve buttonUnpressCurve;
    public float timeToPressButton = 1f;
    [FormerlySerializedAs("timeToDepressButton")]
    public float timeToUnpressButton = 0.5f;
    [FormerlySerializedAs("depressDistance")]
    public float pressDistance = 1f;

    [FormerlySerializedAs("depressAfterPress")]
    public bool unpressAfterPress;
    public float timeBetweenPressEndDepressStart = 0.5f;
    State _state = State.ButtonUnpressed;
    
#region events
    public delegate void ButtonAction(Button button);

    public event ButtonAction OnButtonPressBegin;
    public event ButtonAction OnButtonPressFinish;
    public event ButtonAction OnButtonUnpressBegin;
    public event ButtonAction OnButtonUnpressFinish;
#endregion

    public State state {
        get => _state;
        set {
            if (_state == value) return;
            switch (value) {
                case State.ButtonUnpressed:
                    OnButtonUnpressFinish?.Invoke(this);
                    break;
                case State.ButtonPressing:
                    OnButtonPressBegin?.Invoke(this);
                    break;
                case State.ButtonPressed:
                    OnButtonPressFinish?.Invoke(this);
                    break;
                case State.ButtonUnpressing:
                    OnButtonUnpressBegin?.Invoke(this);
                    break;
            }

            timeSinceStateChange = 0f;
            _state = value;
        }
    }

    float distanceCurrentlyPressed = 0f;

    public bool buttonPressed => state == State.ButtonPressed;

    protected override void Awake() {
        base.Awake();
        interactableObject = GetComponent<InteractableObject>();
        if (interactableObject == null) interactableObject = gameObject.AddComponent<InteractableObject>();
        interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

        if (state == State.ButtonPressed) {
            distanceCurrentlyPressed = pressDistance;
        }
    }

    void Update() {
        UpdateState();
        timeSinceStateChange += Time.deltaTime;
        UpdateButtonPosition();
    }

    protected virtual void UpdateState() {
        switch (state) {
            case State.ButtonUnpressed:
                break;
            case State.ButtonPressing:
                if (timeSinceStateChange > timeToPressButton) {
                    state = State.ButtonPressed;
                }
                break;
            case State.ButtonPressed:
                if (unpressAfterPress && timeSinceStateChange > timeBetweenPressEndDepressStart) {
                    state = State.ButtonUnpressing;
                }
                break;
            case State.ButtonUnpressing:
                if (timeSinceStateChange > timeToUnpressButton) {
                    state = State.ButtonUnpressed;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void OnLeftMouseButtonDown() {
        PressButton();
    }

    protected virtual void UpdateButtonPosition() {
        if (pressDistance <= 0) return;
        float t = timeSinceStateChange / timeToPressButton;
        switch (state) {
            case State.ButtonUnpressed:
            case State.ButtonPressed:
                break;
            case State.ButtonPressing:
                if (timeSinceStateChange < timeToPressButton) {
                    float delta = Time.deltaTime * (pressDistance / timeToPressButton);
                    distanceCurrentlyPressed += delta;
                    transform.position += delta * transform.up;
                }
                else {
                    transform.position += (pressDistance - distanceCurrentlyPressed) * transform.up;
                    distanceCurrentlyPressed = pressDistance;
                }
                break;
            case State.ButtonUnpressing:
                if (timeSinceStateChange < timeToUnpressButton) {
                    float delta = Time.deltaTime * (pressDistance / timeToUnpressButton);
                    distanceCurrentlyPressed -= delta;
                    transform.position -= delta * transform.up;
                }
                else {
                    transform.position -= (distanceCurrentlyPressed) * transform.up;
                    distanceCurrentlyPressed = 0f;
                }
                break;
        }
    }

    public void PressButton() {
        if (state == State.ButtonUnpressed) {
            state = State.ButtonPressing;
        }
        else if (state == State.ButtonPressed) {
            state = State.ButtonUnpressing;
        }

        if (oneTimeButton) {
            interactableObject.interactable = false;
        }
    }

#region Saving

    [Serializable]
    public class ButtonSave : SerializableSaveObject<Button> {
        public float timeSinceStateChange;
        SerializableAnimationCurve buttonDepressCurve;
        SerializableAnimationCurve buttonPressCurve;

        bool depressAfterPress;
        float depressDistance;
        int state;
        float timeBetweenPressEndDepressStart;
        float timeToDepressButton;
        float timeToPressButton;
        bool oneTimeButton;

        public ButtonSave(Button button) : base(button) {
            state = (int) button.state;
            timeSinceStateChange = button.timeSinceStateChange;
            buttonPressCurve = button.buttonPressCurve;
            buttonDepressCurve = button.buttonUnpressCurve;
            timeToPressButton = button.timeToPressButton;
            timeToDepressButton = button.timeToUnpressButton;
            depressDistance = button.pressDistance;

            depressAfterPress = button.unpressAfterPress;
            timeBetweenPressEndDepressStart = button.timeBetweenPressEndDepressStart;
            oneTimeButton = button.oneTimeButton;
        }

        public override void LoadSave(Button button) {
            button.state = (State) state;
            button.timeSinceStateChange = timeSinceStateChange;
            button.buttonPressCurve = buttonPressCurve;
            button.buttonUnpressCurve = buttonDepressCurve;
            button.timeToPressButton = timeToPressButton;
            button.timeToUnpressButton = timeToDepressButton;
            button.pressDistance = depressDistance;

            button.unpressAfterPress = depressAfterPress;
            button.timeBetweenPressEndDepressStart = timeBetweenPressEndDepressStart;
            button.oneTimeButton = oneTimeButton;
        }
    }
#endregion
}