using System.Collections;
using System.Collections.Generic;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class SliderVisual : ItemVisuals {
    public Slider Slider;
    public NovaButton HoverButton; // Just used for the hovering over settings visuals
    public TextBlock Name;
    public TextBlock ValueTextBlock;
    public UIBlock2D Fill;
    public UIBlock2D Handle;
    public UIBlock2D DisabledOverlay;

    public virtual void PopulateFrom(FloatSetting setting) {
        Slider.setting = setting;
        Slider.gameObject.name = $"[Slider] {setting.name}";
        Name.Text = setting.name;
        
        float t = Mathf.InverseLerp(setting.minValue, setting.maxValue, setting.value);
        Fill.Size.X.Percent = t;
        ValueTextBlock.Text = $"{Mathf.RoundToInt(setting.value)}";

        DisabledOverlay.Color = setting.isEnabled ? UIStyle.Settings.DisabledOverlayColor.WithAlpha(0f) : UIStyle.Settings.DisabledOverlayColor;
        Slider.sliderBackgroundInteractable.enabled = setting.isEnabled;
        HoverButton.isEnabled = setting.isEnabled;
        Slider.valueInput.GetComponent<Interactable>().enabled = setting.isEnabled;
        Slider.resetButton.isEnabled = setting.isEnabled;
    }
}
