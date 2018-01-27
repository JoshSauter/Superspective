using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondRoomTriggerManager : MonoBehaviour {
	public enum ColumnsActive {
		Neither,
		Pillar1,
		Pillar2,
		Both
	}

	private ColumnsActive _columnsActive;
	public ColumnsActive columnsActive {
		get { return _columnsActive; }
		set {
			if (value == _columnsActive) return;

			switch (value) {
				case ColumnsActive.Neither:
					break;
				case ColumnsActive.Pillar1:
					break;
				case ColumnsActive.Pillar2:
					break;
				case ColumnsActive.Both:
					break;
			}
		}
	}
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
