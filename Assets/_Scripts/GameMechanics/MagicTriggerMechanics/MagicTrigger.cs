using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using NaughtyAttributes;
using Saving;
using System;
using System.Collections;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	[RequireComponent(typeof(UniqueId))]
	public class MagicTrigger : SaveableObject<MagicTrigger, MagicTrigger.MagicTriggerSave>, BetterTriggers {
		[HorizontalLine(color: EColor.Yellow)]
		public List<TriggerCondition> triggerConditions = new List<TriggerCondition>();
		[HorizontalLine(color: EColor.Green)]
		public List<TriggerAction> actionsToTrigger = new List<TriggerAction>();

		#region events
		public delegate void MagicAction();
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

		public bool playerIsInTriggerZone = false;
		protected bool hasTriggeredOnStay = false;
		protected bool hasNegativeTriggeredOnStay = false;

		protected override void Awake() {
			base.Awake();
			UpdateLayers();
		}
		
		protected override void Init() {
			base.Init();
			AddBetterTriggers();
		}

		protected virtual void UpdateLayers() {
			if (gameObject.layer != LayerMask.NameToLayer("Portal")) {
				gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
			}
		}

		// Any colliders for this MagicTrigger should use BetterTriggers
		protected virtual void AddBetterTriggers() {
			foreach (var c in GetComponentsInChildren<Collider>()) {
				c.GetOrAddComponent<BetterTrigger>();
			}
		}

		public void ResetHasTriggeredOnStayState() {
			StartCoroutine(ResetHasTriggeredOnStayStateAtEndOfPhysicsFrame());
		}

		IEnumerator ResetHasTriggeredOnStayStateAtEndOfPhysicsFrame() {
			yield return new WaitForFixedUpdate();
			if (hasTriggeredOnStay || hasNegativeTriggeredOnStay) {
				debug.LogWarning("Resetting has triggered on stay state");
			}

			playerIsInTriggerZone = false;
			hasTriggeredOnStay = false;
			hasNegativeTriggeredOnStay = false;
		}

		void OnDisable() {
			playerIsInTriggerZone = false;
			hasTriggeredOnStay = false;
			hasNegativeTriggeredOnStay = false;
		}

		public void OnBetterTriggerStay(Collider other) {
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
					debug.Log($"Triggering MagicTrigger for {gameObject.name}!\nFirst trigger? {!hasTriggeredOnStay}");

					ExecuteActionsForTiming(ActionTiming.EveryFrameOnStay);
					OnMagicTriggerStay?.Invoke();
					if (!hasTriggeredOnStay) {
						hasTriggeredOnStay = true;
						hasNegativeTriggeredOnStay = false;

						ExecuteActionsForTiming(ActionTiming.OnceWhileOnStay);
						debug.Log($"Triggering MagicTriggerStayOneTime for {gameObject.name}!");
						OnMagicTriggerStayOneTime?.Invoke();
					}
				}
				// Negative Magic Events triggered (negative triggers cannot turn self off)
				else if (allConditionsNegativelySatisfied) {
					debug.Log($"Triggering Negative MagicTrigger for {gameObject.name}!");
					ExecuteNegativeActionsForTiming(ActionTiming.EveryFrameOnStay);
					OnNegativeMagicTriggerStay?.Invoke();

					if (!hasNegativeTriggeredOnStay) {
						hasNegativeTriggeredOnStay = true;
						hasTriggeredOnStay = false;

						ExecuteNegativeActionsForTiming(ActionTiming.OnceWhileOnStay);
						debug.Log($"Triggering Negative MagicTriggerStayOneTime for {gameObject.name}!");
						OnNegativeMagicTriggerStayOneTime?.Invoke();
					}
				}
			}
		}

		public void OnBetterTriggerEnter(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				playerIsInTriggerZone = true;
				GameObject player = other.gameObject;
				if (DEBUG) {
					PrintDebugInfo(player);
				}

				bool allConditionsSatisfied = triggerConditions.TrueForAll(tc => tc.IsTriggered(transform, player));
				bool allConditionsNegativelySatisfied = triggerConditions.TrueForAll(tc => tc.IsReverseTriggered(transform, player));
				if (allConditionsSatisfied) {
					ExecuteActionsForTiming(ActionTiming.OnEnter);
					OnMagicTriggerEnter?.Invoke();
				}
				else if (allConditionsNegativelySatisfied) {
					ExecuteNegativeActionsForTiming(ActionTiming.OnEnter);
					OnNegativeMagicTriggerEnter?.Invoke();
				}
			}
		}

		public void OnBetterTriggerExit(Collider other) {
			if (!enabled) return;

			if (other.TaggedAsPlayer()) {
				playerIsInTriggerZone = false;
				GameObject player = other.gameObject;
				ExecuteActionsForTiming(ActionTiming.OnExit);
				ExecuteNegativeActionsForTiming(ActionTiming.OnExit);
				OnMagicTriggerExit?.Invoke();

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

		#region Saving

		[Serializable]
		public class MagicTriggerSave : SerializableSaveObject<MagicTrigger> {
			List<List<bool>> gameObjectsToEnableState = new List<List<bool>>();
			List<List<bool>> gameObjectsToDisableState = new List<List<bool>>();
			List<List<bool>> scriptsToEnableStatePerAction = new List<List<bool>>();
			List<List<bool>> scriptsToDisableStatePerAction = new List<List<bool>>();
			bool hasTriggeredOnStay;
			bool hasNegativeTriggeredOnStay;

			public MagicTriggerSave(MagicTrigger magicTrigger) : base(magicTrigger) {
				foreach (var action in magicTrigger.actionsToTrigger) {
					List<bool> objectsToEnableState = new List<bool>();
					List<bool> objectsToDisableState = new List<bool>();
					List<bool> scriptsToEnableState = new List<bool>();
					List<bool> scriptsToDisableState = new List<bool>();
					if (action.objectsToEnable != null) {
						foreach (var objToEnable in action.objectsToEnable) {
							if (objToEnable != null) {
								objectsToEnableState.Add(objToEnable.activeSelf);
							}
						}
					}
					if (action.objectsToDisable != null) {
						foreach (var objToDisable in action.objectsToDisable) {
							if (objToDisable != null) {
								objectsToDisableState.Add(objToDisable.activeSelf);
							}
						}
					}
					if (action.scriptsToEnable != null) {
						foreach (var scriptToEnable in action.scriptsToEnable) {
							if (scriptToEnable != null) {
								scriptsToEnableState.Add(scriptToEnable.enabled);
							}
						}
					}
					if (action.scriptsToDisable != null) {
						foreach (var scriptToDisable in action.scriptsToDisable) {
							if (scriptToDisable != null) {
								scriptsToDisableState.Add(scriptToDisable.enabled);
							}
						}
					}
					this.gameObjectsToEnableState.Add(objectsToEnableState);
					this.gameObjectsToDisableState.Add(objectsToDisableState);
					this.scriptsToEnableStatePerAction.Add(scriptsToEnableState);
					this.scriptsToDisableStatePerAction.Add(scriptsToDisableState);
				}
				this.hasTriggeredOnStay = magicTrigger.hasTriggeredOnStay;
				this.hasNegativeTriggeredOnStay = magicTrigger.hasNegativeTriggeredOnStay;
			}

			public override void LoadSave(MagicTrigger magicTrigger) {
				for (int i = 0; i < magicTrigger.actionsToTrigger.Count; i++) {
					TriggerAction action = magicTrigger.actionsToTrigger[i];

					if (action.objectsToEnable != null) {
						for (int j = 0; j < action.objectsToEnable.Length; j++) {
							if (action.objectsToEnable[j] == null) continue;
							action.objectsToEnable[j].SetActive(this.gameObjectsToEnableState[i][j]);
						}
					}
					if (action.objectsToDisable != null) {
						for (int j = 0; j < action.objectsToDisable.Length; j++) {
							if (action.objectsToDisable[j] == null) continue;
							action.objectsToDisable[j].SetActive(this.gameObjectsToDisableState[i][j]);
						}
					}
					if (action.scriptsToEnable != null) {
						for (int j = 0; j < action.scriptsToEnable.Length; j++) {
							if (action.scriptsToEnable[j] == null) continue;
							action.scriptsToEnable[j].enabled = this.scriptsToEnableStatePerAction[i][j];
						}
					}
					if (action.scriptsToDisable != null) {
						for (int j = 0; j < action.scriptsToDisable.Length; j++) {
							if (action.scriptsToDisable[j] == null) continue;
							action.scriptsToDisable[j].enabled = this.scriptsToDisableStatePerAction[i][j];
						}
					}
				}

				magicTrigger.hasTriggeredOnStay = this.hasTriggeredOnStay;
				magicTrigger.hasNegativeTriggeredOnStay = this.hasNegativeTriggeredOnStay;
			}
		}
		#endregion
	}
}
