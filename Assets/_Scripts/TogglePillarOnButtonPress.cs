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
        thisButton.OnButtonDepressBegin += ToggleActivePillarDepress;
    }

    void ToggleActivePillarPress(Button b) {
        buttonPressedPillar.enabled = true;
        buttonDepressedPillar.enabled = false;
    }

    void ToggleActivePillarDepress(Button b) {
        buttonPressedPillar.enabled = false;
        buttonDepressedPillar.enabled = true;
    }
}