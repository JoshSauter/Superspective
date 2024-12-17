using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MagicTriggerMechanics;
using MagicTriggerMechanics.TriggerActions;
using MagicTriggerMechanics.TriggerConditions;
using PortalMechanics;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEditor;
using UnityEngine;

public class MigrateMagicTriggers : EditorWindow {
    private static bool windowOpen = false;
    
    // Add a menu item and shortcut (Ctrl+Shift+L) to open the tool
    [MenuItem("Tools/Migrate MagicTriggers %#l")]  // % is Ctrl, # is Shift, & is Alt
    public static void ShowWindow() {
        // Opens or focuses the window
        var window = GetWindow<MigrateMagicTriggers>("Migrate MagicTriggers (Ctrl+Shift+L)");
        if (windowOpen) {
            window.Close();
            windowOpen = false;
        }
        else {
            window.Show();
            windowOpen = true;
        }
    }
    
    // Draw the GUI for the window
    private void OnGUI() {
        GUILayout.Label("Select a Level", EditorStyles.boldLabel);

        // Confirm button to open the selected level
        if (GUILayout.Button("Migrate MagicTriggers")) {
            DoMigration();
        }
    }

    private static void DoMigration() {
        MagicTrigger[] magicTriggers = GetMagicTriggers();
        Debug.Log($"Migrating {magicTriggers.Length} MagicTriggers...");
        foreach (var magicTrigger in magicTriggers) {
            MigrateMagicTrigger(magicTrigger);
        }
    }

    private static void MigrateMagicTrigger(MagicTrigger script) {
        script.triggerConditionsNew = script
            .triggerConditions
            .Select(MigrateCondition)
            .ToList();
        script.actionsToTriggerNew = script
            .actionsToTrigger
            .Select(MigrateAction)
            .ToList();
        
        Debug.Log($"Migrated MagicTrigger {script.name} with {script.triggerConditionsNew.Count} conditions and {script.actionsToTriggerNew.Count} actions.", script);
    }

