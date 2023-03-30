using System;
using NaughtyAttributes;
using UnityEngine;

public class ButtonColorChange : MonoBehaviour {
    public bool useMaterialAsStartColor = false;
    public bool useMaterialAsEndColor = false;
    public bool ignoreEmission = false;
    public bool overrideColorProperty = false;
    
    [HideIf("useMaterialAsStartColor")]
    public Color startColor;
    [ColorUsage(true, true), HideIf(EConditionOperator.Or, "ignoreEmission", "useMaterialAsStartColor")]
    public Color startEmission = Color.black;

    [ShowIf("overrideColorProperty")]
    public string colorPropertyOverride = "";

    [HideIf("useMaterialAsEndColor")]
    public Color pressColor = Color.white;
    [ColorUsage(true, true), HideIf(EConditionOperator.Or, "ignoreEmission", "useMaterialAsEndColor")]
    public Color pressEmission = Color.black;

    public Button buttonToReactTo;
    SuperspectiveRenderer r;

    private const float lerpSpeed = 2f;

    // Use this for initialization
    void Start() {
        if (buttonToReactTo == null) buttonToReactTo = GetComponent<Button>();
        if (buttonToReactTo == null) {
            Debug.LogWarning("No button to react to, disabling color change script", gameObject);
            enabled = false;
            return;
        }

        r = GetComponent<SuperspectiveRenderer>();
        if (r == null) r = gameObject.AddComponent<SuperspectiveRenderer>();

        if (useMaterialAsEndColor) {
            pressColor = overrideColorProperty ? r.GetColor(colorPropertyOverride) : r.GetMainColor();

            pressEmission = r.GetColor("_EmissionColor");
        }
        
        if (useMaterialAsStartColor) {
            startColor = overrideColorProperty ? r.GetColor(colorPropertyOverride) : r.GetMainColor();
            startEmission = r.GetColor("_EmissionColor");
        }
        else {
            SetColor(startColor);
            SetEmission(startEmission);
        }

        buttonToReactTo.OnButtonPressFinish += ButtonPressFinish;
        buttonToReactTo.OnButtonUnpressFinish += ButtonUnpressFinish;
    }

    void Update() {
        UpdateColor();
    }

    [Button("Swap powered/depowered colors")]
    void SwapPoweredDepoweredColors() {
        Color tempColor = startColor;
        Color tempEmission = startEmission;

        startColor = pressColor;
        startEmission = pressEmission;

        pressColor = tempColor;
        pressEmission = tempEmission;
    }

    void UpdateColor() {
        float t;
        switch (buttonToReactTo.state) {
            case Button.State.ButtonPressing:
                t = buttonToReactTo.timeSinceStateChange / buttonToReactTo.timeToPressButton;

                SetColor(Color.Lerp(startColor, pressColor, buttonToReactTo.buttonPressCurve.Evaluate(t)));
                SetEmission(Color.Lerp(startEmission, pressEmission, buttonToReactTo.buttonPressCurve.Evaluate(t)));

                break;
            case Button.State.ButtonUnpressing:
                t = buttonToReactTo.timeSinceStateChange / buttonToReactTo.timeToUnpressButton;

                SetColor(Color.Lerp(pressColor, startColor, buttonToReactTo.buttonUnpressCurve.Evaluate(t)));
                SetEmission(Color.Lerp(pressEmission, startEmission, buttonToReactTo.buttonUnpressCurve.Evaluate(t)));

                break;
            case Button.State.ButtonUnpressed:
                SetColor(Color.Lerp(GetCurrentColor(), startColor, Time.deltaTime*lerpSpeed));
                SetEmission(Color.Lerp(GetCurrentEmission(), startEmission, Time.deltaTime*lerpSpeed));
                break;
            case Button.State.ButtonPressed:
                SetColor(Color.Lerp(GetCurrentColor(), pressColor, Time.deltaTime*lerpSpeed));
                SetEmission(Color.Lerp(GetCurrentEmission(), pressEmission, Time.deltaTime*lerpSpeed));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void ButtonPressFinish(Button b) {
        SetColor(pressColor);
        SetEmission(pressEmission);
    }

    void ButtonUnpressFinish(Button b) {
        SetColor(startColor);
        SetEmission(startEmission);
    }

    Color GetCurrentColor() {
        return overrideColorProperty ? r.GetColor(colorPropertyOverride) : r.GetMainColor();
    }

    Color GetCurrentEmission() {
        return r.GetColor("_EmissionColor");
    }

    void SetColor(Color color) {
        if (overrideColorProperty) {
            r.SetColor(colorPropertyOverride, color);
        }
        else {
            r.SetMainColor(color);
        }

    }

    void SetEmission(Color color) {
        if (!ignoreEmission) {
            r.SetColor("_EmissionColor", color);
        }
    }
}