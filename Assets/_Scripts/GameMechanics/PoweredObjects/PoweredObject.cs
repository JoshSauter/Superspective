using System;
using System.Linq;
using PowerTrailMechanics;
using UnityEngine;
using Saving;
using Sirenix.OdinInspector;
using StateUtils;
using UnityEngine.Events;

namespace PoweredObjects {
    public enum MultiMode : byte {
        Single,
        Any,
        All
    }
    
    [Flags]
    public enum PowerMode : byte {
        None = 0,
        PowerOn = 1,
        PowerOff = 2
    }

    public static class PowerModeExt {
        public static bool Has(this PowerMode mode, PowerMode flag) {
            return (mode & flag) == flag;
        }
    }
    
    /// <summary>
    /// Generic script for objects that can be powered on and off.
    /// Can set its own state with daisy-chain power events, through the use of PowerIsOn property, or if automaticallyFinishPowering is true
    /// </summary>
    [RequireComponent(typeof(UniqueId))]
    public class PoweredObject : SuperspectiveObject<PoweredObject, PoweredObject.PoweredObjectSave> {
        public StateMachine<PowerState> state;

#region Power Chaining
        [Header("Parent PoweredObjects")]
        public PowerMode powerMode = PowerMode.PowerOn | PowerMode.PowerOff;
        public MultiMode parentMultiMode = MultiMode.Single;
        bool IsMulti => parentMultiMode != MultiMode.Single;
        [HideIf(nameof(IsMulti))]
        public PoweredObject source;
        [ShowIf(nameof(IsMulti))]
        public PoweredObject[] sources;
        
