using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	public class MagicTrigger : MonoBehaviour {
		public bool DEBUG;
		public DebugLogger debug;

		[HorizontalLine(color: EColor.Yellow)]
		public List<TriggerCondition> triggerConditions = new List<TriggerCondition>();
		[HorizontalLine(color: EColor.Green)]
		public List<TriggerAction> actionsToTrigger = new List<TriggerAction>();

		#region events
		public delegate void MagicAction(GameObject player);
		// These events are fired when the trigger condition specified is met
		public MagicAction OnMagicTriggerStay;
		public MagicAction OnMagicTriggerEnter;
		public MagicAction OnMagicTriggerStayOneTime;
		// These events are fired whenever the opposite of the trigger condition is met (does not necessarily form a complete set with the events above, something may not be fired)
		public MagicAction OnNegativeMagicTriggerStay;
		public MagicAction OnNegativeMagicTriggerEnter;
		public MagicAction OnNegativeMagicTriggerStayOneTime;

		// OnMagicTriggerExit always fire on OnTriggerExit regardless of trigger conditions
		public event MagicAction OnMagicTriggerExit;
		#endregion

		protected bool hasTriggeredOnStay = false;
		protected bool hasNegativeTriggeredOnStay = false;

		protected virtual void Awake() {
			debug = new DebugLogger(this, () => DEBUG);
		}

		private void OnDisable() {
			hasTriggeredOnStay = false;
			hasNegativeTriggeredOnStay = false;
		}

		protected void OnTriggerStay(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				GameObject player = other.gameObject;
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
		}

		protected void OnTriggerEnter(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				GameObject player = other.gameObject;
				if (DEBUG) {
					PrintDebugInfo(player);
				}

				bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform, player));
				bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform, player));
				if (allConditionsSatisfied) {
					ExecuteActionsForTiming(ActionTiming.OnEnter);
					OnMagicTriggerEnter?.Invoke(player);
				}
				else if (allConditionsNegativelySatisfied) {
					ExecuteNegativeActionsForTiming(ActionTiming.OnEnter);
					OnNegativeMagicTriggerEnter?.Invoke(player);
				}
			}
		}

		protected void OnTriggerExit(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				GameObject player = other.gameObject;
				ExecuteActionsForTiming(ActionTiming.OnExit);
				ExecuteNegativeActionsForTiming(ActionTiming.OnExit);
				OnMagicTriggerExit?.Invoke(player);

				if (hasTriggeredOnStay) {
					hasTriggeredOnStay = false;
				}
				if (hasNegativeTriggeredOnStay) {
					hasNegativeTriggeredOnStay = false;
				}
			}
		}

		protected void ExecuteActionsForTiming(ActionTiming timing) {
			foreach (var action in actionsToTrigger.Where(tc => tc.actionTiming.HasFlag(timing))) {
				action.Execute(this);
			}
		}
		protected void ExecuteNegativeActionsForTiming(ActionTiming timing) {
			foreach (var action in actionsToTrigger.Where(tc => tc.actionTiming.HasFlag(timing))) {
				action.NegativeExecute();
			}
		}

		protected void PrintDebugInfo(GameObject player) {
			string debugString = $"{gameObject.name}:\n";
			foreach (var condition in triggerConditions) {
				float triggerValue = condition.Evaluate(transform, player);
				debugString += $"Type: {condition.triggerCondition}\nAmount facing: {triggerValue}\nThreshold: {condition.triggerThreshold}\nPass ?: {(triggerValue > condition.triggerThreshold)}\n";
				debugString += "--------\n";
			}
			debug.Log(debugString);
		}
	}
}