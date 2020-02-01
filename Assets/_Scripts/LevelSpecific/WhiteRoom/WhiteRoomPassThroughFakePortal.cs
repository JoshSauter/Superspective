using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class WhiteRoomPassThroughFakePortal : MonoBehaviour {
	public DimensionObjectBase ceilingDropDown;
	public PillarDimensionObject ceilingDropDownAfterPassingThrough;
	MagicTrigger trigger;

    void Start() {
		trigger = GetComponent<MagicTrigger>();
		trigger.OnMagicTriggerEnter += OnMagicTriggerEnter;
    }

	private void OnMagicTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer()) {
			ceilingDropDown.enabled = false;
			ceilingDropDownAfterPassingThrough.enabled = true;
			ceilingDropDownAfterPassingThrough.Init();
			ceilingDropDownAfterPassingThrough.OverrideStartingMaterials(ceilingDropDown.startingMaterials);
			ceilingDropDownAfterPassingThrough.SwitchVisibilityState(ceilingDropDownAfterPassingThrough.visibilityState, true);
		}
	}
}
