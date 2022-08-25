using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;

public class SliderVisual : ItemVisuals {
    public Slider Slider;
    public TextBlock Name;
    public TextBlock ValueTextBlock;
    public UIBlock2D Fill;
    public UIBlock2D Handle;

    public void PopulateFrom(FloatSetting setting) {
        Slider.setting = setting;
        Slider.gameObject.name = $"{setting.Name} Slider";
        Name.Text = setting.Name;
        
        float t = Mathf.InverseLerp(setting.MinValue, setting.MaxValue, setting.Value);
        Fill.Size.X.Percent = t;
        ValueTextBlock.Text = $"{(int)setting.Value}";
    }
}
