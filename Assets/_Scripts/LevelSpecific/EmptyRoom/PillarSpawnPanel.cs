using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicTriggerMechanics;

public class PillarSpawnPanel : Panel {
	public GameObject pillarBeforeActive;
	public DimensionPillar pillar;
	public MagicTriggerNew pillarActiveTrigger;

	override protected void Start() {
		base.Start();

		gemButton.OnButtonPressBegin += SpawnPillar;
	}

	void SpawnPillar(Button b) {
		pillar.gameObject.SetActive(true);
		pillarBeforeActive.SetActive(false);
		pillarActiveTrigger.gameObject.SetActive(true);
	}
}
