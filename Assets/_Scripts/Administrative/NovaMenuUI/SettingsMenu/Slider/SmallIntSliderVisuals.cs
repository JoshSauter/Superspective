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
        Slider.gameObject.name = $"{setting.Name} Slider";
        Name.Text = setting.Name;
        
        float t = Mathf.InverseLerp(setting.MinValue, setting.MaxValue, setting.Value);
        Fill.Size.X.Percent = t;
        ValueTextBlock.Text = $"{Mathf.RoundToInt(setting.Value)}";
        
        smallIntSlider.CreatePips();
    }
}
