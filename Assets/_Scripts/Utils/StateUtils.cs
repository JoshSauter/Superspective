using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NaughtyAttributes;
using SerializableClasses;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

// ReSharper disable All

namespace StateUtils {
    [Serializable]
    public class StateMachine<T> where T : Enum, IConvertible {
        // Used to execute State Transitions only after other events have fired
        private const int stateTransitionPriority = 10;
        
        [SerializeField, Label("State")]
        private T _state;
        [SerializeField, Label("Previous State"), ReadOnly]
        private T _prevState;
        [SerializeField, Label("Time since state changed")]
        private float _timeSinceStateChanged;

        [NonSerialized]
        private bool hasSubscribedToUpdate = false;

        [NonSerialized]
        private bool useFixedUpdateInstead = false;

        [NonSerialized]
        // If set to true, will use value not scaled by Time.timeScale
        private bool useRealTime = false;

        public delegate void OnStateChangeEvent(T prevState, float prevTimeSinceStateChanged);
        public delegate void OnStateChangeEventSimple();
        public event OnStateChangeEvent OnStateChange;
        public event OnStateChangeEventSimple OnStateChangeSimple;
        public Dictionary<T, UnityEvent> onStateChangeDict;

        [Serializable]
        public struct StateMachineUnityEvent {
            public T state;
            public UnityEvent onStateChange;
        }

        public List<StateMachineUnityEvent> onStateChange;

        public T state {
            get {
                InitIdempotent();
                return _state;
            }
            set {
                InitIdempotent();
                if (value.Equals(_state)) {
                    return;
                }

                ForceSetState(value);
            }
        }

        private void ForceSetState(T newState) {
            float prevTimeSinceStateChanged = _timeSinceStateChanged;
            _timeSinceStateChanged = 0f;
            _prevState = _state;
            _state = newState;
                
            OnStateChange?.Invoke(prevState, prevTimeSinceStateChanged);
            if (onStateChangeDict.ContainsKey(newState)) {
                onStateChangeDict[newState]?.Invoke();
            }
            OnStateChangeSimple?.Invoke();
        }

        public T prevState => _prevState;

        public float timeSinceStateChanged {
            get {
                InitIdempotent();
                return _timeSinceStateChanged;
            }
            set {
                InitIdempotent();
                _timeSinceStateChanged = value;
            }
        }

        public static implicit operator T(StateMachine<T> stateMachine) => stateMachine.state;
        public static implicit operator StateMachine<T>(T enumValue) => new StateMachine<T>(enumValue);
        public static implicit operator int(StateMachine<T> stateMachine) => (int)(object)stateMachine.state;

        public void Set(T newState, bool forceTimeReset = false) {
            state = newState;
            if (forceTimeReset) {
                timeSinceStateChanged = 0f;
            }
        }

        // If we want to determine the toState at runtime, we can pass a method to provide it
        public void AddStateTransition(T fromState, Func<T> toStateDef, float atTime) {
            TimedEventTrigger stateTransitionTrigger = new TimedEventTrigger() {
                forState = fromState,
                atTime = atTime,
                priority = stateTransitionPriority
            };
            
            timedEvents.Add(stateTransitionTrigger, () => state = toStateDef.Invoke());
        }
        
        public void AddStateTransition(T fromState, Func<T> toStateDef, Func<bool> triggerWhen) {
            CustomEventTrigger customEventTrigger = new CustomEventTrigger() {
                forState = fromState,
                triggerWhen = triggerWhen,
                priority = stateTransitionPriority
            };
            
            customEvents.Add(customEventTrigger, () => state = toStateDef.Invoke());
        }

        public void AddStateTransition(T fromState, T toState, float atTime) {
            TimedEventTrigger stateTransitionTrigger = new TimedEventTrigger() {
                forState = fromState,
                atTime = atTime,
                priority = stateTransitionPriority
            };
            
            timedEvents.Add(stateTransitionTrigger, () => state = toState);
        }

        public void AddStateTransition(T fromState, T toState, Func<bool> triggerWhen) {
            CustomEventTrigger customEventTrigger = new CustomEventTrigger() {
                forState = fromState,
                triggerWhen = triggerWhen,
                priority = stateTransitionPriority
            };
            
            customEvents.Add(customEventTrigger, () => state = toState);
        }

        public void AddTrigger(Func<T, bool> forStates, Action whatToDo) {
            AddTrigger(forStates, 0f, whatToDo);
        }

        public void AddTrigger(Func<T, bool> forStates, float atTime, Action whatToDo) {
            foreach (T enumValue in Enum.GetValues(typeof(T))) {
                if (forStates.Invoke(enumValue)) {
                    AddTrigger(enumValue, atTime, whatToDo);
                }
            }
        }
        
        public void AddTrigger(Func<T, bool> forStates, Action<T> whatToDo) {
            AddTrigger(forStates, 0f, whatToDo);
        }
        
