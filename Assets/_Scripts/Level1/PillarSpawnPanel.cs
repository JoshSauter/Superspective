using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarSpawnPanel : Panel {
	public GameObject pillar;

	override protected void Start() {
		base.Start();

		gemButton.OnButtonPressBegin += SpawnPillar;
	}

	void SpawnPillar(Button b) {
		pillar.SetActive(true);
	}
}
