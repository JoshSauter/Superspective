using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
// ReSharper disable All

namespace StateUtils {
    [Serializable]
    public class StateMachine<T> where T : Enum {
        [SerializeField, Label("State")]
        private T _state;
        [SerializeField, Label("Previous State"), ReadOnly]
        private T _prevState;
        [SerializeField, Label("Time since state changed")]
        private float _timeSinceStateChanged;

        [NonSerialized]
        private bool hasSubscribedToUpdate = false;

        public delegate void OnStateChangeEvent(T prevState);
        public delegate void OnStateChangeEventSimple();
        public event OnStateChangeEvent OnStateChange;
        public event OnStateChangeEventSimple OnStateChangeSimple;

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
            _timeSinceStateChanged = 0f;
            _prevState = _state;
            _state = newState;
                
            OnStateChange?.Invoke(prevState);
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
                atTime = atTime
            };
            
            timedEvents.Add(stateTransitionTrigger, () => state = toStateDef.Invoke());
        }

        public void AddStateTransition(T fromState, T toState, float atTime) {
            TimedEventTrigger stateTransitionTrigger = new TimedEventTrigger() {
                forState = fromState,
                atTime = atTime
            };
            
            timedEvents.Add(stateTransitionTrigger, () => state = toState);
        }

        public void AddTrigger(Func<T, bool> forStates, float atTime, Action whatToDo) {
            foreach (T enumValue in Enum.GetValues(typeof(T))) {
                if (forStates.Invoke(enumValue)) {
                    AddTrigger(enumValue, atTime, whatToDo);
                }
            }
        }
        
        public void AddTrigger(Func<T, bool> forStates, float atTime, Action<T> whatToDo) {
            foreach (T enumValue in Enum.GetValues(typeof(T))) {
                if (forStates.Invoke(enumValue)) {
                    AddTrigger(enumValue, atTime, () => whatToDo.Invoke(enumValue));
                }
            }
        }
        
        public void AddTrigger(T forState, float atTime, Action whatToDo) {
            TimedEventTrigger timedEventTrigger = new TimedEventTrigger { forState = forState, atTime = atTime };
            timedEvents.Add(timedEventTrigger, whatToDo);
        }
        
        #region Custom Events

        [Serializable]
        class TimedEventTrigger {
            public T forState;
            public float atTime;
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
            
            var eventsToTrigger = timedEvents.Where(triggerAndAction => {
                TimedEventTrigger trigger = triggerAndAction.Key;
                return trigger.forState.Equals(_state) &&
                       trigger.atTime >= prevTime &&
                       trigger.atTime < _timeSinceStateChanged;
            }).Select(triggerAndAction => triggerAndAction.Value);

            foreach (Action action in eventsToTrigger) {
                action.Invoke();
            }
        }
        #endregion
        
        public StateMachine(T startingState) {
            _state = startingState;
            _prevState = _state;
            _timeSinceStateChanged = 0f;
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
            
            GlobalUpdate.instance.UpdateGlobal += Update;
            hasSubscribedToUpdate = true;
        } 

        private void Update() {
            float prevTime = _timeSinceStateChanged;
            _timeSinceStateChanged += Time.deltaTime;
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