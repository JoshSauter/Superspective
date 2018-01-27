using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondRoomColumnStateManager : MonoBehaviour {
	public GameObject column1Solid;
	public GameObject column2Solid;
	public GameObject column1;
	public GameObject column2;

	public enum ColumnState {
		Inactive,
		Obscured,
		Active
	}

	private ColumnState _column1State = ColumnState.Obscured;
	private ColumnState _column2State = ColumnState.Inactive;
	public ColumnState column1State {
		get { return _column1State; }
		set {
			if (_column1State == value) return;

			SetColumnState(value, ref column1, ref column1Solid);

			_column1State = value;
		}
	}
	public ColumnState column2State {
		get { return _column2State; }
		set {
			if (_column2State == value) return;

			SetColumnState(value, ref column2, ref column2Solid);

			_column2State = value;
		}
	}

	private void SetColumnState(ColumnState newColumnState, ref GameObject column, ref GameObject columnSolid) {
		switch (newColumnState) {
			case ColumnState.Inactive:
				columnSolid.SetActive(false);
				column.SetActive(false);
				break;
			case ColumnState.Obscured:
				columnSolid.SetActive(true);
				column.SetActive(false);
				break;
			case ColumnState.Active:
				columnSolid.SetActive(true);
				columnSolid.SetActive(false);
				break;
		}
	}
}
