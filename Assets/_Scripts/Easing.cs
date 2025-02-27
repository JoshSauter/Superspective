using UnityEngine;

public static class Easing {
    public static float EaseInOut(float t) {
        return 0.5f * (1 - Mathf.Cos(t * Mathf.PI));
    }
    
    // Inverts a smoothstep function to get the original t value
    public static float InverseSmoothStep(float t) {
        return 0.5f - Mathf.Sin(Mathf.Asin(1 - 2 * t) / 2.0f) * Mathf.Sqrt(2) / 2.0f;
    }
}
