using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MagicTriggerMechanics;

public class ConvertOldMagicTriggerToNew : ScriptableWizard {
	[MenuItem("My Tools/Convert Old MagicTrigger to New MagicTrigger")]
	static void ConvertOldMagicTriggerToNewWizard() {
		DisplayWizard<ConvertOldMagicTriggerToNew>("Convert old MagicTriggers to MagicTriggerNew", "Convert All Selected");
	}

	// TODO: Learn how to make this undo-able
	private void OnWizardCreate() {
		int counter = 0;
		foreach (GameObject go in Selection.gameObjects) {
			if (go.GetComponents<MagicTrigger>().Length > 1) {
				Debug.LogError($"Multiple MagicTriggers on {go.name}, handle manually.", go);
				continue;
			}
			MagicTrigger oldMagicTrigger = go.GetComponent<MagicTrigger>();
			if (oldMagicTrigger != null) {
				ConvertMagicTrigger(oldMagicTrigger);
				counter++;

				EditorSceneManager.MarkSceneDirty(go.scene);
			}
		}

		Debug.Log($"Successfully converted {counter} MagicTriggers into MagicTriggerNew");
	}

	static void ConvertMagicTrigger(MagicTrigger oldTrigger) {
		// Get or add MagicTriggerNew
		MagicTriggerNew newTrigger = oldTrigger.gameObject.GetComponent<MagicTriggerNew>();
		if (newTrigger == null) {
			newTrigger = oldTrigger.gameObject.AddComponent<MagicTriggerNew>();
		}
		else {
			newTrigger.triggerConditions.Clear();
			newTrigger.actionsToTrigger.Clear();
		}

		// Convert old trigger conditions to new TriggerConditions
		CopyTrigggerConditions(oldTrigger, newTrigger);

		// Convert old trigger effects to new TriggerActions
		if (oldTrigger is ForceActivePillarToggle) {
			ForceActivePillarToggle pillarTrigger = oldTrigger as ForceActivePillarToggle;
			// Handle active pillar toggle triggers
			newTrigger.actionsToTrigger.Add(new TriggerAction {
				action = TriggerActionType.ChangeActivePillar,
				actionTiming = ActionTiming.OnceWhileOnStay,
				forwardSameScenePillar = pillarTrigger.forwardSameScenePillar,
				forwardPillar = pillarTrigger.forwardTriggeredPillar,
				forwardPillarLevel = pillarTrigger.forwardPillarLevel,
				forwardPillarName = pillarTrigger.forwardPillarName
			});
		}
		else if (oldTrigger is LevelChangeTrigger) {
			// Handle level change triggers
			LevelChangeTrigger levelChangeTrigger = oldTrigger as LevelChangeTrigger;
			newTrigger.actionsToTrigger.Add(new TriggerAction {
				action = TriggerActionType.ChangeLevel,
				actionTiming = ActionTiming.OnceWhileOnStay,
				levelForward = levelChangeTrigger.levelForward,
				levelBackward = levelChangeTrigger.levelBackward
			});
		}
		else if (oldTrigger is MagicSpawnDespawnToggle) {
			// Handle toggle trigger
			MagicSpawnDespawnToggle toggleTrigger = oldTrigger as MagicSpawnDespawnToggle;
			if (toggleTrigger.objectsToEnable.Length > 0 || toggleTrigger.objectsToDisable.Length > 0) {
				newTrigger.actionsToTrigger.Add(new TriggerAction {
					action = TriggerActionType.ToggleGameObjects,
					actionTiming = ActionTiming.OnceWhileOnStay,
					objectsToEnable = DeepListCopy(toggleTrigger.objectsToEnable),
					objectsToDisable = DeepListCopy(toggleTrigger.objectsToDisable)
				});
			}
			if (toggleTrigger.scriptsToEnable.Length > 0 || toggleTrigger.scriptsToDisable.Length > 0) {
				newTrigger.actionsToTrigger.Add(new TriggerAction {
					action = TriggerActionType.ToggleScripts,
					actionTiming = ActionTiming.OnceWhileOnStay,
					scriptsToEnable = DeepListCopy(toggleTrigger.scriptsToEnable),
					scriptsToDisable = DeepListCopy(toggleTrigger.scriptsToDisable)
				});
			}
		}
		else if (oldTrigger is MagicSpawnDespawn) {
			// Handle spawn despawn triggers
			MagicSpawnDespawn enableDisableTrigger = oldTrigger as MagicSpawnDespawn;
			if (enableDisableTrigger.objectsToEnable.Length > 0 || enableDisableTrigger.objectsToDisable.Length > 0) {
				newTrigger.actionsToTrigger.Add(new TriggerAction {
					action = TriggerActionType.EnableDisableGameObjects,
					actionTiming = ActionTiming.OnceWhileOnStay,
					objectsToEnable = DeepListCopy(enableDisableTrigger.objectsToEnable),
					objectsToDisable = DeepListCopy(enableDisableTrigger.objectsToDisable)
				});
			}
			if (enableDisableTrigger.scriptsToEnable.Length > 0 || enableDisableTrigger.scriptsToDisable.Length > 0) {
				newTrigger.actionsToTrigger.Add(new TriggerAction {
					action = TriggerActionType.EnableDisableScripts,
					actionTiming = ActionTiming.OnceWhileOnStay,
					scriptsToEnable = DeepListCopy(enableDisableTrigger.scriptsToEnable),
					scriptsToDisable = DeepListCopy(enableDisableTrigger.scriptsToDisable)
				});
			}
		}

		// Convert old self-disable effects to new TriggerActions
		if (oldTrigger.disableGameObjectOnTrigger) {
			newTrigger.actionsToTrigger.Add(new TriggerAction {
				action = TriggerActionType.DisableSelfGameObject,
				actionTiming = ActionTiming.OnceWhileOnStay
			});
		}
		if (oldTrigger.disableScriptOnTrigger) {
			newTrigger.actionsToTrigger.Add(new TriggerAction {
				action = TriggerActionType.DisableSelfScript,
				actionTiming = ActionTiming.OnceWhileOnStay
			});
		}

		oldTrigger.enabled = false;
	}

