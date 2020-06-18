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
		public delegate void MagicAction(Collider other);
		// These events are fired when the trigger condition specified is met
		public event MagicAction OnMagicTriggerStay;
		public event MagicAction OnMagicTriggerEnter;
		public event MagicAction OnMagicTriggerStayOneTime;
		// These events are fired whenever the opposite of the trigger condition is met (does not necessarily form a complete set with the events above, something may not be fired)
		public event MagicAction OnNegativeMagicTriggerStay;
		public event MagicAction OnNegativeMagicTriggerEnter;
		public event MagicAction OnNegativeMagicTriggerStayOneTime;

		// OnMagicTriggerExit always fire on OnTriggerExit regardless of trigger conditions
		public event MagicAction OnMagicTriggerExit;
		#endregion

		private bool hasTriggeredOnStay = false;
		private bool hasNegativeTriggeredOnStay = false;

		private void Awake() {
			debug = new DebugLogger(this, () => DEBUG);
		}

		private void OnTriggerStay(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				if (DEBUG) {
					PrintDebugInfo(other);
				}

				bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform, other));
				bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform, other));
				// Magic Events triggered
				if (allConditionsSatisfied) {
					debug.Log($"Triggering MagicTrigger for {gameObject.name}!");

					ExecuteActionsForTiming(ActionTiming.EveryFrameOnStay);
					OnMagicTriggerStay?.Invoke(other);
					if (!hasTriggeredOnStay) {
						hasTriggeredOnStay = true;
						hasNegativeTriggeredOnStay = false;

						ExecuteActionsForTiming(ActionTiming.OnceWhileOnStay);
						OnMagicTriggerStayOneTime?.Invoke(other);
					}
				}
				// Negative Magic Events triggered (negative triggers cannot turn self off)
				else if (allConditionsNegativelySatisfied) {
					debug.Log("Triggering NegativeMagicTrigger!");
					ExecuteNegativeActionsForTiming(ActionTiming.EveryFrameOnStay);
					OnNegativeMagicTriggerStay?.Invoke(other);

					if (!hasNegativeTriggeredOnStay) {
						hasNegativeTriggeredOnStay = true;
						hasTriggeredOnStay = false;

						ExecuteNegativeActionsForTiming(ActionTiming.OnceWhileOnStay);
						OnNegativeMagicTriggerStayOneTime?.Invoke(other);
					}
				}
			}
		}

		private void OnTriggerEnter(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				if (DEBUG) {
					PrintDebugInfo(other);
				}

				bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform, other));
				bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform, other));
				if (allConditionsSatisfied) {
					ExecuteActionsForTiming(ActionTiming.OnEnter);
					OnMagicTriggerEnter?.Invoke(other);
				}
				else if (allConditionsNegativelySatisfied) {
					ExecuteNegativeActionsForTiming(ActionTiming.OnEnter);
					OnNegativeMagicTriggerEnter?.Invoke(other);
				}
			}
		}

		private void OnTriggerExit(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				ExecuteActionsForTiming(ActionTiming.OnExit);
				ExecuteNegativeActionsForTiming(ActionTiming.OnExit);
				OnMagicTriggerExit?.Invoke(other);

				if (hasTriggeredOnStay) {
					hasTriggeredOnStay = false;
				}
				if (hasNegativeTriggeredOnStay) {
					hasNegativeTriggeredOnStay = false;
				}
			}
		}

		private void ExecuteActionsForTiming(ActionTiming timing) {
			foreach (var action in actionsToTrigger.Where(tc => tc.actionTiming.HasFlag(timing))) {
				action.Execute(this);
			}
		}
		private void ExecuteNegativeActionsForTiming(ActionTiming timing) {
			foreach (var action in actionsToTrigger.Where(tc => tc.actionTiming.HasFlag(timing))) {
				action.NegativeExecute();
			}
		}

		private void PrintDebugInfo(Collider player) {
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