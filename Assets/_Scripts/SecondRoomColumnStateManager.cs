using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ColumnState {
	Inactive,
	Obscured,
	Active
}

public class SecondRoomColumnStateManager : MonoBehaviour {
	[SerializeField]
	private ColumnState _column1State = ColumnState.Inactive;
	[SerializeField]
	private ColumnState _column2State = ColumnState.Inactive;

	public GameObject column1Solid;
	public GameObject column2Solid;
	public GameObject column1;
	public GameObject column2;

	public GameObject[] objectsDuringNothing;
	public GameObject[] objectsDuringPillar1;
	public GameObject[] objectsDuringPillar1Solid;
	public GameObject[] objectsDuringPillar1SolidPillar2;
	public GameObject[] objectsDuringPillar1SolidPillar2Solid;
	public GameObject[] objectsDuringPillar1Pillar2Solid;
	public GameObject[] objectsDuringPillar2Solid;

	public ColumnState column1State {
		get { return _column1State; }
		set {
			GameObject[] objectsToDisable = ObjectsFromColumnState(_column1State, _column2State);
			GameObject[] objectsToEnable = ObjectsFromColumnState(value, _column2State);

			DisableEnableObjects(objectsToDisable, objectsToEnable);

			_column1State = value;
			SetColumnState(_column1State, ref column1, ref column1Solid);

		}
	}
	public ColumnState column2State {
		get { return _column2State; }
		set {
			GameObject[] objectsToDisable = ObjectsFromColumnState(_column1State, _column2State);
			GameObject[] objectsToEnable = ObjectsFromColumnState(_column1State, value);

			DisableEnableObjects(objectsToDisable, objectsToEnable);

			_column2State = value;
			SetColumnState(_column2State, ref column2, ref column2Solid);

		}
	}

	private void SetColumnState(ColumnState newColumnState, ref GameObject column, ref GameObject columnSolid) {
		switch (newColumnState) {
			case ColumnState.Inactive:
				columnSolid.SetActive(false);
				column.SetActive(false);
				break;
			case ColumnState.Obscured:
				columnSolid.SetActive(false);
				column.SetActive(true);
				break;
			case ColumnState.Active:
				columnSolid.SetActive(true);
				column.SetActive(false);
				break;
		}
	}

	GameObject[] ObjectsFromColumnState(ColumnState p1, ColumnState p2) {
		if (p1 == ColumnState.Inactive && p2 == ColumnState.Inactive) return objectsDuringNothing;
		if (p1 == ColumnState.Obscured && p2 == ColumnState.Inactive) return objectsDuringPillar1;
		if (p1 == ColumnState.Active && p2 == ColumnState.Inactive) return objectsDuringPillar1Solid;
		if (p1 == ColumnState.Active && p2 == ColumnState.Obscured) return objectsDuringPillar1SolidPillar2;
		if (p1 == ColumnState.Active && p2 == ColumnState.Active) return objectsDuringPillar1SolidPillar2Solid;
		if (p1 == ColumnState.Obscured && p2 == ColumnState.Active) return objectsDuringPillar1Pillar2Solid;
		if (p1 == ColumnState.Inactive && p2 == ColumnState.Active) return objectsDuringPillar2Solid;
		else throw new System.Exception("Unreachable state: column1State: " + column1State + ", column2State: " + column2State);
	}

	void DisableEnableObjects(GameObject[] disabled, GameObject[] enabled) {
		foreach (var GO in disabled) {
			GO.SetActive(false);
		}
		foreach (var GO in enabled) {
			GO.SetActive(true);
		}
	}
}
