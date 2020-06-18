using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using MagicTriggerMechanics;

public class FogToggle : MonoBehaviour {
	public bool disableFog = true;
	
	public GlobalFog fog;
	private MagicTriggerNew trigger;

	private void Awake() {
		trigger = GetComponent<MagicTriggerNew>();
	}

	private void Start() {
		fog = Camera.main.GetComponent<GlobalFog>();

		trigger.OnMagicTriggerStayOneTime += ToggleForward;
		trigger.OnNegativeMagicTriggerStayOneTime += ToggleBackward;
	}

	void ToggleForward(Collider c) {
		fog.enabled = !disableFog;
	}
	void ToggleBackward(Collider c) {
		fog.enabled = disableFog;
	}
}
