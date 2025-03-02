﻿using LevelManagement;
using MagicTriggerMechanics.TriggerActions;
using UnityEngine;

namespace MagicTriggerMechanics {
	[RequireComponent(typeof(UniqueId))]
    public class GlobalMagicTrigger : MagicTrigger {
		public bool sceneMustBeActive = true;
		Levels thisScene = Levels.ManagerScene;

		bool IsActive() {
			if (!GameManager.instance.gameHasLoaded || GameManager.instance.IsCurrentlyLoading) return false;
			
			bool isActive = enabled;
			if (sceneMustBeActive) {
				if (thisScene == Levels.ManagerScene) {
					thisScene = gameObject.scene.name.ToLevel();
				}
				isActive = isActive && thisScene == LevelManager.instance.ActiveScene;
			}
			return isActive;
		}

		protected override void UpdateLayers() { /* No need to change layers for global triggers */ }
		protected override void AddBetterTriggers() { /* No colliders to modify for global triggers */ }

        void Update() {
	        if (!IsActive()) {
		        debug.Log("IsActive is false");
		        return;
	        }
			if (DEBUG) {
				PrintDebugInfo();
			}

			bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform));
            bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform));
			// Magic Events triggered
			if (allConditionsSatisfied) {
				debug.Log($"Triggering MagicTrigger for {gameObject.name}!");

				ExecuteActionsForTiming(ActionTiming.EveryFrameOnStay);
				OnMagicTriggerStay?.Invoke();
				if (!hasTriggeredOnStay) {
					hasTriggeredOnStay = true;
					hasNegativeTriggeredOnStay = false;

					ExecuteActionsForTiming(ActionTiming.OnceWhileOnStay);
					OnMagicTriggerStayOneTime?.Invoke();
				}
			}
			// Negative Magic Events triggered (negative triggers cannot turn self off)
			else if (allConditionsNegativelySatisfied) {
				debug.Log("Triggering NegativeMagicTrigger!");
				ExecuteNegativeActionsForTiming(ActionTiming.EveryFrameOnStay);
				OnNegativeMagicTriggerStay?.Invoke();

				if (!hasNegativeTriggeredOnStay) {
					hasNegativeTriggeredOnStay = true;
					hasTriggeredOnStay = false;

					ExecuteNegativeActionsForTiming(ActionTiming.OnceWhileOnStay);
					OnNegativeMagicTriggerStayOneTime?.Invoke();
				}
			}
		}

        protected override void OnValidate() {
	        base.OnValidate();
			foreach (var action in actionsToTrigger) {
				if (action != null && (action.actionTiming.HasFlag(ActionTiming.OnEnter) || action.actionTiming.HasFlag(ActionTiming.OnExit))) {
					Debug.LogError("OnEnter and OnExit trigger action timings will not work for GlobalMagicTriggers");
				}
			}
		}
	}
}
