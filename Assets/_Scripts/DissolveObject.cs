using System;
using System.Linq;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using StateUtils;
using SuperspectiveAttributes;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DissolveObjects {
    
    [RequireComponent(typeof(UniqueId))]
    public class DissolveObject : SuperspectiveObject<DissolveObject, DissolveObject.DissolveObjectSave> {
        private const string DISSOLVE_OBJECT_KEYWORD = "DISSOLVE_OBJECT";
        
        public enum State : byte {
            Materialized,
            Dematerializing,
            Dematerialized,
            Materializing
        }

        [FormerlySerializedAs("_state")]
        [SerializeField]
        private State startingState;
        public StateMachine<State> stateMachine;

        private bool IsInvisibleDimensionObj => thisDimensionObj != null && thisDimensionObj.visibilityState == VisibilityState.Invisible;
        [DoNotSave]
        private bool hasSubscribedToResetLayers = false;
        private bool SubscribedToResetLayers {
            get => thisDimensionObj != null && hasSubscribedToResetLayers;
            set {
                if (thisDimensionObj == null) {
                    hasSubscribedToResetLayers = false;
                    return;
                }

                if (value) {
                    thisDimensionObj.OnStateChangeSimple += ResetLayersToDefaultsOnVisibilityState;
                }
                else {
                    thisDimensionObj.OnStateChangeSimple -= ResetLayersToDefaultsOnVisibilityState;
                }
            }
        }
        void ResetLayersToDefaultsOnVisibilityState() {
            if (thisDimensionObj == null) return;
            // Only trigger when the visibility state become some sort of visible
            if (IsInvisibleDimensionObj) return;

            // Only trigger once
            SubscribedToResetLayers = false;
            
            ApplyLayers(defaultLayers);
        }

        private const float MATERIALIZE_COLLIDER_THRESHOLD = 0.25f;
        private const string DISSOLVE_AMOUNT_PROP = "_DissolveAmount";
        private const string DISSOLVE_BURN_SIZE_PROP = "_DissolveBurnSize";
        private const string DISSOLVE_BURN_COLOR_PROP = "_DissolveBurnColor";
        private const string DISSOLVE_BURN_EMISSION_AMOUNT_PROP = "_DissolveEmissionAmount";
        private const string DISSOLVE_TEX_PROP = "_DissolveTex";
        private const string DISSOLVE_BURN_RAMP_PROP = "_DissolveBurnRamp";
        
        [Range(0f, 1f)]
        public float dissolveAmount = 0.25f;
        public float materializeTime = 2f;
        [Range(0f, 1f)]
        public float physicsThreshold = MATERIALIZE_COLLIDER_THRESHOLD;
        public float burnSize = 0.1f;
        public Color burnColor = new Color(.3f,.6f,.9f,1);
        public float burnEmissionBrightness = 2f;
        public bool advancedOptions;
        [ShowIf("advancedOptions")]
        public AnimationCurve dissolveAnimationCurve = AnimationCurve.EaseInOut(0,0,1,1);
        [ShowIf("advancedOptions")]
        public Texture dissolveTexture;
        [ShowIf("advancedOptions")]
        public Texture dissolveBurnRamp;

        public Renderer[] renderers;
        public Collider[] colliders;
        // Default layers are the layers at object initialization time
        private int[] defaultLayers;
        // The layer the objects would be on if not dissolved
        private int[] cachedLayers;

        private DimensionObject thisDimensionObj;
        
        public UnityEvent onMaterializeBegin;
        public UnityEvent onDematerializeBegin;
        public UnityEvent onMaterializeFinish;
        public UnityEvent onDematerializeFinish;
        
        [DoNotSave]
        bool hasInitializedStateMachine = false;

        [Button("Dematerialize")]
        public void Dematerialize() {
            if (!Application.isPlaying) return;

            if (stateMachine.State is not (State.Dematerialized or State.Dematerializing)) {
                float prevTime = stateMachine.Time;
                stateMachine.Set(State.Dematerializing);

                // If we transitioned from Materializing to Dematerializing, we need to adjust the time to match the animation
                if (stateMachine.PrevState == State.Materializing) {
                    stateMachine.Time = materializeTime - prevTime;
                }
            }
        }

        [Button("Materialize")]
        public void Materialize() {
            if (!Application.isPlaying) return;
            
            if (stateMachine.State is not (State.Materialized or State.Materializing)) {
                float prevTime = stateMachine.Time;
                stateMachine.Set(State.Materializing);
                
                // If we transitioned from Dematerializing to Materializing, we need to adjust the time to match the animation
                if (stateMachine.PrevState == State.Dematerializing) {
                    stateMachine.Time = materializeTime - prevTime;
                }
            }
        }

        protected override void OnValidate() {
            base.OnValidate();
            if (dissolveTexture == null) {
                dissolveTexture = Resources.Load<Texture>("Materials/Suberspective/SuberspectiveDissolveTextureDefault");
            }
            if (dissolveBurnRamp == null) {
                dissolveBurnRamp = Resources.Load<Texture>("Materials/Suberspective/SuberspectiveDissolveBurnRampDefault");
            }
        }

        protected override void Start() {
            base.Start();
            if (renderers == null || renderers.Length == 0) {
                renderers = gameObject.GetComponentsInChildrenRecursively<Renderer>();
            }

            foreach (Renderer renderer in renderers) {
                foreach (Material material in renderer.materials) {
                    material.EnableKeyword(DISSOLVE_OBJECT_KEYWORD);
                }
            }
            cachedLayers = renderers.Select(r => r.gameObject.layer).ToArray();
            defaultLayers = renderers.Select(r => r.gameObject.layer).ToArray();
            if (colliders == null || colliders.Length == 0) {
                colliders = gameObject.GetComponentsInChildrenRecursively<Collider>();
            }
            thisDimensionObj = gameObject.FindDimensionObjectRecursively<DimensionObject>();
        }

        protected override void Init() {
            base.Init();
            
            InitializeStateMachine();
            switch (startingState) {
                case State.Materialized:
                    dissolveAmount = 0;
                    break;
                case State.Dematerializing:
                    break;
                case State.Dematerialized:
                    dissolveAmount = 1;
                    break;
                case State.Materializing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ApplyCurrentState();
        }

        protected override void OnEnable() {
            base.OnEnable();

            InitializeStateMachine();
        }

        public void InitializeStateMachine() {
            if (hasInitializedStateMachine) return;
            hasInitializedStateMachine = true;
            stateMachine = this.StateMachine(startingState);
            
            // Add state transitions for finishing an animation
            stateMachine.AddStateTransition(State.Materializing, State.Materialized, () => dissolveAmount <= 0);
            stateMachine.AddStateTransition(State.Dematerializing, State.Dematerialized, () => dissolveAmount >= 1);

            stateMachine.OnStateChangeSimple += () => {
                switch (stateMachine.State) {
                    case State.Materialized:
                        onMaterializeFinish?.Invoke();
                        break;
                    case State.Dematerializing:
                        onDematerializeBegin?.Invoke();
                        break;
                    case State.Dematerialized:
                        onDematerializeFinish?.Invoke();
                        break;
                    case State.Materializing:
                        onMaterializeBegin?.Invoke();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            
            // Set dissolveAmount to 1 or 0 when entering Materialized or Dematerialized states
            stateMachine.WithUpdate(State.Materialized, _ => {
                dissolveAmount = 0;
                ApplyCurrentState();
            });
            stateMachine.WithUpdate(State.Dematerialized, _ => {
                dissolveAmount = 1;
                ApplyCurrentState();
            });
            
            // Update the dissolveAmount each frame when in Materializing or Dematerializing states
            stateMachine.WithUpdate(State.Materializing, time => {
                dissolveAmount = Mathf.Clamp01(1 - time / materializeTime);
                ApplyCurrentState();
            });
            
            stateMachine.WithUpdate(State.Dematerializing, time => {
                dissolveAmount = Mathf.Clamp01(time / materializeTime);
                ApplyCurrentState();
            });
        }

        // Update is called once per frame
        void Update() {
            if (DEBUG && DebugInput.GetKeyDown(KeyCode.Alpha0)) {
                stateMachine.Set((stateMachine.State is State.Materialized or State.Materializing)
                    ? State.Dematerializing
                    : State.Materializing);
            }
        }

        void ApplyLayers(int[] layersToApply) {
            for (var i = 0; i < renderers.Length; i++) {
                renderers[i].gameObject.layer = layersToApply[i];
            }
        }

        void ApplyCurrentState() {
            // Enable or disable colliders based on dissolveAmount
            foreach (var c in colliders) {
                bool colliderShouldBeEnabled = false;
                switch (stateMachine.State) {
                    case State.Materialized:
                        colliderShouldBeEnabled = true;
                        break;
                    case State.Dematerializing:
                    case State.Materializing:
                        colliderShouldBeEnabled = 1-dissolveAmount > physicsThreshold;
                        break;
                    case State.Dematerialized:
                        colliderShouldBeEnabled = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                c.enabled = colliderShouldBeEnabled;
            }
            
            // Retrieve the MaterialPropertyBlock from the renderer
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    
            foreach (var renderer in renderers) {
                renderer.GetPropertyBlock(mpb);

                // Set material properties via MaterialPropertyBlock
                mpb.SetFloat(DISSOLVE_AMOUNT_PROP, dissolveAmount);
                mpb.SetFloat(DISSOLVE_BURN_SIZE_PROP, burnSize);
                mpb.SetColor(DISSOLVE_BURN_COLOR_PROP, burnColor);
                mpb.SetFloat(DISSOLVE_BURN_EMISSION_AMOUNT_PROP, burnEmissionBrightness);
                mpb.SetTexture(DISSOLVE_TEX_PROP, dissolveTexture);
                mpb.SetTexture(DISSOLVE_BURN_RAMP_PROP, dissolveBurnRamp);
                    
                renderer.SetPropertyBlock(mpb);
            }

            foreach (var renderer in renderers) {
                if (!DEBUG) continue;
                renderer.GetPropertyBlock(mpb);
            }
        }
        
#region Saving

        public override void LoadSave(DissolveObjectSave save) { }

        [Serializable]
        public class DissolveObjectSave : SaveObject<DissolveObject> {
            public DissolveObjectSave(DissolveObject dissolveObject) : base(dissolveObject) { }
        }

#endregion
    }
}
