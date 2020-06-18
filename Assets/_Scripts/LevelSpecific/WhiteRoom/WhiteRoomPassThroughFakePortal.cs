using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using MagicTriggerMechanics;

public class WhiteRoomPassThroughFakePortal : MonoBehaviour {
	public MagicTrigger restoreFakePortalTrigger;

	public DimensionObjectBase ceilingDropDown;
	public PillarDimensionObject ceilingDropDownAfterPassingThrough;
	MagicTrigger trigger;

    void Start() {
		trigger = GetComponent<MagicTrigger>();
		trigger.OnMagicTriggerStayOneTime += OnMagicTriggerStayOneTime;
		trigger.OnNegativeMagicTriggerStayOneTime += OnNegativeMagicTriggerStayOneTime;
    }

	private void OnMagicTriggerStayOneTime(Collider other) {
		if (other.TaggedAsPlayer()) {
			ceilingDropDownAfterPassingThrough.Start();
			ceilingDropDownAfterPassingThrough.OverrideStartingMaterials(ceilingDropDown.startingMaterials);
			ceilingDropDownAfterPassingThrough.SwitchVisibilityState(ceilingDropDownAfterPassingThrough.visibilityState, true);
		}
	}

	private void OnNegativeMagicTriggerStayOneTime(Collider other) {
		if (other.TaggedAsPlayer()) {
			restoreFakePortalTrigger.gameObject.SetActive(true);
			gameObject.SetActive(false);
		}
	}
}
