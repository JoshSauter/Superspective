using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class ProjectorPowerButton : MonoBehaviour {
	public bool lightTurnedOn = false;
	public LightProjector projector;
	Button b;

    void Start() {
		b = GetComponent<Button>();
		b.OnButtonPressBegin += TurnOnProjector;
		b.OnButtonDepressBegin += TurnOffProjector;

		if (lightTurnedOn) {
			b.PressButton();
		}
    }

	void TurnOnProjector(Button unused) {
		foreach (Transform child in projector.transform) {
			child.gameObject.SetActive(true);
		}
		lightTurnedOn = true;
	}
	void TurnOffProjector(Button unused) {
		foreach (Transform child in projector.transform) {
			child.gameObject.SetActive(false);
		}
		lightTurnedOn = false;
	}
}
