using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class TogglePillarOnButtonPress : MonoBehaviour {
	private Button thisButton;
	public ObscurePillar buttonPressedPillar;
	public ObscurePillar buttonDepressedPillar;

	// Use this for initialization
	void Start () {
		thisButton = GetComponent<Button>();
		thisButton.OnButtonPressBegin += ToggleActivePillarPress;
		thisButton.OnButtonDepressBegin += ToggleActivePillarDepress;
	}
	
	void ToggleActivePillarPress(Button b) {
		ObscurePillar.activePillar = buttonPressedPillar;
	}
	void ToggleActivePillarDepress(Button b) {
		ObscurePillar.activePillar = buttonDepressedPillar;
	}
}
