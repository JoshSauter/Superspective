using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class Slider : UIControl<SliderVisual> {
    public ItemView itemView;
    public TextBlock sliderName;
    public Interactable sliderBackgroundInteractable;
    public UIBlock2D sliderBackground;
    public UIBlock2D sliderFill;
    public UIBlock2D sliderHandle;
    public NovaButton resetButton;
    public TextBlock valueTextBlock;

    public SuperspectiveTextField valueInput;
    
    public FloatSetting setting;
    private float prevValue;

    public bool handleIsHeld;
    
    private void Start() {
        sliderBackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleFillHover);
        sliderBackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleFillClick);
        sliderBackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnDrag>(HandleFillDrag);
        sliderBackgroundInteractable.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleFillRelease);
        valueInput.GetComponent<TextFieldKeyboardInput>().OnSubmit += ValueSubmit;
        valueInput.OnTextChanged += ValueSubmit;
        resetButton.OnClick += (_) => {
            UpdateValue(setting.defaultValue);
            
            // Remember the value we last clicked at
            prevValue = setting.value;
        };
    }

    private void UpdateValue(float newValue, bool playSound = true) {
        setting.value = newValue;
        Visuals.PopulateFrom(setting);

        bool differentValue = Mathf.RoundToInt(prevValue) != Mathf.RoundToInt(setting.value);
        if (playSound && differentValue) {
            AudioManager.instance.Play(AudioName.UI_ShortBlip, shouldForcePlay: true, settingsOverride: job => {
                float baseline = .12f; // min pitch
                job.basePitch = baseline + (1-baseline) * Mathf.InverseLerp(setting.minValue, setting.maxValue, setting.value);
            });
        }
    }

    private void ValueSubmit() {
        if (string.IsNullOrEmpty(valueTextBlock.Text)) {
            UpdateValue(setting.minValue);
            return;
        }
        
        if (int.TryParse(valueTextBlock.Text, out int value)) {
            UpdateValue(Mathf.Clamp(value, setting.minValue, setting.maxValue));
        }
        else {
            Debug.LogError($"Could not parse int from string {valueTextBlock.Text}");
        }
    }

    private void HandleFillHover(Gesture.OnHover evt) {
        AudioManager.instance.Play(AudioName.UI_HoverBlip, shouldForcePlay: true);
    }

    private void HandleFillClick(Gesture.OnPress evt) {
        Debug.Log($"Fill click on {gameObject.FullPath()}: {evt.Interaction.Ray}");
        Ray ray = evt.Interaction.Ray;
        Debug.Log($"Updating value to {GetValueFromMouseRay(ray)}...");
        UpdateValue(Mathf.RoundToInt(GetValueFromMouseRay(ray)), false);

        handleIsHeld = true;
    }

    private void HandleFillDrag(Gesture.OnDrag evt) {
        Debug.Log($"Fill drag on {gameObject.FullPath()}: {evt.Interaction.Ray}");
        if (!handleIsHeld) return;
        
        Ray ray = evt.Interaction.Ray;
        Debug.Log($"Updating value to {GetValueFromMouseRay(ray)}...");
        UpdateValue(GetValueFromMouseRay(ray), false);
    }

    private void HandleFillRelease(Gesture.OnRelease evt) {
        Debug.Log($"Fill release on {gameObject.FullPath()}: {evt.Interaction.Ray}");
        if (!handleIsHeld) return;

        Ray ray = evt.Interaction.Ray;
        Debug.Log($"Updating value to {GetValueFromMouseRay(ray)}...");
        UpdateValue(Mathf.RoundToInt(GetValueFromMouseRay(ray)));
        
        // Remember the value we last released at
        prevValue = setting.value;
        
        handleIsHeld = false;
    }

    float GetValueFromMouseRay(Ray worldSpaceRay) {
        // Convert the ray to a point in UIBlock slider space
        Plane textBlockPlane = new Plane(-sliderBackground.transform.forward, sliderBackground.transform.position);
        if (!textBlockPlane.Raycast(worldSpaceRay, out float hitDistance)) {
            return setting.value;
        }
        Vector3 worldSpacePos = worldSpaceRay.GetPoint(hitDistance);
        Vector3 localSpace = sliderBackground.transform.InverseTransformPoint(worldSpacePos);

        float lengthOfSlider = sliderBackground.CalculatedSize.Value.x;
        float t = Mathf.InverseLerp(0, lengthOfSlider, localSpace.x + (lengthOfSlider / 2f));
        
        return Mathf.Lerp(setting.minValue, setting.maxValue, t);
    }
}
