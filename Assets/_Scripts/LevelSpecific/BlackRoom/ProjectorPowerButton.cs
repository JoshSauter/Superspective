using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	[RequireComponent(typeof(Button))]
	public class ProjectorPowerButton : SaveableObject<ProjectorPowerButton, ProjectorPowerButton.ProjectorPowerButtonSave> {
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
			b.OnButtonDepressBegin += ctx => TurnOffPowerTrail();
			powerTrail.OnPowerFinish += TurnOnProjector;
			powerTrail.OnDepowerBegin += TurnOffProjector;

			projectorTurnedOn = powerTrail.powerIsOn && powerTrail.maxDistance - powerTrail.distance < 0.01f;

			if (projectorTurnedOn) {
				TurnOnProjector();
			}
		}

		void TurnOnPowerTrail() {
			powerTrail.powerIsOn = true;
		}

		void TurnOffPowerTrail() {
			powerTrail.powerIsOn = false;
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

		#region Saving
		public override bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }

		public override string ID => $"{gameObject.name}";

		[Serializable]
		public class ProjectorPowerButtonSave : SerializableSaveObject<ProjectorPowerButton> {
			bool projectorTurnedOn;

			public ProjectorPowerButtonSave(ProjectorPowerButton projectorPowerButton) {
				this.projectorTurnedOn = projectorPowerButton.projectorTurnedOn;
			}

			public override void LoadSave(ProjectorPowerButton projectorPowerButton) {
				if (this.projectorTurnedOn) {
					projectorPowerButton.TurnOnProjector();
				}
			}
		}
		#endregion
	}
}