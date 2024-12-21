using MagicTriggerMechanics;
using NaughtyAttributes;
using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using MagicTriggerMechanics.TriggerActions;
using MagicTriggerMechanics.TriggerConditions;
using UnityEngine;

namespace PictureTeleportMechanics {
	[RequireComponent(typeof(UniqueId))]
    public class BigFrame : SaveableObject<BigFrame, BigFrame.BigFrameSave> {

		public Renderer frameRenderer;
		public Collider frameCollider;

		public Renderer[] otherRenderers;
		public Collider[] otherColliders;

		[ReadOnly]
        public GlobalMagicTrigger disableFrameTrigger;

		void Initialize() {
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
	        Initialize();
	        if (frameRenderer == null) {
		        frameRenderer = GetComponent<Renderer>();
	        }
	        if (frameCollider == null) {
		        frameCollider = GetComponent<Collider>();
	        }
		}

        public void TurnOnFrame() {
			frameRenderer.enabled = true;
			frameCollider.enabled = true;
			if (otherRenderers != null) {
				foreach (Renderer otherRenderer in otherRenderers) {
					otherRenderer.enabled = true;
				}
			}

			if (otherColliders != null) {
				foreach (Collider otherCollider in otherColliders) {
					otherCollider.enabled = true;
				}
			}
			
			disableFrameTrigger.enabled = true;
		}

		public void TurnOffFrame() {
			frameRenderer.enabled = false;
			frameCollider.enabled = false;
			
			if (otherRenderers != null) {
				foreach (Renderer otherRenderer in otherRenderers) {
					otherRenderer.enabled = false;
				}
			}

			if (otherColliders != null) {
				foreach (Collider otherCollider in otherColliders) {
					otherCollider.enabled = false;
				}
			}
		}

		protected override void OnEnable() {
			base.OnEnable();
			PictureTeleport.bigFrames[PictureTeleport.BigFrameKey(gameObject.scene.name, gameObject.name)] = this;
			TurnOffFrame();
		}

		void OnDisable() {
			PictureTeleport.bigFrames.Remove(PictureTeleport.BigFrameKey(gameObject.scene.name, gameObject.name));
		}

		GlobalMagicTrigger AddGlobalEnableTrigger() {
            GlobalMagicTrigger trigger = gameObject.AddComponent<GlobalMagicTrigger>();

			// Add condition
			TriggerCondition condition = new RendererNotVisibleCondition() {
				targetRenderer = frameRenderer == null ? GetComponent<Renderer>() : frameRenderer
			};
			trigger.triggerConditionsNew.Add(condition);

			// Add action
			TriggerAction disableSelfScriptAction = new DisableSelfScriptAction() {
				actionTiming = ActionTiming.OnceWhileOnStay
			};
			trigger.actionsToTriggerNew.Add(disableSelfScriptAction);

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