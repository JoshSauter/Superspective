using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarSpawnPanel : Panel {
	public GameObject pillarBeforeActive;
	public DimensionPillar pillar;

	override protected void Start() {
		base.Start();

		gemButton.OnButtonPressBegin += SpawnPillar;
	}

	void SpawnPillar(Button b) {
		pillar.gameObject.SetActive(true);
		pillarBeforeActive.SetActive(false);
		DimensionPillar.activePillar = pillar;
	}
}
