using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;

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
                    int invisibleLayer = LayerMask.NameToLayer("Invisible");
                    cachedLayers = renderers.Select(r => r.gameObject.layer).ToArray();
                    foreach (Renderer r in renderers) {
                        r.gameObject.layer = invisibleLayer;
                    }
                    break;
                case State.Materialized:
                case State.Dematerializing:
                case State.Materializing:
                    for (var i = 0; i < renderers.Length; i++) {
                        renderers[i].gameObject.layer = cachedLayers[i];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
    [ShowNonSerializedField, ReadOnly]
    private float timeSinceStateChanged = 0f;

    public float materializeTime = 2f;
    private const string dissolveValueProp = "_DissolveValue";
    private const string dissolveBurnSizeProp = "_DissolveBurnSize";
    private const string dissolveBurnColorProp = "_DissolveBurnColor";
    private const string dissolveBurnEmissionAmountProp = "_DissolveEmissionAmount";
    private const string dissolveTexProp = "_DissolveTex";
    private const string dissolveBurnRampProp = "_DissolveBurnRamp";
    
    [Range(0f, 1f)]
    public float dissolveAmount = 0.25f;
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
    // The layer the objects would be on if not dissolved
    private int[] cachedLayers;

    public void Dematerialize() {
        if (state == State.Materialized) {
            state = State.Dematerializing;
        }
    }

    public void Materialize() {
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
        renderers = gameObject.GetComponentsInChildren<Renderer>();
        cachedLayers = renderers.Select(r => r.gameObject.layer).ToArray();
        colliders = gameObject.GetComponentsInChildren<Collider>();
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
        if (DebugInput.GetKeyDown(KeyCode.Alpha0)) {
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
            c.enabled = state != State.Dematerialized;
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