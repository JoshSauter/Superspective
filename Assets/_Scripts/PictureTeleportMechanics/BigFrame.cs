using MagicTriggerMechanics;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PictureTeleportMechanics {
    public class BigFrame : MonoBehaviour {
		public Renderer frameRenderer;
		public Collider frameCollider;

		[ReadOnly]
        public GlobalMagicTrigger disableFrameTrigger;

		private void OnValidate() {
			if (disableFrameTrigger == null) {
				disableFrameTrigger = GetComponent<GlobalMagicTrigger>();
			}
			if (disableFrameTrigger == null) {
				disableFrameTrigger = AddGlobalEnableTrigger();
				disableFrameTrigger.enabled = true;
			}
		}

		void Awake() {
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

		private void OnEnable() {
			PictureTeleport.bigFrames[PictureTeleport.BigFrameKey(gameObject.scene.name, gameObject.name)] = this;
			TurnOffFrame();
		}

		private void OnDisable() {
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

			trigger.OnMagicTriggerStayOneTime += (ctx) => TurnOffFrame();

			return trigger;
		}
    }
}