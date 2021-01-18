using System;
using NaughtyAttributes;
using PowerTrailMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ColorChangeOnPower : MonoBehaviour, SaveableObject {
    public enum ActivationTiming {
        OnPowerBegin,
        OnPowerFinish,
        OnDepowerBegin,
        OnDepowerFinish
    }

    public ActivationTiming timing = ActivationTiming.OnPowerFinish;
    public bool useMaterialAsStartColor = true;
    public Color depoweredColor;

    [ColorUsage(true, true)]
    public Color depoweredEmission;

    public Color poweredColor;

    [ColorUsage(true, true)]
    public Color poweredEmission;

    public AnimationCurve colorChangeAnimationCurve;
    public float timeToChangeColor = 0.25f;
    public PowerTrail powerTrailToReactTo;
    public EpitaphRenderer[] renderers;
    UniqueId _id;
    float timeElapsedSinceStateChange;

    public UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    bool reverseColors =>
        timing == ActivationTiming.OnDepowerBegin || timing == ActivationTiming.OnDepowerFinish;

    // Use this for initialization
    void Awake() {
        if (powerTrailToReactTo == null) powerTrailToReactTo = GetComponent<PowerTrail>();
        if (powerTrailToReactTo == null) {
            Debug.LogWarning("No Power Trail to react to, disabling color change script", gameObject);
            enabled = false;
            return;
        }

        if (renderers == null || renderers.Length == 0) renderers = GetComponents<EpitaphRenderer>();
        if (renderers == null || renderers.Length == 0) {
            renderers = new EpitaphRenderer[1];
            renderers[0] = gameObject.AddComponent<EpitaphRenderer>();
        }

        foreach (EpitaphRenderer r in renderers) {
            if (useMaterialAsStartColor) {
                depoweredColor = r.GetMainColor();
                depoweredEmission = r.GetColor("_EmissionColor");
            }
            else {
                r.SetMainColor(depoweredColor);
                r.SetColor("_EmissionColor", depoweredEmission);
            }
        }

        switch (timing) {
            case ActivationTiming.OnPowerBegin:
                powerTrailToReactTo.OnPowerBegin += PowerOn;
                powerTrailToReactTo.OnDepowerFinish += PowerOff;
                break;
            case ActivationTiming.OnPowerFinish:
                powerTrailToReactTo.OnPowerFinish += PowerOn;
                powerTrailToReactTo.OnDepowerBegin += PowerOff;
                break;
            case ActivationTiming.OnDepowerBegin:
                powerTrailToReactTo.OnDepowerBegin += PowerOn;
                powerTrailToReactTo.OnPowerFinish += PowerOff;
                break;
            case ActivationTiming.OnDepowerFinish:
                powerTrailToReactTo.OnDepowerFinish += PowerOn;
                powerTrailToReactTo.OnPowerBegin += PowerOff;
                break;
        }
    }

    void Update() {
        if (timeElapsedSinceStateChange < timeToChangeColor) timeElapsedSinceStateChange += Time.deltaTime;

        float t = timeElapsedSinceStateChange / timeToChangeColor;
        Color startColor = powerTrailToReactTo.powerIsOn && !reverseColors ? depoweredColor : poweredColor;
        Color startEmission = powerTrailToReactTo.powerIsOn && !reverseColors ? depoweredEmission : poweredEmission;
        Color endColor = powerTrailToReactTo.powerIsOn && !reverseColors ? poweredColor : depoweredColor;
        Color endEmission = powerTrailToReactTo.powerIsOn && !reverseColors ? poweredEmission : depoweredEmission;
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
        foreach (EpitaphRenderer r in renderers) {
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
    public bool SkipSave { get; set; }

    // There's only one player so we don't need a UniqueId here
    public string ID => $"{poweredColor:F3}_{id.uniqueId}";

    [Serializable]
    class ColorChangeOnPowerSave {
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

        public ColorChangeOnPowerSave(ColorChangeOnPower colorChange) {
            timing = (int) colorChange.timing;
            useMaterialAsStartColor = colorChange.useMaterialAsStartColor;
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

        public void LoadSave(ColorChangeOnPower colorChange) {
            colorChange.timing = (ActivationTiming) timing;
            colorChange.useMaterialAsStartColor = useMaterialAsStartColor;
            colorChange.depoweredColor = depoweredColor;
            colorChange.depoweredEmission = depoweredEmission;
            colorChange.poweredColor = poweredColor;
            colorChange.poweredEmission = poweredEmission;
            colorChange.colorChangeAnimationCurve = colorChangeAnimationCurve;
            colorChange.timeElapsedSinceStateChange = timeElapsedSinceStateChange;

            if (currentColor != null && currentEmission != null) colorChange.SetColor(currentColor, currentEmission);
        }
    }

    public object GetSaveObject() {
        return new ColorChangeOnPowerSave(this);
    }

    public void LoadFromSavedObject(object savedObject) {
        ColorChangeOnPowerSave save = savedObject as ColorChangeOnPowerSave;

        save.LoadSave(this);
    }
#endregion
}