using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Saving;
using System;
using Interactables;

namespace LevelSpecific.BlackRoom {
	[RequireComponent(typeof(Button))]
	public class ProjectorPowerButton : SuperspectiveObject<ProjectorPowerButton, ProjectorPowerButton.ProjectorPowerButtonSave> {
		public PowerTrail powerTrail;
		public bool projectorTurnedOn = false;
		public LightProjector projector;
		Button b;

		protected override void Start() {
			base.Start();
			StartCoroutine(Initialize());
		}

		IEnumerator Initialize() {
			yield return null;
			b = GetComponent<Button>();
			b.OnButtonPressBegin += ctx => TurnOnPowerTrail();
			b.OnButtonUnpressBegin += ctx => TurnOffPowerTrail();
			powerTrail.pwr.OnPowerFinish += TurnOnProjector;
			powerTrail.pwr.OnDepowerBegin += TurnOffProjector;

			projectorTurnedOn = powerTrail.pwr.PowerIsOn && powerTrail.maxDistance - powerTrail.distance < 0.01f;

			if (projectorTurnedOn) {
				TurnOnProjector();
			}
		}

		void TurnOnPowerTrail() {
			powerTrail.pwr.PowerIsOn = true;
		}

		void TurnOffPowerTrail() {
			powerTrail.pwr.PowerIsOn = false;
		}

		void TurnOnProjector() {
			foreach (Transform child in projector.transform) {
				child.gameObject.SetActive(true);
			}
			projectorTurnedOn = true;
		}
		void TurnOffProjector() {
			foreach (Transform child in projector.transform) {
				child.gameObject.SetActive(false);
			}
			projectorTurnedOn = false;
		}
		
		void RestoreProjector() {
			if (projectorTurnedOn) {
				TurnOnProjector();
			}
			else {
				TurnOffProjector();
			}
		}

#region Saving

		public override void LoadSave(ProjectorPowerButtonSave save) {
			RestoreProjector();
		}

		public override bool SkipSave => !gameObject.activeInHierarchy;

		public override string ID => $"{gameObject.name}";

		[Serializable]
		public class ProjectorPowerButtonSave : SaveObject<ProjectorPowerButton> {
			public ProjectorPowerButtonSave(ProjectorPowerButton projectorPowerButton) : base(projectorPowerButton) { }
		}
#endregion
	}
}