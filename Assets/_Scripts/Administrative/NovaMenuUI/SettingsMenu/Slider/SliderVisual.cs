using System.Collections;
using System.Collections.Generic;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

public class SliderVisual : ItemVisuals {
    private const float disabledAlpha = .65f;
    
    public Slider Slider;
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

        DisabledOverlay.Color = DisabledOverlay.Color.WithAlpha(setting.isEnabled ? 0f : disabledAlpha);
        Slider.sliderBackgroundInteractable.enabled = setting.isEnabled;
    }
}
