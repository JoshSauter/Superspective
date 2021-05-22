using MagicTriggerMechanics;
using NaughtyAttributes;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PictureTeleportMechanics {
	[RequireComponent(typeof(UniqueId))]
    public class BigFrame : SaveableObject<BigFrame, BigFrame.BigFrameSave> {

		public Renderer frameRenderer;
		public Collider frameCollider;

		[ReadOnly]
        public GlobalMagicTrigger disableFrameTrigger;

        void OnValidate() {
			if (disableFrameTrigger == null) {
				disableFrameTrigger = GetComponent<GlobalMagicTrigger>();
			}
			if (disableFrameTrigger == null) {
				disableFrameTrigger = AddGlobalEnableTrigger();
				disableFrameTrigger.enabled = true;
			}

			disableFrameTrigger.OnMagicTriggerStayOneTime += TurnOffFrame;
		}

        protected override void Awake() {
	        base.Awake();
			frameRenderer = GetComponent<Renderer>();
			frameCollider = GetComponent<Collider>();
		}

		public void TurnOnFrame() {
			frameRenderer.enabled = true;
			frameCollider.enabled = true;
			disableFrameTrigger.enabled = true;
		}

		public void TurnOffFrame() {
			frameRenderer.enabled = false;
			frameCollider.enabled = false;
		}

		void OnEnable() {
			PictureTeleport.bigFrames[PictureTeleport.BigFrameKey(gameObject.scene.name, gameObject.name)] = this;
			TurnOffFrame();
		}

		void OnDisable() {
			PictureTeleport.bigFrames.Remove(PictureTeleport.BigFrameKey(gameObject.scene.name, gameObject.name));
		}

		GlobalMagicTrigger AddGlobalEnableTrigger() {
            GlobalMagicTrigger trigger = gameObject.AddComponent<GlobalMagicTrigger>();

			// Add condition
			TriggerCondition condition = new TriggerCondition {
				triggerCondition = TriggerConditionType.RendererNotVisible,
				targetRenderer = GetComponent<Renderer>()
			};
			trigger.triggerConditions.Add(condition);

			// Add action
			TriggerAction disableSelfScriptAciton = new TriggerAction {
				actionTiming = ActionTiming.OnceWhileOnStay,
				action = TriggerActionType.DisableSelfScript
			};
			trigger.actionsToTrigger.Add(disableSelfScriptAciton);

			return trigger;
		}

		#region Saving

		[Serializable]
		public class BigFrameSave : SerializableSaveObject<BigFrame> {
			bool frameEnabled;
			bool disableFrameTriggerEnabled;

			public BigFrameSave(BigFrame bigFrame) : base(bigFrame) {
				this.frameEnabled = bigFrame.frameRenderer.enabled;
				this.disableFrameTriggerEnabled = bigFrame.disableFrameTrigger.enabled;
			}

			public override void LoadSave(BigFrame bigFrame) {
				bigFrame.frameRenderer.enabled = this.frameEnabled;
				bigFrame.frameCollider.enabled = this.frameEnabled;
				bigFrame.disableFrameTrigger.enabled = this.disableFrameTriggerEnabled;
			}
		}
		#endregion
	}
}