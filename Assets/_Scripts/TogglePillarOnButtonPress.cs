using UnityEngine;

[RequireComponent(typeof(Button))]
public class TogglePillarOnButtonPress : MonoBehaviour {
    public DimensionPillar buttonPressedPillar;
    public DimensionPillar buttonDepressedPillar;
    Button thisButton;

    // Use this for initialization
    void Start() {
        thisButton = GetComponent<Button>();
        thisButton.OnButtonPressBegin += ToggleActivePillarPress;
        thisButton.OnButtonUnpressBegin += ToggleActivePillarUnpress;
    }

    void ToggleActivePillarPress(Button b) {
        buttonPressedPillar.enabled = true;
        buttonDepressedPillar.enabled = false;
    }

    void ToggleActivePillarUnpress(Button b) {
        buttonPressedPillar.enabled = false;
        buttonDepressedPillar.enabled = true;
    }
}