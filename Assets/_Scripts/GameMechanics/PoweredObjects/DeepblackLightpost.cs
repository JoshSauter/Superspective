using Deepblack;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(DeepblackObject))]
public class DeepblackLightpost : Lightpost {
    protected override bool IsEmissive => false;
    
    public DeepblackObject deepblack;

    [Range(0f, 1f)]
    public float lerpValueToStartDeepblack = 0.25f;
    public float maxDarkness = 2.5f;

    private Color startColor;
    
    protected void Awake() {
        if (deepblack == null) {
            deepblack = gameObject.GetOrAddComponent<DeepblackObject>();
        }

        if (r == null) {
            r = this.GetOrAddComponent<SuperspectiveRenderer>();
        }

        startColor = r.GetMainColor();
    }
    
    protected override void UpdateVisuals() {
        if (t < lerpValueToStartDeepblack) {
            float adjustedT = t / lerpValueToStartDeepblack;
            r.SetMainColor(Color.Lerp(startColor, Color.black, adjustedT));
        }
        else {
            r.SetMainColor(Color.black);
            float adjustedT = (t - lerpValueToStartDeepblack) / (1 - lerpValueToStartDeepblack);
            deepblack.darkness = adjustedT * maxDarkness;
        }
    }
}
