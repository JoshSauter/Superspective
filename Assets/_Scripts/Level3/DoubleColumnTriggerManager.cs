using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleColumnTriggerManager : MonoBehaviour {
	DoubleColumnStateManager columnStateManager;

	public MagicTrigger column1StartingPartiallyOnTrigger;

	public MagicTrigger column1AlwaysOnTrigger;
	public MagicTrigger column2AlwaysOnTrigger;

	public MagicTrigger column1PartiallyOnTrigger;
	public MagicTrigger column2PartiallyOnTrigger;

	public MagicTrigger column1AlwaysOffTrigger;

	// Use this for initialization
	void Start () {
		columnStateManager = GetComponent<DoubleColumnStateManager>();

		// Starting Partially On Trigger
		column1StartingPartiallyOnTrigger.OnMagicTriggerStayOneTime += Column1PartiallyOn;
		column1StartingPartiallyOnTrigger.OnNegativeMagicTriggerStayOneTime += Column1AlwaysOff;

		// Always On Triggers
		column1AlwaysOnTrigger.OnMagicTriggerStayOneTime += Column1AlwaysOn;
		column1AlwaysOnTrigger.OnNegativeMagicTriggerStayOneTime += Column1PartiallyOn;

		column2AlwaysOnTrigger.OnMagicTriggerStayOneTime += Column2AlwaysOn;
		column2AlwaysOnTrigger.OnNegativeMagicTriggerStayOneTime += Column2PartiallyOn;

		// Partially On Triggers
		column1PartiallyOnTrigger.OnMagicTriggerStayOneTime += Column1PartiallyOn;
		column1PartiallyOnTrigger.OnNegativeMagicTriggerStayOneTime += Column1AlwaysOn;

		column2PartiallyOnTrigger.OnMagicTriggerStayOneTime += Column2PartiallyOn;
		column2PartiallyOnTrigger.OnNegativeMagicTriggerStayOneTime += Column2AlwaysOff;

		// Always Off Triggers
		column1AlwaysOffTrigger.OnMagicTriggerStayOneTime += Column1AlwaysOff;
		column1AlwaysOffTrigger.OnNegativeMagicTriggerStayOneTime += Column1PartiallyOn;
	}

	// Column 1 listener functions
	void Column1AlwaysOn(Collider c) {
		columnStateManager.column1State = ColumnState.Active;
	}
	void Column1AlwaysOff(Collider c) {
		columnStateManager.column1State = ColumnState.Inactive;
	}
	void Column1PartiallyOn(Collider c) {
		columnStateManager.column1State = ColumnState.Obscured;
	}
	// Column 2 listener functions
	void Column2AlwaysOn(Collider c) {
		columnStateManager.column2State = ColumnState.Active;
	}
	void Column2AlwaysOff(Collider c) {
		columnStateManager.column2State = ColumnState.Inactive;
	}
	void Column2PartiallyOn(Collider c) {
		columnStateManager.column2State = ColumnState.Obscured;
	}
}
