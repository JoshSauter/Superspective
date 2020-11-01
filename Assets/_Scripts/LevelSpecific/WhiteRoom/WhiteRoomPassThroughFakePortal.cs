using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using MagicTriggerMechanics;
using Saving;
using System;

namespace LevelSpecific.WhiteRoom {
	public class WhiteRoomPassThroughFakePortal : MonoBehaviour, SaveableObject {
		public MagicTrigger restoreFakePortalTrigger;

		public DimensionObjectBase ceilingDropDown;
		public PillarDimensionObject ceilingDropDownAfterPassingThrough;
		MagicTrigger trigger;

		private void Awake() {
			trigger = GetComponent<MagicTrigger>();
		}

		void Start() {
			trigger.OnMagicTriggerStayOneTime += OnMagicTriggerStayOneTime;
			trigger.OnNegativeMagicTriggerStayOneTime += OnNegativeMagicTriggerStayOneTime;
		}

		private void OnMagicTriggerStayOneTime(GameObject other) {
			if (other.TaggedAsPlayer()) {
				ceilingDropDownAfterPassingThrough.Start();
				ceilingDropDownAfterPassingThrough.OverrideStartingMaterials(ceilingDropDown.startingMaterials);
				ceilingDropDownAfterPassingThrough.SwitchVisibilityState(ceilingDropDownAfterPassingThrough.visibilityState, true);
			}
		}

		private void OnNegativeMagicTriggerStayOneTime(GameObject other) {
			if (other.TaggedAsPlayer()) {
				restoreFakePortalTrigger.gameObject.SetActive(true);
				gameObject.SetActive(false);
			}
		}

		#region Saving
		public bool SkipSave { get; set; }

		public string ID => "WhiteRoomPassThroughFakePortal";

		[Serializable]
		class WhiteRoomPassThroughFakePortalSave {
			bool restoreFakePortalTriggerActive;
			bool thisActive;

			public WhiteRoomPassThroughFakePortalSave(WhiteRoomPassThroughFakePortal script) {
				this.restoreFakePortalTriggerActive = script.restoreFakePortalTrigger.gameObject.activeSelf;
				this.thisActive = script.gameObject.activeSelf;
			}

			public void LoadSave(WhiteRoomPassThroughFakePortal script) {
				script.restoreFakePortalTrigger.gameObject.SetActive(this.restoreFakePortalTriggerActive);
				script.gameObject.SetActive(this.thisActive);
			}
		}

		public object GetSaveObject() {
			return new WhiteRoomPassThroughFakePortalSave(this);
		}

		public void LoadFromSavedObject(object savedObject) {
			WhiteRoomPassThroughFakePortalSave save = savedObject as WhiteRoomPassThroughFakePortalSave;

			save.LoadSave(this);
		}
		#endregion
	}
}