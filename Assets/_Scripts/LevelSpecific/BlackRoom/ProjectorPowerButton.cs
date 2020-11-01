using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTrailMechanics;
using Saving;
using System;

namespace LevelSpecific.BlackRoom {
	[RequireComponent(typeof(Button))]
	public class ProjectorPowerButton : MonoBehaviour, SaveableObject {
		public PowerTrail powerTrail;
		public bool projectorTurnedOn = false;
		public LightProjector projector;
		Button b;

		IEnumerator Start() {
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
		public bool SkipSave { get { return !gameObject.activeInHierarchy; } set { } }

		public string ID => $"{gameObject.name}";

		[Serializable]
		class ProjectorPowerButtonSave {
			bool projectorTurnedOn;

			public ProjectorPowerButtonSave(ProjectorPowerButton projectorPowerButton) {
				this.projectorTurnedOn = projectorPowerButton.projectorTurnedOn;
			}

			public void LoadSave(ProjectorPowerButton projectorPowerButton) {
				if (this.projectorTurnedOn) {
					projectorPowerButton.TurnOnProjector();
				}
			}
		}

		public object GetSaveObject() {
			return new ProjectorPowerButtonSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			ProjectorPowerButtonSave save = savedObject as ProjectorPowerButtonSave;

			save.LoadSave(this);
		}
		#endregion
	}
}