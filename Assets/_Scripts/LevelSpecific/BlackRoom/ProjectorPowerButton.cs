﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class ProjectorPowerButton : MonoBehaviour {
	public PowerTrail powerTrail;
	public bool lightTurnedOn = false;
	public LightProjector projector;
	Button b;

    IEnumerator Start() {
		yield return null;
		b = GetComponent<Button>();
		b.OnButtonPressBegin += ctx => TurnOnPowerTrail();
		b.OnButtonDepressBegin += ctx => TurnOffPowerTrail();
		powerTrail.OnPowerFinish += TurnOnProjector;
		powerTrail.OnDepowerBegin += TurnOffProjector;

		if (lightTurnedOn) {
			b.PressButton();
		}
    }

	void TurnOnPowerTrail() {
		powerTrail.powerIsOn = true;
	}

	void TurnOffPowerTrail() {
		powerTrail.powerIsOn = false;
	}

	void TurnOnProjector() {
		foreach (Transform child in projector.transform) {
			child.gameObject.SetActive(true);
		}
		lightTurnedOn = true;
	}
	void TurnOffProjector() {
		foreach (Transform child in projector.transform) {
			child.gameObject.SetActive(false);
		}
		lightTurnedOn = false;
	}
}