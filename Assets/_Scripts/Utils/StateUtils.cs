using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityObject = UnityEngine.Object;

namespace StateUtils {
    [Serializable]
    public class StateMachine<T> where T : Enum, IConvertible {
        private bool hasUnityObjectOwner;
        private UnityObject _owner;
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
            private set {
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
                atTime = atTime
            };
            
            timedStateTransitions.Add(stateTransitionTrigger, () => state = toStateDef.Invoke());
        }
        
        public void AddStateTransition(T fromState, Func<T> toStateDef, Func<bool> triggerWhen) {
            CustomEventTrigger customEventTrigger = new CustomEventTrigger() {
                forState = fromState,
                triggerWhen = triggerWhen
            };
            
            customStateTransitions.Add(customEventTrigger, () => state = toStateDef.Invoke());
        }

        public void AddStateTransition(T fromState, T toState, float atTime) {
            TimedEventTrigger stateTransitionTrigger = new TimedEventTrigger() {
                forState = fromState,
                atTime = atTime
            };
            
            timedStateTransitions.Add(stateTransitionTrigger, () => state = toState);
        }

        public void AddStateTransition(T fromState, T toState, Func<float> atTimeProvider) {
            throw new NotImplementedException("Time to make this work obviously");
            // TODO: Implement this and add the other AddStateTransition method signatures for it
        }