        private bool ParentsFullyPowered {
            get {
                switch (parentMultiMode) {
                    case MultiMode.Single:
                        return source.PowerIsOn;
                    case MultiMode.Any:
                        return sources.ToList().Exists(s => s.FullyPowered);
                    case MultiMode.All:
                        return sources.ToList().TrueForAll(s => s.FullyPowered);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void PowerFromParents() {
            PowerIsOn = ParentsFullyPowered;
        }
        
        private void InitEvents() {
            state.OnStateChange += OnPowerStateChange;
            switch (parentMultiMode) {
                case MultiMode.Single:
                    if (source == null) return;

                    if (powerMode.HasFlag(PowerMode.PowerOn)) {
                        source.OnPowerFinish += PowerFromParents;
                    }

                    if (powerMode.HasFlag(PowerMode.PowerOff)) {
                        source.OnDepowerBegin += PowerFromParents;
                    }
                    break;
                case MultiMode.Any:
                case MultiMode.All:
                    if (sources == null || sources.Length == 0) return;
                    
                    foreach (var parent in sources) {
                        if (powerMode.HasFlag(PowerMode.PowerOn)) {
                            parent.OnPowerFinish += PowerFromParents;
                        }
                        
                        if (powerMode.HasFlag(PowerMode.PowerOff)) {
                            parent.OnDepowerBegin += PowerFromParents;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TeardownEvents() {
            state.OnStateChange -= OnPowerStateChange;
            switch (parentMultiMode) {
                case MultiMode.Single:
                    if (source == null) return;
                    
                    source.OnPowerFinish -= PowerFromParents;
                    source.OnDepowerBegin -= PowerFromParents;
                    break;
                case MultiMode.Any:
                case MultiMode.All:
                    if (sources == null || sources.Length == 0) return;
                    
                    foreach (var parent in sources) {
                        parent.OnPowerFinish -= PowerFromParents;
                        parent.OnDepowerBegin -= PowerFromParents;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
#endregion

#region events
        public delegate void PowerSourceAction();
        public delegate void PowerSourceRefAction(PoweredObject poweredObject);
        
        public event PowerSourceAction OnPowerBegin;
        public event PowerSourceAction OnPowerFinish;
        public event PowerSourceAction OnDepowerBegin;
        public event PowerSourceAction OnDepowerFinish;
        public event PowerSourceRefAction OnPowerBeginRef;
        public event PowerSourceRefAction OnPowerFinishRef;
        public event PowerSourceRefAction OnDepowerBeginRef;
        public event PowerSourceRefAction OnDepowerFinishRef;
        
        [Header("Events")]
        public UnityEvent onPowerBegin;
        public UnityEvent onPowerFinish;
        public UnityEvent onDepowerBegin;
        public UnityEvent onDepowerFinish;
#endregion

        public bool automaticallyFinishPowering = false;
        [ShowIf(nameof(automaticallyFinishPowering))]
        [Header("Time to automatically finish powering")]
        public float automaticFinishPoweringTime;
        public bool automaticallyFinishDepowering = false;
        [ShowIf(nameof(automaticallyFinishDepowering))]
        [Header("Time to automatically finish depowering")]
        public float automaticFinishDepoweringTime;

        public bool PowerIsOn {
            get => state == PowerState.PartiallyPowered || state == PowerState.Powered;
            set {
                if (value == PowerIsOn) return;

                switch (state.State) {
                    case PowerState.PartiallyDepowered:
                    case PowerState.Depowered:
                        if (value) {
                            state.Set(PowerState.PartiallyPowered);
                        }
                        break;
                    case PowerState.PartiallyPowered:
                    case PowerState.Powered:
                        if (!value) {
                            state.Set(PowerState.PartiallyDepowered);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public bool FullyPowered => state == PowerState.Powered;
        public bool FullyDepowered => state == PowerState.Depowered;
        public bool PartiallyPowered => state == PowerState.PartiallyPowered || state == PowerState.PartiallyDepowered;

        protected override void Start() {
            base.Start();
            
            InitializeStateEvents();
        }

        protected override void OnEnable() {
            base.OnEnable();
            state = this.StateMachine(PowerState.Depowered);
            
            InitEvents();
        }

        protected override void OnDisable() {
            base.OnDisable();
            TeardownEvents();
        }

        private void InitializeStateEvents() {
            if (automaticallyFinishPowering) {
                state.AddStateTransition(PowerState.PartiallyPowered, PowerState.Powered, automaticFinishPoweringTime);
            }
            if (automaticallyFinishDepowering) {
                state.AddStateTransition(PowerState.PartiallyDepowered, PowerState.Depowered, automaticFinishDepoweringTime);
            }
        }

        private void OnPowerStateChange(PowerState prevState, float prevTime) {
            switch (prevState) {
                case PowerState.Depowered:
                    if (state == PowerState.PartiallyPowered) {
                        OnPowerBegin?.Invoke();
                        onPowerBegin?.Invoke();
                        OnPowerBeginRef?.Invoke(this);
                    }
                    else if (state == PowerState.Powered) {
                        // Trigger both OnPowerBegin and OnPowerFinish events
                        OnPowerBegin?.Invoke();
                        onPowerBegin?.Invoke();
                        OnPowerBeginRef?.Invoke(this);
                        
                        OnPowerFinish?.Invoke();
                        onPowerFinish?.Invoke();
                        OnPowerFinishRef?.Invoke(this);
                    }
                    break;
                case PowerState.PartiallyPowered:
                    if (state == PowerState.Powered) {
                        OnPowerFinish?.Invoke();
                        onPowerFinish?.Invoke();
                        OnPowerFinishRef?.Invoke(this);
                    }
                    break;
                case PowerState.Powered:
                    if (state == PowerState.PartiallyDepowered) {
                        OnDepowerBegin?.Invoke();
                        onDepowerBegin?.Invoke();
                        OnDepowerBeginRef?.Invoke(this);
                    }
                    else if (state == PowerState.Depowered) {
                        // Trigger both OnDepowerBegin and OnDepowerFinish events
                        OnDepowerBegin?.Invoke();
                        onDepowerBegin?.Invoke();
                        OnDepowerBeginRef?.Invoke(this);
                        
                        OnDepowerFinish?.Invoke();
                        onDepowerFinish?.Invoke();
                        OnDepowerFinishRef?.Invoke(this);
                    }
                    break;
                case PowerState.PartiallyDepowered:
                    if (state == PowerState.Depowered) {
                        OnDepowerFinish?.Invoke();
                        onDepowerFinish?.Invoke();
                        OnDepowerFinishRef?.Invoke(this);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(prevState), prevState, null);
            }
        }

#region Saving

        public override void LoadSave(PoweredObjectSave save) {
            state.LoadFromSave(save.stateSave);
            automaticFinishPoweringTime = save.automaticFinishPoweringTime;
            automaticFinishDepoweringTime = save.automaticFinishDepoweringTime;
            automaticallyFinishPowering = save.automaticallyFinishPowering;
            automaticallyFinishDepowering = save.automaticallyFinishDepowering;
        }

        [Serializable]
        public class PoweredObjectSave : SaveObject<PoweredObject> {
            public StateMachine<PowerState>.StateMachineSave stateSave;
            public float automaticFinishPoweringTime;
            public float automaticFinishDepoweringTime;
            public bool automaticallyFinishPowering;
            public bool automaticallyFinishDepowering;
            
            // Provided so that Power can be set even when the object is unloaded
            public bool PowerIsOn {
                get => stateSave.state is PowerState.PartiallyPowered or PowerState.Powered;
                set {
                    if (value == PowerIsOn) return;

                    switch (stateSave.state) {
                        case PowerState.PartiallyDepowered:
                        case PowerState.Depowered:
                            if (value) {
                                stateSave.prevState = stateSave.state;
                                stateSave.state = PowerState.PartiallyPowered;
                                stateSave.timeSinceStateChanged = 0;
                            }
                            break;
                        case PowerState.PartiallyPowered:
                        case PowerState.Powered:
                            if (!value) {
                                stateSave.prevState = stateSave.state;
                                stateSave.state = PowerState.PartiallyDepowered;
                                stateSave.timeSinceStateChanged = 0;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            public PoweredObjectSave(PoweredObject script) : base(script) {
                this.stateSave = script.state.ToSave();
                this.automaticFinishPoweringTime = script.automaticFinishPoweringTime;
                this.automaticFinishDepoweringTime = script.automaticFinishDepoweringTime;
                this.automaticallyFinishPowering = script.automaticallyFinishPowering;
                this.automaticallyFinishDepowering = script.automaticallyFinishDepowering;
            }
        }
#endregion
    }
}