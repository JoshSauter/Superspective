using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;
using System.Linq;
using Saving;
using System;
using System.Collections;
using MagicTriggerMechanics.TriggerActions;
using MagicTriggerMechanics.TriggerConditions;
using NaughtyAttributes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	[RequireComponent(typeof(UniqueId))]
	public class MagicTrigger : SaveableObject<MagicTrigger, MagicTrigger.MagicTriggerSave>, BetterTriggers {
		[HorizontalLine(color: EColor.Yellow)]
		[FormerlySerializedAs("triggerConditions")]
		public List<TriggerCondition_Deprecated> triggerConditionsOld = new List<TriggerCondition_Deprecated>();
		[HorizontalLine(color: EColor.Green)]
		[FormerlySerializedAs("actionsToTrigger")]
		public List<TriggerAction_Deprecated> actionsToTriggerOld = new List<TriggerAction_Deprecated>();
		
		[SerializeReference, Sirenix.OdinInspector.BoxGroup("Trigger Conditions")]
		[GUIColor(1f, 1f, 0.8f)]
		[Sirenix.OdinInspector.InfoBox("@NumConditionsLabel")]
		public List<TriggerCondition> triggerConditionsNew = new List<TriggerCondition>();
		[SerializeReference, Sirenix.OdinInspector.BoxGroup("Trigger Actions")]
		[GUIColor(0.8f, 1f, 0.8f)]
		[Sirenix.OdinInspector.InfoBox("@NumActionsLabel")]
		public List<TriggerAction> actionsToTriggerNew = new List<TriggerAction>();
		
		private string NumConditionsLabel => $"Conditions: {triggerConditionsNew.Count}";
		private string NumActionsLabel => $"Actions: {actionsToTriggerNew.Count}";

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

		public bool AllConditionsSatisfied => triggerConditionsNew.TrueForAll(tc => tc.IsTriggered(transform));
		public bool AllConditionsNegativelySatisfied => triggerConditionsNew.TrueForAll(tc => tc.IsReverseTriggered(transform));
		
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
			if (gameObject.layer != SuperspectivePhysics.PortalLayer) {
				gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
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
				if (DEBUG) {
					PrintDebugInfo();
				}

				bool allConditionsSatisfied = AllConditionsSatisfied;
				bool allConditionsNegativelySatisfied = AllConditionsNegativelySatisfied;
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
				if (DEBUG) {
					PrintDebugInfo();
				}

				bool allConditionsSatisfied = AllConditionsSatisfied;
				bool allConditionsNegativelySatisfied = AllConditionsNegativelySatisfied;
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
			foreach (var action in actionsToTriggerNew.Where(a => a.actionTiming.HasFlag(timing))) {
				action.Execute(this);
			}
		}
		protected void ExecuteNegativeActionsForTiming(ActionTiming timing) {
			foreach (var action in actionsToTriggerNew.Where(a => a.actionTiming.HasFlag(timing))) {
				action.NegativeExecute(this);
			}
		}

		protected void PrintDebugInfo() {
			string debugString = $"{gameObject.name}:\n";
			foreach (var condition in triggerConditionsNew) {
				debugString += condition.GetDebugInfo(transform);
			}
			debug.Log(debugString);
		}

		#region Saving

		[Serializable]
		public class MagicTriggerSave : SerializableSaveObject<MagicTrigger> {
			List<object> triggerActionSaves;
			bool hasTriggeredOnStay;
			bool hasNegativeTriggeredOnStay;

			public MagicTriggerSave(MagicTrigger magicTrigger) : base(magicTrigger) {
				this.hasTriggeredOnStay = magicTrigger.hasTriggeredOnStay;
				this.hasNegativeTriggeredOnStay = magicTrigger.hasNegativeTriggeredOnStay;

				triggerActionSaves = magicTrigger.actionsToTriggerNew.Select(a => a.GetSaveData(magicTrigger)).ToList();
			}

			public override void LoadSave(MagicTrigger magicTrigger) {
				magicTrigger.hasTriggeredOnStay = this.hasTriggeredOnStay;
				magicTrigger.hasNegativeTriggeredOnStay = this.hasNegativeTriggeredOnStay;

				for (int i = 0; i < triggerActionSaves.Count; i++) {
					object saveData = triggerActionSaves[i];
					magicTrigger.actionsToTriggerNew[i].LoadSaveData(saveData, magicTrigger);
				}
			}
		}
		#endregion
	}
}
