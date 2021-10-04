using NaughtyAttributes;
using UnityEngine;

public class ButtonColorChange : MonoBehaviour {
    public bool useMaterialAsStartColor = false;
    public bool useMaterialAsEndColor = false;
    public Color startColor;

    [ColorUsage(true, true)]
    public Color startEmission = Color.black;

    public Color pressColor = Color.white;

    [ColorUsage(true, true)]
    public Color pressEmission = Color.black;

    public Button buttonToReactTo;
    SuperspectiveRenderer r;

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
            pressColor = r.GetMainColor();
            pressEmission = r.GetColor("_EmissionColor");
        }
        
        if (useMaterialAsStartColor) {
            startColor = r.GetMainColor();
            startEmission = r.GetColor("_EmissionColor");
        }
        else {
            r.SetMainColor(startColor);
            r.SetColor("_EmissionColor", startEmission);
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

                r.SetMainColor(Color.Lerp(startColor, pressColor, buttonToReactTo.buttonPressCurve.Evaluate(t)));
                r.SetColor(
                    "_EmissionColor",
                    Color.Lerp(startEmission, pressEmission, buttonToReactTo.buttonPressCurve.Evaluate(t))
                );
                break;
            case Button.State.ButtonUnpressing:
                t = buttonToReactTo.timeSinceStateChange / buttonToReactTo.timeToUnpressButton;

                r.SetMainColor(Color.Lerp(pressColor, startColor, buttonToReactTo.buttonUnpressCurve.Evaluate(t)));
                r.SetColor(
                    "_EmissionColor",
                    Color.Lerp(pressEmission, startEmission, buttonToReactTo.buttonUnpressCurve.Evaluate(t))
                );
                break;
        }
    }

    void ButtonPressFinish(Button b) {
        r.SetMainColor(pressColor);
        r.SetColor("_EmissionColor", pressEmission);
    }

    void ButtonUnpressFinish(Button b) {
        r.SetMainColor(startColor);
        r.SetColor("_EmissionColor", startEmission);
    }
}