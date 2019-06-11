using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class ProjectorControls : MonoBehaviour {
	public LightProjector projector;

	public ButtonHold projectorSizeIncreaseButton;
	public ButtonHold projectorSizeDecreaseButton;

	public ValveControl projectorRotateValve;

	public ButtonHold projectorRotateAxisLeftButton;
	public ButtonHold projectorRotateAxisRightButton;

	public ButtonHold projectorRotateAxisDownButton;
	public ButtonHold projectorRotateAxisUpButton;

	// Use this for initialization
	void Start () {
		projectorSizeIncreaseButton.OnButtonHeld += IncreaseFrustumSize;
		projectorSizeDecreaseButton.OnButtonHeld += DecreaseFrustumSize;

		projectorRotateValve.OnValveRotate += RotateProjector;

		projectorRotateAxisLeftButton.OnButtonHeld += RotateProjectorLeftOnAxis;
		projectorRotateAxisRightButton.OnButtonHeld += RotateProjectorRightOnAxis;

		projectorRotateAxisDownButton.OnButtonHeld += RotateProjectorDownOnAxis;
		projectorRotateAxisUpButton.OnButtonHeld += RotateProjectorUpOnAxis;
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
		projector.RotateAroundCircumference(-diff.degrees);
	}

	void RotateProjectorLeftOnAxis(Button unused) {
		projector.RotateAngleLeft();
	}

	void RotateProjectorRightOnAxis(Button unused) {
		projector.RotateAngleRight();
	}

	void RotateProjectorDownOnAxis(Button unused) {
		projector.RotateAngleDown();
	}

	void RotateProjectorUpOnAxis(Button unused) {
		projector.RotateAngleUp();
	}
}
