using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using MagicTriggerMechanics;

namespace LevelSpecific.Transition2_3 {
	public class FogToggle : MonoBehaviour {
		public bool disableFog = true;

		public GlobalFog fog;
		MagicTrigger trigger;

		void Awake() {
			trigger = GetComponent<MagicTrigger>();
		}

		void Start() {
			fog = Camera.main.GetComponent<GlobalFog>();

			trigger.OnMagicTriggerStayOneTime += ToggleForward;
			trigger.OnNegativeMagicTriggerStayOneTime += ToggleBackward;
		}

		void ToggleForward() {
			fog.enabled = !disableFog;
		}
		void ToggleBackward() {
			fog.enabled = disableFog;
		}
	}
}