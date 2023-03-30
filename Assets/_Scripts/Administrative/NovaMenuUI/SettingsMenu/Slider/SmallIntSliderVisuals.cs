using UnityEngine;

public class SmallIntSliderVisuals : SliderVisual {
    public SmallIntSlider smallIntSlider;

    // This is some of the smelliest code I ever seen
    public override void PopulateFrom(FloatSetting setting) {
        base.PopulateFrom(setting);
        smallIntSlider.UpdatePipColors();
    }
    
    public void PopulateFrom(SmallIntSetting setting) {
        smallIntSlider.setting = setting;
        Slider.gameObject.name = $"[Slider] {setting.name}";
        Name.Text = setting.name;
        
        float t = Mathf.InverseLerp(setting.minValue, setting.maxValue, setting.value);
        Fill.Size.X.Percent = t;
        ValueTextBlock.Text = $"{Mathf.RoundToInt(setting.value)}";
        
        smallIntSlider.CreatePips();
    }
}
