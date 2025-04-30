using System;
using Saving;
using SerializableClasses;
using UnityEngine;
using System.Linq;
using PoweredObjects;
using Sirenix.OdinInspector;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class ColorChangeOnPower : SuperspectiveObject<ColorChangeOnPower, ColorChangeOnPower.ColorChangeOnPowerSave> {
    
    public enum ActivationTiming : byte {
        OnPowerBegin,
        OnPowerFinish,
        OnDepowerBegin,
        OnDepowerFinish
    }

    public ActivationTiming timing = ActivationTiming.OnPowerFinish;
    public bool useMaterialAsStartColor = false;
    public bool useMaterialAsEndColor = false;
    
    [HideIf(nameof(useMaterialAsStartColor))]
    public Color depoweredColor;
    [HideIf(nameof(useMaterialAsStartColor))]
    [ColorUsage(true, true)]
    public Color depoweredEmission;
    [HideIf(nameof(useMaterialAsEndColor))]
    public Color poweredColor;
    [HideIf(nameof(useMaterialAsEndColor))]
    [ColorUsage(true, true)]
    public Color poweredEmission;

    public AnimationCurve colorChangeAnimationCurve;
    public float timeToChangeColor = 0.25f;
    [Space]
    public MultiMode mode = MultiMode.Single;
    [HideIf("IsMulti")]
    public PoweredObject poweredObjectToReactTo;
    [ShowIf("IsMulti")]
    public PoweredObject[] poweredObjectsToReactTo;
    public SuperspectiveRenderer[] renderers;
    float timeElapsedSinceStateChange;

    bool IsMulti() {
        return mode != MultiMode.Single;
    }

    bool reverseColors =>
        timing == ActivationTiming.OnDepowerBegin || timing == ActivationTiming.OnDepowerFinish;

    // Use this for initialization
    protected override void Awake() {
        base.Awake();
        if (poweredObjectToReactTo == null) poweredObjectToReactTo = GetComponent<PoweredObject>();
        if (poweredObjectToReactTo == null && !IsMulti()) {
            Debug.LogWarning("No Power Trail to react to, disabling color change script", gameObject);
            enabled = false;
            return;
        }

        if (renderers == null || renderers.Length == 0) renderers = GetComponents<SuperspectiveRenderer>();
        if (renderers == null || renderers.Length == 0) {
            renderers = new SuperspectiveRenderer[1];
            renderers[0] = gameObject.AddComponent<SuperspectiveRenderer>();
        }

        if (!IsMulti()) {
            if (poweredObjectToReactTo == null) {
                debug.LogError($"PoweredObjectToReactTo is null on {poweredObjectToReactTo.FullPath()}, disabling color change script", true);
                enabled = false;
                return;
            }
            
            switch (timing) {
                case ActivationTiming.OnPowerBegin:
                    poweredObjectToReactTo.OnPowerBegin += PowerOn;
                    poweredObjectToReactTo.OnDepowerFinish += PowerOff;
                    break;
                case ActivationTiming.OnPowerFinish:
                    poweredObjectToReactTo.OnPowerFinish += PowerOn;
                    poweredObjectToReactTo.OnDepowerBegin += PowerOff;
                    break;
                case ActivationTiming.OnDepowerBegin:
                    poweredObjectToReactTo.OnDepowerBegin += PowerOn;
                    poweredObjectToReactTo.OnPowerFinish += PowerOff;
                    break;
                case ActivationTiming.OnDepowerFinish:
                    poweredObjectToReactTo.OnDepowerFinish += PowerOn;
                    poweredObjectToReactTo.OnPowerBegin += PowerOff;
                    break;
            }
        }
        else {
            switch (timing) {
                case ActivationTiming.OnPowerBegin:
                    foreach (var pt in poweredObjectsToReactTo) {
                        pt.OnPowerBegin += PowerOn;
                        pt.OnDepowerFinish += PowerOff;
                    }
                    break;
                case ActivationTiming.OnPowerFinish:
                    foreach (var pt in poweredObjectsToReactTo) {
                        pt.OnPowerFinish += PowerOn;
                        pt.OnDepowerBegin += PowerOff;
                    }

                    break;
                case ActivationTiming.OnDepowerBegin:
                    foreach (var pt in poweredObjectsToReactTo) {
                        pt.OnDepowerBegin += PowerOn;
                        pt.OnPowerFinish += PowerOff;
                    }

                    break;
                case ActivationTiming.OnDepowerFinish:
                    foreach (var pt in poweredObjectsToReactTo) {
                        pt.OnDepowerFinish += PowerOn;
                        pt.OnPowerBegin += PowerOff;
                    }

                    break;
            }
        }
    }

    protected override void Start() {
        base.Start();

        foreach (SuperspectiveRenderer r in renderers) {
            if (useMaterialAsEndColor) {
                poweredColor = r.GetMainColor();
                poweredEmission = r.GetColor("_EmissionColor");
            }

            if (useMaterialAsStartColor) {
                depoweredColor = r.GetMainColor();
                depoweredEmission = r.GetColor("_EmissionColor");
            }
            else {
                r.SetMainColor(depoweredColor);
                r.SetColor("_EmissionColor", depoweredEmission);
            }
        }
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        if (timeElapsedSinceStateChange < timeToChangeColor) timeElapsedSinceStateChange += Time.deltaTime;

        float t = timeElapsedSinceStateChange / timeToChangeColor;
        bool PowerIsOn() {
            switch (mode) {
                case MultiMode.Single:
                    return poweredObjectToReactTo.PowerIsOn;
                case MultiMode.Any:
                    return poweredObjectsToReactTo.ToList().Exists(pt => pt.PowerIsOn);
                case MultiMode.All:
                    return poweredObjectsToReactTo.ToList().TrueForAll(pt => pt.PowerIsOn);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        bool powerIsOn = PowerIsOn();

        Color startColor = powerIsOn && !reverseColors ? depoweredColor : poweredColor;
        Color startEmission = powerIsOn && !reverseColors ? depoweredEmission : poweredEmission;
        Color endColor = powerIsOn && !reverseColors ? poweredColor : depoweredColor;
        Color endEmission = powerIsOn && !reverseColors ? poweredEmission : depoweredEmission;
        if (t > 1) {
            timeElapsedSinceStateChange = timeToChangeColor;
            SetColor(endColor, endEmission);
        }
        else if (t < 1) {
            float animationTime = colorChangeAnimationCurve.Evaluate(t);
            SetColor(
                Color.Lerp(startColor, endColor, animationTime),
                Color.Lerp(startEmission, endEmission, animationTime)
            );
        }
    }

    [Button("Swap powered/depowered colors")]
    void SwapPoweredDepoweredColors() {
        Color tempColor = depoweredColor;
        Color tempEmission = depoweredEmission;

        depoweredColor = poweredColor;
        depoweredEmission = poweredEmission;

        poweredColor = tempColor;
        poweredEmission = tempEmission;
    }

    void SetColor(Color color, Color emission) {
        foreach (SuperspectiveRenderer r in renderers) {
            r.SetMainColor(color);
            r.SetColor("_EmissionColor", emission);
        }
    }

    void PowerOn() {
        timeElapsedSinceStateChange = 0f;
    }

    void PowerOff() {
        timeElapsedSinceStateChange = 0f;
    }

#region Saving

    public override void LoadSave(ColorChangeOnPowerSave save) {
        if (save.currentColor != null && save.currentEmission != null) SetColor(save.currentColor, save.currentEmission);
    }

    public override string ID => $"{poweredColor:F3}_{id.uniqueId}";

    [Serializable]
    public class ColorChangeOnPowerSave : SaveObject<ColorChangeOnPower> {
        public SerializableColor currentColor;
        public SerializableColor currentEmission;

        public ColorChangeOnPowerSave(ColorChangeOnPower colorChange) : base(colorChange) {
            if (colorChange.renderers != null && colorChange.renderers.Length > 0) {
                currentColor = colorChange.renderers[0].GetMainColor();
                currentEmission = colorChange.renderers[0].GetColor("_EmissionColor");
            }
        }
    }
#endregion
}