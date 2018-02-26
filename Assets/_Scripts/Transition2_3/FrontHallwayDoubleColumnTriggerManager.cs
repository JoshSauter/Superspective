using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontHallwayDoubleColumnTriggerManager : MonoBehaviour {
	DoubleColumnStateManager columnStateManager;

	public MagicTrigger column2PartiallyOnTrigger;
	public MagicTrigger column2OffTrigger;

	// Use this for initialization
	void Start () {
		columnStateManager = GetComponent<DoubleColumnStateManager>();

		column2PartiallyOnTrigger.OnMagicTriggerStayOneTime += Column2PartiallyOn;
		column2PartiallyOnTrigger.OnNegativeMagicTriggerStayOneTime += Column2AlwaysOn;

		column2OffTrigger.OnMagicTriggerStayOneTime += Column2AlwaysOff;
		column2OffTrigger.OnNegativeMagicTriggerStayOneTime += Column2PartiallyOn;
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
