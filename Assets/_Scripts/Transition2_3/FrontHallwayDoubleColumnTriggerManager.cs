using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontHallwayDoubleColumnTriggerManager : MonoBehaviour {
	DoubleColumnStateManager columnStateManager;

	public Panel column1Panel;
	public Panel column2Panel;

	// Use this for initialization
	void Start () {
		columnStateManager = GetComponent<DoubleColumnStateManager>();

		column1Panel.OnPanelActivateFinish += Column1AlwaysOn;
		column1Panel.OnPanelDeactivateFinish += Column1AlwaysOff;

		column2Panel.OnPanelActivateFinish += Column2AlwaysOn;
		column2Panel.OnPanelDeactivateFinish += Column2AlwaysOff;
	}

	// Column 2 listener functions
	void Column2AlwaysOn() {
		columnStateManager.column2State = ColumnState.Active;
	}
	void Column2AlwaysOff() {
		columnStateManager.column2State = ColumnState.Inactive;
	}
	void Column1AlwaysOn() {
		columnStateManager.column1State = ColumnState.Active;
	}
	void Column1AlwaysOff() {
		columnStateManager.column1State = ColumnState.Inactive;
	}
}
