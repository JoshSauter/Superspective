using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoPillarHallwayPanel : Panel {
	public PhaseInOutMaterialAnimator pillarAnimator;

	private void Awake() {
		colorLerpTime = pillarAnimator.animationTime;
		OnPanelActivateBegin += PhasePillarIn;
		OnPanelDeactivateBegin += PhasePillarOut;
	}

	// Use this for initialization
	override protected void Start () {
		base.Start();
		gemButton.PressButton();
	}

	void PhasePillarIn() {
		pillarAnimator.gameObject.SetActive(true);
		pillarAnimator.PhaseInOut(true);
	}
	void PhasePillarOut() {
		pillarAnimator.PhaseInOut(false);
	}
}
