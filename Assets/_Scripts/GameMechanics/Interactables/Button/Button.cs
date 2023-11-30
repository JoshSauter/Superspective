using System;
using Audio;
using NaughtyAttributes;
using PoweredObjects;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Interactables {
    [RequireComponent(typeof(UniqueId), typeof(PoweredObject))]
    public class Button : SaveableObject<Button, Button.ButtonSave> {
        private PoweredObject _pwr;

        public PoweredObject pwr {
            get {
                if (_pwr == null) {
                    _pwr = this.GetOrAddComponent<PoweredObject>();
                }

                return _pwr;
            }
            set => _pwr = value;
        }

        public enum State {
            ButtonUnpressed,
            ButtonPressing,
            ButtonPressed,
            ButtonUnpressing
        }

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

        public bool automaticallySetHelpText = true;

        [ShowIf("automaticallySetHelpText")]
        public string buttonOffHelpText = "Turn on";

        [ShowIf("automaticallySetHelpText")]
        public string buttonOnHelpText = "Turn off";

        public UnityEvent onButtonPressFinish;
        public UnityEvent onButtonUnpressBegin;

#region events

        public delegate void ButtonAction(Button button);

        public event ButtonAction OnButtonPressBegin;
        public event ButtonAction OnButtonPressFinish;
        public event ButtonAction OnButtonUnpressBegin;
        public event ButtonAction OnButtonUnpressFinish;

#endregion

        public StateMachine<State> stateMachine;

        float distanceCurrentlyPressed = 0f;

        public bool buttonPressed => stateMachine.state == State.ButtonPressed;

        protected override void Awake() {
            base.Awake();

            stateMachine = this.StateMachine(State.ButtonUnpressed);

            interactableObject = GetComponent<InteractableObject>();
            if (interactableObject == null) interactableObject = gameObject.AddComponent<InteractableObject>();
            interactableObject.OnLeftMouseButtonDown += OnLeftMouseButtonDown;

            if (stateMachine.state == State.ButtonPressed) {
                distanceCurrentlyPressed = pressDistance;
            }

            if (automaticallySetHelpText) {
                interactableObject.enabledHelpText = stateMachine.state == State.ButtonPressed ? buttonOnHelpText : buttonOffHelpText;
            }
        }

        protected override void Start() {
            base.Start();
            InitializeStateMachine();
        }

        protected virtual void Update() {
            UpdateButtonPosition();
        }

        protected virtual void InitializeStateMachine() {
            stateMachine.AddStateTransition(State.ButtonPressing, State.ButtonPressed, timeToPressButton);
            if (unpressAfterPress) {
                stateMachine.AddStateTransition(State.ButtonPressed, State.ButtonUnpressing, timeBetweenPressEndDepressStart);
            }

            stateMachine.AddStateTransition(State.ButtonUnpressing, State.ButtonUnpressed, timeToUnpressButton);

            stateMachine.OnStateChange += (prevState, _) => {
                switch (stateMachine.state) {
                    case State.ButtonUnpressed:
                        transform.position -= (distanceCurrentlyPressed) * transform.up;
                        distanceCurrentlyPressed = 0f;

                        if (prevState == State.ButtonUnpressing) {
                            pwr.state.Set(PowerState.Depowered);
                        }

                        if (automaticallySetHelpText) interactableObject.enabledHelpText = buttonOffHelpText;
                        OnButtonUnpressFinish?.Invoke(this);
                        break;
                    case State.ButtonPressing:
                        if (prevState == State.ButtonUnpressed) {
                            pwr.state.Set(PowerState.PartiallyPowered);
                        }

                        if (automaticallySetHelpText) interactableObject.enabledHelpText = buttonOnHelpText;
                        AudioManager.instance.PlayAtLocation(AudioName.ButtonPress, ID, transform.position, true);
                        OnButtonPressBegin?.Invoke(this);
                        break;
                    case State.ButtonPressed:
                        transform.position += (pressDistance - distanceCurrentlyPressed) * transform.up;
                        distanceCurrentlyPressed = pressDistance;

                        if (prevState == State.ButtonPressing) {
                            pwr.state.Set(PowerState.Powered);
                        }

                        if (automaticallySetHelpText) interactableObject.enabledHelpText = buttonOnHelpText;
                        OnButtonPressFinish?.Invoke(this);
                        onButtonPressFinish?.Invoke();
                        break;
                    case State.ButtonUnpressing:
                        if (prevState == State.ButtonPressed) {
                            pwr.state.Set(PowerState.Depowered);
                        }

                        if (automaticallySetHelpText) interactableObject.enabledHelpText = buttonOffHelpText;
                        AudioManager.instance.PlayAtLocation(AudioName.ButtonUnpress, ID, transform.position, true);
                        OnButtonUnpressBegin?.Invoke(this);
                        onButtonUnpressBegin?.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }

        public virtual void OnLeftMouseButtonDown() {
            PressButton();
        }

        protected virtual void UpdateButtonPosition() {
            // if (pressDistance <= 0) return;
            float t = stateMachine.timeSinceStateChanged / timeToPressButton;
            switch (stateMachine.state) {
                case State.ButtonUnpressed:
                case State.ButtonPressed:
                    break;
                case State.ButtonPressing:
                    if (stateMachine.timeSinceStateChanged < timeToPressButton) {
                        float delta = Time.deltaTime * (pressDistance / timeToPressButton);
                        distanceCurrentlyPressed += delta;
                        transform.position += delta * transform.up;
                    }

                    break;
                case State.ButtonUnpressing:
                    if (stateMachine.timeSinceStateChanged < timeToUnpressButton) {
                        float delta = Time.deltaTime * (pressDistance / timeToUnpressButton);
                        distanceCurrentlyPressed -= delta;
                        transform.position -= delta * transform.up;
                    }

                    break;
            }
        }

        public void TurnButtonOff() {
            if (stateMachine == State.ButtonPressed || stateMachine == State.ButtonPressing) {
                stateMachine.Set(State.ButtonUnpressing);
            }
        }

        public void PressButton() {
            if (stateMachine == State.ButtonUnpressed) {
                stateMachine.Set(State.ButtonPressing);
            }
            else if (stateMachine == State.ButtonPressed) {
                stateMachine.Set(State.ButtonUnpressing);
            }

            if (oneTimeButton) {
                interactableObject.SetAsHidden();
            }
        }

#region Saving

        [Serializable]
        public class ButtonSave : SerializableSaveObject<Button> {
            SerializableAnimationCurve buttonDepressCurve;
            SerializableAnimationCurve buttonPressCurve;

            bool depressAfterPress;
            float depressDistance;
            StateMachine<State>.StateMachineSave stateSave;
            float timeBetweenPressEndDepressStart;
            float timeToDepressButton;
            float timeToPressButton;
            bool oneTimeButton;

            public ButtonSave(Button button) : base(button) {
                stateSave = button.stateMachine.ToSave();
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
                button.stateMachine.LoadFromSave(this.stateSave);
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
}