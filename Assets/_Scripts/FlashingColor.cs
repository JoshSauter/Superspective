
using System;
using NaughtyAttributes;
using UnityEngine;

public class FlashingColor : MonoBehaviour {
    public enum Mode {
        Additive,
        Subtractive,
        Replace
    }

    public Mode mode;
    
    public enum State {
        NotFlashing,
        Flashing
    }

    [SerializeField]
    private State _state;

    public State state {
        get => _state;
        set {
            if (value != state) {
                timeSinceStateChanged = 0f;
            }

            _state = value;
        }
    }
    private float timeSinceStateChanged = 0f;
    private float flashAmount = 0f;
    private float timesToFlash = 1f;

    public AnimationCurve flashCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public float flashDuration = 1f; // Time to cycle to full flash and back off once
    public Color flashColor = Color.red;
    [ColorUsage(true, true)]
    public Color flashEmission = Color.red;

    private bool ReplaceMode => mode == Mode.Replace;
    [ShowIf("ReplaceMode")]
    public Color startColor;
    [ShowIf("ReplaceMode")]
    [ColorUsage(true, true)]
    public Color startEmission;

    public SuperspectiveRenderer renderer;

    public void Awake() {
        if (renderer == null) {
            renderer = gameObject.GetComponent<SuperspectiveRenderer>();
            if (renderer == null) {
                renderer = gameObject.AddComponent<SuperspectiveRenderer>();
            }
        }
    }

    public void Update() {
        timeSinceStateChanged += Time.deltaTime;

        if (state == State.Flashing) {
            UpdateFlashing();
        }
    }

    void UpdateFlashing() {
        float timesFlashed = timeSinceStateChanged / flashDuration;
        if (timesFlashed >= timesToFlash) {
            state = State.NotFlashing;
            return;
        }

        float t = timesFlashed % 1f;
        // Reverse animation in second half to flash back off
        if (t > 0.5f) {
            t = 1 - t;
        }

        float prevFlashAmount = flashAmount;
        flashAmount = flashCurve.Evaluate(t);
        float diff = flashAmount - prevFlashAmount;

        Color curColor = renderer.GetColor(SuperspectiveRenderer.mainColor);
        Color curEmission = renderer.GetColor(SuperspectiveRenderer.emissionColor);

        Color nextColor = curColor;
        Color nextEmission = curEmission;
        switch (mode) {
            case Mode.Additive:
                nextColor += diff * flashColor;
                nextEmission += diff * flashEmission;
                break;
            case Mode.Subtractive:
                nextColor -= diff * flashColor;
                nextEmission -= diff * flashEmission;
                break;
            case Mode.Replace:
                nextColor = Color.Lerp(startColor, flashColor, t);
                nextEmission = Color.Lerp(startEmission, flashEmission, t);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        renderer.SetColor(SuperspectiveRenderer.mainColor, nextColor);
        renderer.SetColor(SuperspectiveRenderer.emissionColor, nextEmission);
    }

    public void Flash(int times) {
        timesToFlash = times;
        state = State.Flashing;
    }
}