        public void AddTrigger(Func<T, bool> forStates, float atTime, Action<T> whatToDo) {
            foreach (T enumValue in Enum.GetValues(typeof(T))) {
                if (forStates.Invoke(enumValue)) {
                    AddTrigger(enumValue, atTime, () => whatToDo.Invoke(enumValue));
                }
            }
        }

        public void AddTrigger(T forState, Action whatToDo) {
            AddTrigger(forState, 0f, whatToDo);
        }
        
        public void AddTrigger(T forState, float atTime, Action whatToDo) {
            TimedEventTrigger timedEventTrigger = new TimedEventTrigger { forState = forState, atTime = atTime };
            timedEvents.Add(timedEventTrigger, whatToDo);
        }
        
        #region Custom Events

        class CustomEventTrigger {
            public T forState;
            public Func<bool> triggerWhen;
            public int priority = 0;
        }

        [NonSerialized] private Dictionary<CustomEventTrigger, Action> _customEvents;

        private Dictionary<CustomEventTrigger, Action> customEvents {
            get {
                if (_customEvents == null) {
                    _customEvents = new Dictionary<CustomEventTrigger, Action>();
                }

                return _customEvents;
            }
        }

        class TimedEventTrigger {
            public T forState;
            public float atTime;
            public int priority = 0; // Lower value == executed first
        }

        [NonSerialized] private Dictionary<TimedEventTrigger, Action> _timedEvents;

        private Dictionary<TimedEventTrigger, Action> timedEvents {
            get {
                if (_timedEvents == null) {
                    _timedEvents = new Dictionary<TimedEventTrigger, Action>();
                }

                return _timedEvents;
            }
        }

        private void TriggerEvents(float prevTime) {
            // Don't trigger events while Time.deltaTime is 0
            if (Math.Abs(prevTime - _timeSinceStateChanged) < float.Epsilon) return;
            
            var timedEventsToTrigger = timedEvents.Where(triggerAndAction => {
                TimedEventTrigger trigger = triggerAndAction.Key;
                return trigger.forState.Equals(_state) &&
                       trigger.atTime >= prevTime &&
                       trigger.atTime < _timeSinceStateChanged;
            }).OrderBy(triggerAndAction => triggerAndAction.Key.priority)
                .Select(triggerAndAction => triggerAndAction.Value);

            foreach (Action action in timedEventsToTrigger) {
                action.Invoke();
            }

            var customEventsToTrigger = customEvents.Where(triggerAndAction => {
                CustomEventTrigger trigger = triggerAndAction.Key;
                return trigger.forState.Equals(_state) && trigger.triggerWhen.Invoke();
            }).OrderBy(triggerAndAction => triggerAndAction.Key.priority)
                .Select(triggerAndAction => triggerAndAction.Value);

            foreach (var action in customEventsToTrigger) {
                action.Invoke();
            }
        }
        #endregion
        
        public StateMachine(T startingState) {
            this._state = startingState;
            this._prevState = _state;
            this._timeSinceStateChanged = 0f;
        }
        
        public StateMachine(T startingState, bool useFixedUpdateInstead = false, bool useRealTime = false) {
            this._state = startingState;
            this._prevState = _state;
            this._timeSinceStateChanged = 0f;
            this.useFixedUpdateInstead = useFixedUpdateInstead;
            this.useRealTime = useRealTime;
        }

        private StateMachine() { }

        ~StateMachine() {
            if (hasSubscribedToUpdate) {
                try {
                    GlobalUpdate.instance.UpdateGlobal -= Update;
                }
                catch { }
            }
        }

        private void InitIdempotent() {
            if (hasSubscribedToUpdate || GlobalUpdate.instance == null) return;

            onStateChangeDict = onStateChange?.ToDictionary(unityEvent => unityEvent.state, unityEvent => unityEvent.onStateChange) ?? new Dictionary<T, UnityEvent>();

            if (useFixedUpdateInstead) {
                GlobalUpdate.instance.FixedUpdateGlobal += Update;
            }
            else {
                GlobalUpdate.instance.UpdateGlobal += Update;
            }
            hasSubscribedToUpdate = true;
        }

        // Does either Update or FixedUpdate based on config
        private void Update() {
            float prevTime = _timeSinceStateChanged;

            float GetDeltaTime() {
                if (useFixedUpdateInstead) {
                    if (useRealTime) {
                        return Time.fixedUnscaledDeltaTime;
                    }
                    else {
                        return Time.fixedDeltaTime;
                    }
                }
                else {
                    if (useRealTime) {
                        return Time.unscaledDeltaTime;
                    }
                    else {
                        return Time.deltaTime;
                    }
                }
            }
            
            _timeSinceStateChanged += GetDeltaTime();
            TriggerEvents(prevTime);
        }

        public StateMachineSave ToSave() {
            StateMachineSave save = new StateMachineSave {
                timeSinceStateChanged = timeSinceStateChanged,
                state = state
            };
            return save;
        }

        public void FromSave(StateMachineSave save) {
            this.state = save.state;
            this.timeSinceStateChanged = save.timeSinceStateChanged;
        }
        
        [Serializable]
        public class StateMachineSave {
            public float timeSinceStateChanged;
            public T state;
        }
    }
}