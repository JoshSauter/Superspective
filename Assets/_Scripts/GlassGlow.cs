using UnityEngine;

[RequireComponent(typeof(SuperspectiveRenderer))]
public class GlassGlow : MonoBehaviour {
    const string emissionColorName = "_EmissionColor";
    public Color glowColor = Color.white;
    Color startEmissionColor;
    SuperspectiveRenderer thisRenderer;

    // Use this for initialization
    void Start() {
        thisRenderer = GetComponent<SuperspectiveRenderer>();
        startEmissionColor = thisRenderer.GetColor(emissionColorName);
    }

    // Update is called once per frame
    void Update() {
        Color emissionColor = glowColor * (0.5f * Mathf.Sin(Time.time) + 0.5f);
        thisRenderer.SetColor(emissionColorName, emissionColor);
    }

    void OnDisable() {
        thisRenderer.SetColor(emissionColorName, startEmissionColor);
    }
}
