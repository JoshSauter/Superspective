using System;
using PowerTrailMechanics;
using System.Collections;
using PoweredObjects;
using Saving;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class Lightpost : SuperspectiveObject<Lightpost, Lightpost.LightpostSave> {

    protected virtual bool IsEmissive => true;
    
    [ShowIf(nameof(DistanceMode))]
    public PowerTrail powerTrail;
    [HideIf(nameof(DistanceMode))]
    public PoweredObject powerSource;
    
    [ShowIf(nameof(IsEmissive)), ColorUsage(true, true)]
    public Color emissiveColor;
    [ShowIf(nameof(IsEmissive))]
    Color startEmission;

    public enum TriggerMode : byte {
        Distance,
        PowerFinish
    }
    public TriggerMode triggerMode;
    private bool DistanceMode => triggerMode == TriggerMode.Distance;
    [ShowIf(nameof(DistanceMode))]
    public float turnOnAtDistance;
    public float turnOnSpeed = 4f;

    [ShowInInspector, ReadOnly]
    protected float t = 0f;
    protected SuperspectiveRenderer r;

    const string EMISSION_COLOR_KEY = "_EmissionColor";

    bool Powered {
        get {
            switch (triggerMode) {
                case TriggerMode.Distance:
                    return powerTrail.distance > turnOnAtDistance;
                case TriggerMode.PowerFinish:
                    return powerSource.FullyPowered;
                default:
                    return false;
            }
        }
    }

    protected override void Awake() {
        base.Awake();
        
        if (r == null) {
            r = this.GetOrAddComponent<SuperspectiveRenderer>();
        }
    }

    protected override void Start() {
        base.Start();

        startEmission = r.GetColor(EMISSION_COLOR_KEY);
    }

    void Update() {
        if (Powered) {
            float delta = Mathf.Clamp01(t + Time.deltaTime * turnOnSpeed) - t;
            if (delta > 0) {
                t += delta;
                UpdateVisuals();
            }
        }
        else {
            float delta = Mathf.Clamp01(t - Time.deltaTime * turnOnSpeed) - t;
            if (delta < 0) {
                t += delta;
                UpdateVisuals();
            }
        }
    }

    public override void LoadSave(LightpostSave save) {
        UpdateVisuals();
    }

    protected virtual void UpdateVisuals() {
        r.SetColor(EMISSION_COLOR_KEY, Color.Lerp(startEmission, emissiveColor, t));
    }
    
    [Serializable]
    public class LightpostSave : SaveObject<Lightpost> {
        public LightpostSave(Lightpost saveableObject) : base(saveableObject) { }
    }
}
