using System.Collections;
using System.Collections.Generic;
using Interactables;
using UnityEngine;
using MagicTriggerMechanics;

namespace LevelSpecific.EmptyRoom {
	public class PillarSpawnPanel : Panel {
		public GameObject pillarBeforeActive;
		public DimensionPillar pillar;
		public MagicTrigger pillarActiveTrigger;

		protected override void Start() {
			base.Start();

			gemButton.OnButtonPressBegin += SpawnPillar;
		}

		void SpawnPillar(Button b) {
			pillar.gameObject.SetActive(true);
			pillarBeforeActive.SetActive(false);
			pillarActiveTrigger.gameObject.SetActive(true);
		}
	}
}