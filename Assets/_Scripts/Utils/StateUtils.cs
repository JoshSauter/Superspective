using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace StateUtils {
    [Serializable]
    public class StateMachine<T> where T : Enum {
        [SerializeField, Label("State")]
        private T _state;
        [SerializeField, Label("Previous State"), ReadOnly]
        private T _prevState;
        [SerializeField, Label("Time since state changed")]
        private float _timeSinceStateChanged;

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

                _timeSinceStateChanged = 0f;
                _prevState = _state;
                _state = value;
                
                OnStateChange?.Invoke(prevState);
                OnStateChangeSimple?.Invoke();
            }
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

        public void Set(T newState) {
            state = newState;
        }

        public void AddStateTransition(T fromState, T toState, float atTime) {
            TimedEventTrigger stateTransitionTrigger = new TimedEventTrigger() {
                forState = fromState,
                atTime = atTime
            };
            
            timedEvents.Add(stateTransitionTrigger, () => state = toState);
        }
        
        public void AddTrigger(T forState, float atTime, Action whatToDo) {
            TimedEventTrigger timedEventTrigger = new TimedEventTrigger { forState = forState, atTime = atTime };
            timedEvents.Add(timedEventTrigger, whatToDo);
        }
        
        #region Custom Events

        class TimedEventTrigger {
            public T forState;
            public float atTime;
        }

        private Dictionary<TimedEventTrigger, Action> timedEvents = new Dictionary<TimedEventTrigger, Action>();

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
                GlobalUpdate.instance.UpdateGlobal -= Update;
            }
        }

        private void InitIdempotent() {
            if (hasSubscribedToUpdate) return;
            
            GlobalUpdate.instance.UpdateGlobal += Update;
            hasSubscribedToUpdate = true;
        } 

        private void Update() {
            float prevTime = _timeSinceStateChanged;
            _timeSinceStateChanged += Time.deltaTime;
            TriggerEvents(prevTime);
        }
    }
}