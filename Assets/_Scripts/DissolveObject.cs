using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;

namespace DissolveObjects {
    
    [RequireComponent(typeof(UniqueId))]
    public class DissolveObject : SaveableObject<DissolveObject, DissolveObject.DissolveObjectSave> {
        private const string dissolveObjectKeyword = "DISSOLVE_OBJECT";
        
        public enum State {
            Materialized,
            Dematerializing,
            Dematerialized,
            Materializing
        }

        [SerializeField]
        private State _state;
        public State state {
            get => _state;
            set {
                if (value == _state) return;
                
                timeSinceStateChanged = 0f;
                _state = value;
                switch (value) {
                    case State.Dematerialized:
                        int invisibleLayer = SuperspectivePhysics.InvisibleLayer;
                        if (IsInvisibleDimensionObj) {
                            cachedLayers = defaultLayers;
                        }
                        else {
                            cachedLayers = renderers.Select(r => r.gameObject.layer).ToArray();
                        }
                        ApplyLayers(invisibleLayer);

                        SubscribedToResetLayers = false;
                        break;
                    case State.Materialized:
                    case State.Dematerializing:
                    case State.Materializing:
                        // Fix to DissolveObjects interacting with DimensionObjects on their parent tree
                        if (IsInvisibleDimensionObj) {
                            SubscribedToResetLayers = true;
                            return;
                        }

                        ApplyLayers(cachedLayers);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        private bool IsInvisibleDimensionObj => thisDimensionObj != null && thisDimensionObj.visibilityState == VisibilityState.invisible;
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
        
        [ReadOnly]
        public float timeSinceStateChanged = 0f;

        private const float materializedColliderThreshold = 0.25f;
        private const string dissolveValueProp = "_DissolveValue";
        private const string dissolveBurnSizeProp = "_DissolveBurnSize";
        private const string dissolveBurnColorProp = "_DissolveBurnColor";
        private const string dissolveBurnEmissionAmountProp = "_DissolveEmissionAmount";
        private const string dissolveTexProp = "_DissolveTex";
        private const string dissolveBurnRampProp = "_DissolveBurnRamp";
        
        [Range(0f, 1f)]
        public float dissolveAmount = 0.25f;
        public float materializeTime = 2f;
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

        [Button("Dematerialize")]
        public void Dematerialize() {
            if (!Application.isPlaying) return;

            if (state == State.Materialized) {
                state = State.Dematerializing;
            }
        }

        [Button("Materialize")]
        public void Materialize() {
            if (!Application.isPlaying) return;

            if (state == State.Dematerialized) {
                state = State.Materializing;
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

        protected override void Awake() {
            base.Awake();
            renderers = gameObject.GetComponentsInChildrenRecursively<Renderer>();
            cachedLayers = renderers.Select(r => r.gameObject.layer).ToArray();
            defaultLayers = renderers.Select(r => r.gameObject.layer).ToArray();
            colliders = gameObject.GetComponentsInChildrenRecursively<Collider>();
            thisDimensionObj = gameObject.FindDimensionObjectRecursively<DimensionObject>();
        }

        protected override void Start() {
            base.Start();
            switch (state) {
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
        }

        // Update is called once per frame
        void Update() {
            if (DEBUG && DebugInput.GetKeyDown(KeyCode.Alpha0)) {
                state = (state == State.Materialized || state == State.Materializing)
                    ? State.Dematerializing
                    : State.Materializing;
            }
            
            UpdateState();
            SetMaterialProperties();
        }

        void UpdateState() {
            timeSinceStateChanged += Time.deltaTime;

            switch (state) {
                case State.Materialized:
                    dissolveAmount = 0f;
                    break;
                case State.Dematerializing:
                    dissolveAmount = timeSinceStateChanged / materializeTime;
                    if (dissolveAmount >= 1) {
                        state = State.Dematerialized;
                    }
                    break;
                case State.Dematerialized:
                    dissolveAmount = 1f;
                    break;
                case State.Materializing:
                    dissolveAmount = 1 - timeSinceStateChanged / materializeTime;
                    if (dissolveAmount <= 0) {
                        state = State.Materialized;
                    }
                    break;
            }

            foreach (var c in colliders) {
                bool isOverMaterializedThreshold = false;
                switch (state) {
                    case State.Materialized:
                        isOverMaterializedThreshold = true;
                        break;
                    case State.Dematerializing:
                    case State.Materializing:
                        isOverMaterializedThreshold = 1-dissolveAmount > materializedColliderThreshold;
                        break;
                    case State.Dematerialized:
                        isOverMaterializedThreshold = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                c.enabled = isOverMaterializedThreshold;
            }
        }

        void ApplyLayers(int[] layersToApply) {
            for (var i = 0; i < renderers.Length; i++) {
                renderers[i].gameObject.layer = layersToApply[i];
            }
        }

        void ApplyLayers(int layerToApply) {
            for (var i = 0; i < renderers.Length; i++) {
                renderers[i].gameObject.layer = layerToApply;
            }
        }

        void SetMaterialProperties() {
            foreach (var renderer in renderers) {
                Material material = renderer.material;
                if (state == State.Materialized) {
                    material.DisableKeyword(dissolveObjectKeyword);
                }
                else {
                    material.EnableKeyword(dissolveObjectKeyword);
                }
                material.SetFloat(dissolveValueProp, dissolveAmount);
                material.SetFloat(dissolveBurnSizeProp, burnSize);
                material.SetColor(dissolveBurnColorProp, burnColor);
                material.SetFloat(dissolveBurnEmissionAmountProp, burnEmissionBrightness);
                material.SetTexture(dissolveTexProp, dissolveTexture);
                material.SetTexture(dissolveBurnRampProp, dissolveBurnRamp);
            }
        }
        
        #region Saving

        [Serializable]
        public class DissolveObjectSave : SerializableSaveObject<DissolveObject> {
            private State state;
            private float timeSinceStateChanged;
            private float materializeTime;
            private float dissolveAmount;
            private float burnSize;
            private SerializableColor burnColor;
            private float burnEmissionBrightness;
            public bool advancedOptions;
            private SerializableAnimationCurve dissolveAnimationCurve;
            private int[] cachedLayers;
            
            public DissolveObjectSave(DissolveObject dissolveObject) : base(dissolveObject) {
                this.state = dissolveObject.state;
                this.timeSinceStateChanged = dissolveObject.timeSinceStateChanged;
                this.materializeTime = dissolveObject.materializeTime;
                this.dissolveAmount = dissolveObject.dissolveAmount;
                this.burnSize = dissolveObject.burnSize;
                this.burnColor = dissolveObject.burnColor;
                this.burnEmissionBrightness = dissolveObject.burnEmissionBrightness;
                this.advancedOptions = dissolveObject.advancedOptions;
                this.dissolveAnimationCurve = dissolveObject.dissolveAnimationCurve;
                this.cachedLayers = (int[])dissolveObject.cachedLayers.Clone();
            }

            public override void LoadSave(DissolveObject dissolveObject) {
                dissolveObject.state = this.state;
                dissolveObject.timeSinceStateChanged = this.timeSinceStateChanged;
                dissolveObject.materializeTime = this.materializeTime;
                dissolveObject.dissolveAmount = this.dissolveAmount;
                dissolveObject.burnSize = this.burnSize;
                dissolveObject.burnColor = this.burnColor;
                dissolveObject.burnEmissionBrightness = this.burnEmissionBrightness;
                dissolveObject.advancedOptions = this.advancedOptions;
                dissolveObject.dissolveAnimationCurve = this.dissolveAnimationCurve;
                dissolveObject.cachedLayers = (int[])this.cachedLayers.Clone();
            }
        }
        #endregion
    }
}
