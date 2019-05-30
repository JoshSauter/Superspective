using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class ProjectorControls : MonoBehaviour {
	public LightProjector projector;

	public ButtonHold projectorSizeIncreaseButton;
	public ButtonHold projectorSizeDecreaseButton;

	public ValveControl projectorRotateValve;

	// Use this for initialization
	void Start () {
		projectorSizeIncreaseButton.OnButtonHeld += IncreaseFrustumSize;
		projectorSizeDecreaseButton.OnButtonHeld += DecreaseFrustumSize;

		projectorRotateValve.OnValveRotate += RotateProjector;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void IncreaseFrustumSize(Button unused) {
		projector.IncreaseFrustumSize();
	}

	void DecreaseFrustumSize(Button unused) {
		projector.DecreaseFrustumSize();
	}

	void RotateProjector(Angle diff) {
		projector.RotateAroundCircumference(5 * diff.degrees);
	}
}