        public void AddStateTransition(T fromState, T toState, Func<bool> triggerWhen) {
            CustomEventTrigger customEventTrigger = new CustomEventTrigger() {
                forState = fromState,
                triggerWhen = triggerWhen
            };
            
            customStateTransitions.Add(customEventTrigger, () => state = toState);
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

        public void WithUpdate(T forState, Action<float> forTime) {
            CustomUpdate customUpdate = new CustomUpdate { forState = forState, updateAction = forTime };
            List<CustomUpdate> customUpdatesForState = updateActions.GetOrNull(forState) ?? new List<CustomUpdate>();
            customUpdatesForState.Add(customUpdate);
            updateActions[forState] = customUpdatesForState;
        }
        
        # region Custom Update

        class CustomUpdate {
            public T forState;
            public Action<float> updateAction;
        }
        
        [NonSerialized]
        private Dictionary<T, List<CustomUpdate>> _updateActions;
        private Dictionary<T, List<CustomUpdate>> updateActions {
            get {
                if (_updateActions == null) {
                    _updateActions = new Dictionary<T, List<CustomUpdate>>();
                }

                return _updateActions;
            }
        }
        #endregion
        
        #region Custom Events

        class CustomEventTrigger {
            public T forState;
            public Func<bool> triggerWhen;
        }

        [NonSerialized]
        private Dictionary<CustomEventTrigger, Action> _customEvents;
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

        [NonSerialized]
        private Dictionary<TimedEventTrigger, Action> _timedStateTransitions;
        private Dictionary<TimedEventTrigger, Action> timedStateTransitions {
            get {
                if (_timedStateTransitions == null) {
                    _timedStateTransitions = new Dictionary<TimedEventTrigger, Action>();
                }

                return _timedStateTransitions;
            }
        }
        
        [NonSerialized]
        private Dictionary<CustomEventTrigger, Action> _customStateTransitions;
        private Dictionary<CustomEventTrigger, Action> customStateTransitions {
            get {
                if (_customStateTransitions == null) {
                    _customStateTransitions = new Dictionary<CustomEventTrigger, Action>();
                }

                return _customStateTransitions;
            }
        }

        private void TriggerEvents(float prevTime) {
            // Don't trigger events while Time.deltaTime is 0
            if (Math.Abs(prevTime - _timeSinceStateChanged) < float.Epsilon) return;

            // Timed and Custom events are triggered before state transitions
            foreach (var triggerAndAction in timedEvents) {
                TimedEventTrigger trigger = triggerAndAction.Key;
                if (trigger.forState.Equals(_state) && trigger.atTime >= prevTime &&
                    trigger.atTime < _timeSinceStateChanged) {
                    triggerAndAction.Value.Invoke();
                }
            }
            foreach (var triggerAndAction in customEvents) {
                CustomEventTrigger trigger = triggerAndAction.Key;
                if (trigger.forState.Equals(_state) && trigger.triggerWhen.Invoke()) {
                    triggerAndAction.Value.Invoke();
                }
            }
            
            // Timed and Custom state transitions are triggered after all other events
            foreach (var triggerAndAction in timedStateTransitions) {
                TimedEventTrigger trigger = triggerAndAction.Key;
                if (trigger.forState.Equals(_state) && trigger.atTime >= prevTime &&
                    trigger.atTime < _timeSinceStateChanged) {
                    triggerAndAction.Value.Invoke();
                }
            }
            foreach (var triggerAndAction in customStateTransitions) {
                CustomEventTrigger trigger = triggerAndAction.Key;
                if (trigger.forState.Equals(_state) && trigger.triggerWhen.Invoke()) {
                    triggerAndAction.Value.Invoke();
                }
            }
        }

        private void RunUpdateActions() {
            if (updateActions.ContainsKey(state)) {
                foreach (var update in updateActions[state]) {
                    try {
                        update.updateAction.Invoke(timeSinceStateChanged);
                    }
                    catch (Exception e) {
                        Debug.LogError(_owner + " threw an exception: " + e.Message + "\n" + e.StackTrace);
                    }
                }
            }
        }
        #endregion
        
        // Hide this constructor behind a static method to make sure the user intends it
        public static StateMachine<T> CreateWithoutOwner(T startingState, bool useFixedUpdateInstead = false, bool useRealTime = false) {
            return new StateMachine<T>(startingState, useFixedUpdateInstead, useRealTime);
        }

        private StateMachine(T startingState, bool useFixedUpdateInstead = false, bool useRealTime = false) {
            this.hasUnityObjectOwner = false;
            this._state = startingState;
            this._prevState = _state;
            this._timeSinceStateChanged = 0f;
            this.useFixedUpdateInstead = useFixedUpdateInstead;
            this.useRealTime = useRealTime;
        }
        
        public StateMachine(UnityObject owner, T startingState) {
            this.hasUnityObjectOwner = true;
            this._owner = owner;
            this._state = startingState;
            this._prevState = _state;
            this._timeSinceStateChanged = 0f;
        }
        
        public StateMachine(UnityObject owner, T startingState, bool useFixedUpdateInstead = false, bool useRealTime = false) {
            this.hasUnityObjectOwner = true;
            this._owner = owner;
            this._state = startingState;
            this._prevState = _state;
            this._timeSinceStateChanged = 0f;
            this.useFixedUpdateInstead = useFixedUpdateInstead;
            this.useRealTime = useRealTime;
        }

        private StateMachine() { }

        public void CleanUp() {
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
            // If the Unity Object that was using this StateMachine is destroyed, cleanup event subscriptions
            if (hasUnityObjectOwner && _owner == null) {
                CleanUp();
                return;
            }
            
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
            RunUpdateActions();
        }

        public StateMachineSave ToSave() {
            StateMachineSave save = new StateMachineSave {
                timeSinceStateChanged = timeSinceStateChanged,
                state = state
            };
            return save;
        }

        public void LoadFromSave(StateMachineSave save) {
            this.InitIdempotent();
            this._state = save.state;
            this._timeSinceStateChanged = save.timeSinceStateChanged;
        }
        
        [Serializable]
        public class StateMachineSave {
            public float timeSinceStateChanged;
            public T state;
        }
    }

    public static class StateMachineExt {
        public static StateMachine<T> StateMachine<T>(this UnityObject owner, T startingState) where T : Enum {
            return new StateMachine<T>(owner, startingState);
        }
        
        public static StateMachine<T> StateMachine<T>(this UnityObject owner, T startingState, bool useFixedUpdateInstead = false, bool useRealTime = false) where T : Enum {
            return new StateMachine<T>(owner, startingState, useFixedUpdateInstead, useRealTime);
        }
    }
}