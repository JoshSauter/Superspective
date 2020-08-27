using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using MagicTriggerMechanics;

public class FogToggle : MonoBehaviour {
	public bool disableFog = true;
	
	public GlobalFog fog;
	private MagicTrigger trigger;

	private void Awake() {
		trigger = GetComponent<MagicTrigger>();
	}

	private void Start() {
		fog = Camera.main.GetComponent<GlobalFog>();

		trigger.OnMagicTriggerStayOneTime += (ctx) => ToggleForward();
		trigger.OnNegativeMagicTriggerStayOneTime += (ctx) => ToggleBackward();
	}

	void ToggleForward() {
		fog.enabled = !disableFog;
	}
	void ToggleBackward() {
		fog.enabled = disableFog;
	}
}
