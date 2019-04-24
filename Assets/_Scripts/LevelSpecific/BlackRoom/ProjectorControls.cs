using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectorControls : MonoBehaviour {
	public LightProjector projector;

	public ButtonHold projectorSizeIncreaseButton;
	public ButtonHold projectorSizeDecreaseButton;

	// Use this for initialization
	void Start () {
		projectorSizeIncreaseButton.OnButtonHeld += IncreaseFrustumSize;
		projectorSizeDecreaseButton.OnButtonHeld += DecreaseFrustumSize;
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
}