	static T[] DeepListCopy<T>(T[] original) {
		List<T> returnList = new List<T>();

		foreach (T i in original) {
			returnList.Add(i);
		}

		return returnList.ToArray();
	}

	static void CopyTrigggerConditions(MagicTrigger oldTrigger, MagicTriggerNew newTrigger) {
		switch (oldTrigger.triggerCondition) {
			case MagicTrigger.TriggerConditionType.PlayerFacingDirection:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingDirection,
					targetDirection = oldTrigger.targetDirection,
					useLocalCoordinates = oldTrigger.useLocalCoordinates,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerFacingObject:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingObject,
					targetObject = oldTrigger.targetObject,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromObject:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingAwayFromObject,
					targetObject = oldTrigger.targetObject,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerFacingPosition:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingPosition,
					targetPosition = oldTrigger.targetPosition,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromPosition:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingAwayFromPosition,
					targetPosition = oldTrigger.targetPosition,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerMovingDirection:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerMovingDirection,
					targetDirection = oldTrigger.targetDirection,
					useLocalCoordinates = oldTrigger.useLocalCoordinates,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			case MagicTrigger.TriggerConditionType.PlayerMovingAndFacingDirection:
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerMovingDirection,
					targetDirection = oldTrigger.targetDirection,
					useLocalCoordinates = oldTrigger.useLocalCoordinates,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				newTrigger.triggerConditions.Add(new TriggerCondition {
					triggerCondition = TriggerConditionType.PlayerFacingDirection,
					targetDirection = oldTrigger.targetDirection,
					useLocalCoordinates = oldTrigger.useLocalCoordinates,
					triggerThreshold = oldTrigger.playerFaceThreshold
				});
				break;
			default:
				break;
		}
	}
}