    private static TriggerCondition MigrateCondition(TriggerCondition_Deprecated old) {
        switch (old.triggerCondition) {
            case TriggerConditionType.PlayerFacingDirection:
                return new PlayerFacingDirectionCondition {
                    targetDirection = old.targetDirection,
                    useLocalCoordinates = old.useLocalCoordinates,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.PlayerFacingObject:
                return new PlayerFacingObjectCondition() {
                    targetObject = old.targetObject,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.PlayerFacingAwayFromObject:
                return new PlayerFacingAwayFromObjectCondition() {
                    targetObject = old.targetObject,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.PlayerFacingPosition:
                return new PlayerFacingPositionCondition() {
                    targetPosition = old.targetPosition,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.PlayerFacingAwayFromPosition:
                return new PlayerFacingAwayFromPositionCondition() {
                    targetPosition = old.targetPosition,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.PlayerMovingDirection:
                return new PlayerMovingDirectionCondition() {
                    targetDirection = old.targetDirection,
                    useLocalCoordinates = old.useLocalCoordinates,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.RendererVisible:
                return new RendererVisibleCondition() {
                    targetRenderer = old.targetRenderer
                };
            case TriggerConditionType.RendererNotVisible:
                return new RendererNotVisibleCondition() {
                    targetRenderer = old.targetRenderer
                };
            case TriggerConditionType.PlayerInDirectionFromPoint:
                return new PlayerInDirectionFromPointCondition() {
                    targetPosition = old.targetPosition,
                    targetDirection = old.targetDirection,
                    useLocalCoordinates = old.useLocalCoordinates,
                    triggerThreshold = old.triggerThreshold
                };
            case TriggerConditionType.LevelsAreActive:
                return new AnyLevelsActiveCondition() {
                    targetLevels = old.targetLevels
                };
            case TriggerConditionType.PlayerScaleWithinRange:
                return new PlayerScaleWithinRangeCondition() {
                    targetPlayerScaleRange = old.targetPlayerScaleRange
                };
            case TriggerConditionType.PlayerWithinCollider:
                return new PlayerWithinColliderCondition() {
                    targetObject = old.targetObject
                };
            case TriggerConditionType.PlayerOutsideOfCollider:
                return new PlayerOutsideOfColliderCondition() {
                    targetObject = old.targetObject
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static TriggerAction MigrateAction(TriggerAction_Deprecated old) {
        switch (old.action) {
            case TriggerActionType.DisableSelfScript:
                return new DisableSelfScriptAction() {
                    actionTiming = old.actionTiming
                };
            case TriggerActionType.DisableSelfGameObject:
                return new DisableSelfGameObjectAction() {
                    actionTiming = old.actionTiming
                };
            case TriggerActionType.EnableDisableScripts:
                return new EnableDisableScriptsAction() {
                    actionTiming = old.actionTiming,
                    scriptsToEnable = old.scriptsToEnable,
                    scriptsToDisable = old.scriptsToDisable
                };
            case TriggerActionType.EnableDisableGameObjects:
                return new EnableDisableGameObjectsAction() {
                    actionTiming = old.actionTiming,
                    objectsToEnable = old.objectsToEnable,
                    objectsToDisable = old.objectsToDisable
                };
            case TriggerActionType.ToggleScripts:
                return new ToggleScriptsAction() {
                    actionTiming = old.actionTiming,
                    scriptsToEnable = old.scriptsToEnable,
                    scriptsToDisable = old.scriptsToDisable
                };
            case TriggerActionType.ToggleGameObjects:
                return new ToggleGameObjectsAction() {
                    actionTiming = old.actionTiming,
                    objectsToEnable = old.objectsToEnable,
                    objectsToDisable = old.objectsToDisable
                };
            case TriggerActionType.ChangeLevel:
                return new ChangeLevelAction() {
                    actionTiming = old.actionTiming,
                    onlyTriggerForward = old.onlyTriggerForward,
                    levelForward = old.levelForward,
                    levelBackward = old.levelBackward
                };
            case TriggerActionType.PowerOrDepowerPowerTrail:
                return new PowerOrDepowerAction() {
                    actionTiming = old.actionTiming,
                    poweredObject = old.powerTrail.pwr,
                    setPowerIsOn = old.setPowerIsOn
                };
            case TriggerActionType.ChangeVisibilityState:
                return new ChangeVisibilityStateAction() {
                    actionTiming = old.actionTiming,
                    dimensionObjects = old.dimensionObjects,
                    visibilityState = old.visibilityState
                };
            case TriggerActionType.PlayCameraFlythrough:
                return new PlayCameraFlythroughAction() {
                    actionTiming = old.actionTiming,
                    flythroughCameraLevel = old.flythroughCameraLevel
                };
            case TriggerActionType.EnablePortalRendering:
                return new EnableDisablePortalRendering() {
                    actionTiming = old.actionTiming,
                    portalsToEnable = old.portalsToEnable.Select(SerializableReference<Portal, Portal.PortalSave>.FromGenericReference).ToArray(),
                    portalsToDisable = old.portalsToDisable.Select(SerializableReference<Portal, Portal.PortalSave>.FromGenericReference).ToArray()
                };
            case TriggerActionType.UnityEvent:
                return new UnityEventAction() {
                    actionTiming = old.actionTiming,
                    unityEvent = old.unityEvent
                };
            case TriggerActionType.ToggleColliders:
                return new ToggleCollidersAction() {
                    actionTiming = old.actionTiming,
                    collidersToEnable = old.collidersToEnable,
                    collidersToDisable = old.collidersToDisable
                };
            case TriggerActionType.PlayLevelChangeBanner:
                return new PlayLevelChangeBannerAction() {
                    actionTiming = old.actionTiming,
                    onlyTriggerForward = old.onlyTriggerForward,
                    levelForward = old.levelForward,
                    levelBackward = old.levelBackward
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static MagicTrigger[] GetMagicTriggers() {
        return Selection.gameObjects.SelectMany(go => go.GetComponentsInChildrenRecursively<MagicTrigger>()).ToArray();
    }
}
