using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicTriggerMechanics {
    public class GlobalMagicTrigger : MagicTrigger {
		public bool sceneMustBeActive = true;
		private Level thisScene = Level.managerScene;

		bool IsActive() {
			bool isActive = enabled;
			if (sceneMustBeActive) {
				if (thisScene == Level.managerScene) {
					thisScene = LevelManager.instance.GetLevel(gameObject.scene.name);
				}
				isActive = isActive && thisScene == LevelManager.instance.activeScene;
			}
			return isActive;
		}

        void Update() {
            if (!IsActive()) return;
			GameObject player = Player.instance.gameObject;
			if (DEBUG) {
				PrintDebugInfo(player);
			}

			bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform, player));
            bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform, player));
			// Magic Events triggered
			if (allConditionsSatisfied) {
				debug.Log($"Triggering MagicTrigger for {gameObject.name}!");

				ExecuteActionsForTiming(ActionTiming.EveryFrameOnStay);
				OnMagicTriggerStay?.Invoke(player);
				if (!hasTriggeredOnStay) {
					hasTriggeredOnStay = true;
					hasNegativeTriggeredOnStay = false;

					ExecuteActionsForTiming(ActionTiming.OnceWhileOnStay);
					OnMagicTriggerStayOneTime?.Invoke(player);
				}
			}
			// Negative Magic Events triggered (negative triggers cannot turn self off)
			else if (allConditionsNegativelySatisfied) {
				debug.Log("Triggering NegativeMagicTrigger!");
				ExecuteNegativeActionsForTiming(ActionTiming.EveryFrameOnStay);
				OnNegativeMagicTriggerStay?.Invoke(player);

				if (!hasNegativeTriggeredOnStay) {
					hasNegativeTriggeredOnStay = true;
					hasTriggeredOnStay = false;

					ExecuteNegativeActionsForTiming(ActionTiming.OnceWhileOnStay);
					OnNegativeMagicTriggerStayOneTime?.Invoke(player);
				}
			}
		}

		private void OnValidate() {
			foreach (var action in actionsToTrigger) {
				if (action.actionTiming.HasFlag(ActionTiming.OnEnter) || action.actionTiming.HasFlag(ActionTiming.OnExit)) {
					Debug.LogError("OnEnter and OnExit trigger action timings will not work for GlobalMagicTriggers");
				}
			}
		}
	}
}