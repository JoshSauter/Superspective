using System;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using UnityEngine;
using System.Linq;
using PoweredObjects;
using SuperspectiveUtils;

[RequireComponent(typeof(UniqueId))]
public class ColorChangeOnPower : SaveableObject<ColorChangeOnPower, ColorChangeOnPower.ColorChangeOnPowerSave> {
    
    public enum ActivationTiming {
        OnPowerBegin,
        OnPowerFinish,
        OnDepowerBegin,
        OnDepowerFinish
    }

    public ActivationTiming timing = ActivationTiming.OnPowerFinish;
    public bool useMaterialAsStartColor = false;
    public bool useMaterialAsEndColor = false;
    public Color depoweredColor;

    [ColorUsage(true, true)]
    public Color depoweredEmission;

    public Color poweredColor;

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

        if (!IsMulti()) {
            if (poweredObjectToReactTo == null) {
                new DebugLogger(this).LogError($"PoweredObjectToReactTo is null on {poweredObjectToReactTo.FullPath()}, disabling color change script");
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

    void Update() {
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
    public override string ID => $"{poweredColor:F3}_{id.uniqueId}";

    [Serializable]
    public class ColorChangeOnPowerSave : SerializableSaveObject<ColorChangeOnPower> {
        SerializableAnimationCurve colorChangeAnimationCurve;

        SerializableColor currentColor;
        SerializableColor currentEmission;
        SerializableColor depoweredColor;
        SerializableColor depoweredEmission;
        SerializableColor poweredColor;
        SerializableColor poweredEmission;
        float timeElapsedSinceStateChange;
        int timing;
        bool useMaterialAsStartColor;
        private bool useMaterialAsEndColor;

        public ColorChangeOnPowerSave(ColorChangeOnPower colorChange) : base(colorChange) {
            timing = (int) colorChange.timing;
            useMaterialAsStartColor = colorChange.useMaterialAsStartColor;
            useMaterialAsEndColor = colorChange.useMaterialAsEndColor;
            depoweredColor = colorChange.depoweredColor;
            depoweredEmission = colorChange.depoweredEmission;
            poweredColor = colorChange.poweredColor;
            poweredEmission = colorChange.poweredEmission;
            colorChangeAnimationCurve = colorChange.colorChangeAnimationCurve;
            timeElapsedSinceStateChange = colorChange.timeElapsedSinceStateChange;

            if (colorChange.renderers != null && colorChange.renderers.Length > 0) {
                currentColor = colorChange.renderers[0].GetMainColor();
                currentEmission = colorChange.renderers[0].GetColor("_EmissionColor");
            }
        }

        public override void LoadSave(ColorChangeOnPower colorChange) {
            colorChange.timing = (ActivationTiming) timing;
            colorChange.useMaterialAsStartColor = useMaterialAsStartColor;
            colorChange.useMaterialAsEndColor = useMaterialAsEndColor;
            colorChange.depoweredColor = depoweredColor;
            colorChange.depoweredEmission = depoweredEmission;
            colorChange.poweredColor = poweredColor;
            colorChange.poweredEmission = poweredEmission;
            colorChange.colorChangeAnimationCurve = colorChangeAnimationCurve;
            colorChange.timeElapsedSinceStateChange = timeElapsedSinceStateChange;

            if (currentColor != null && currentEmission != null) colorChange.SetColor(currentColor, currentEmission);
        }
    }
#endregion
}