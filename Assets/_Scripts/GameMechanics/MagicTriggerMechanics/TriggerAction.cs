using System;
using System.Collections.Generic;
using LevelManagement;
using UnityEngine;
using NaughtyAttributes;
using PortalMechanics;
using PowerTrailMechanics;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	public enum TriggerActionType {
		DisableSelfScript = 0,
		DisableSelfGameObject = 1,
		EnableDisableScripts = 2,
		EnableDisableGameObjects = 3,
		ToggleScripts = 4,              // Enables scripts when triggered forward, disables when triggered negatively
		ToggleGameObjects = 5,          // Enables GameObjects when triggered forward, disables when triggered negatively
		ChangeLevel = 6,
		PowerOrDepowerPowerTrail = 8,
		ChangeVisibilityState = 9,
		PlayCameraFlythrough = 10,
		EnablePortalRendering = 11,		// Enables Portal rendering when triggered forward, disable when triggered negatively
		UnityEvent = 12,
		ToggleColliders = 13
	}

	[Flags]
	public enum ActionTiming {
		OnEnter = (1 << 0),
		OnExit = (1 << 1),          // OnExit is triggered regardless of trigger conditions
		OnceWhileOnStay = (1 << 2),
		EveryFrameOnStay = (1 << 3)
	}

	[Serializable]
	public class TriggerAction {
		[EnumFlags]
		[ValidateInput("HasTiming", "Action requires at least one timing set to activate")]
		public ActionTiming actionTiming = ActionTiming.OnceWhileOnStay;
		public TriggerActionType action;
		public GameObject[] objectsToEnable;
		public GameObject[] objectsToDisable;
		public MonoBehaviour[] scriptsToEnable;
		public MonoBehaviour[] scriptsToDisable;
		public bool onlyTriggerForward;
		public Levels levelForward;
		public Levels levelBackward;
		public bool forwardSameScenePillar = true;
		public bool backwardSameScenePillar = true;
		public PowerTrail powerTrail;
		public bool setPowerIsOn = true;
		public DimensionObject[] dimensionObjects;
		public VisibilityState visibilityState;
		public Levels flythroughCameraLevel;
		public SerializableReference[] portalsToEnable;
		public SerializableReference[] portalsToDisable;
		public UnityEvent unityEvent;
		public Collider[] collidersToEnable;
		public Collider[] collidersToDisable;

		public void Execute(MagicTrigger triggerScript) {
			triggerScript.debug.Log($"Timing: {actionTiming} Execute");
			switch (action) {
				case TriggerActionType.DisableSelfScript:
					triggerScript.enabled = false;
					return;
				case TriggerActionType.DisableSelfGameObject:
					triggerScript.gameObject.SetActive(false);
					return;
				case TriggerActionType.ToggleScripts:
				case TriggerActionType.EnableDisableScripts:
					foreach (var scriptToEnable in scriptsToEnable) {
						scriptToEnable.enabled = true;
					}
					foreach (var scriptToDisable in scriptsToDisable) {
						scriptToDisable.enabled = false;
					}
					return;
				case TriggerActionType.ToggleGameObjects:
				case TriggerActionType.EnableDisableGameObjects:
					foreach (var objectToEnable in objectsToEnable) {
						objectToEnable.SetActive(true);
					}
					foreach (var objectToDisable in objectsToDisable) {
						if (objectToDisable == null) {
							Debug.LogError($"{triggerScript.FullPath()}.MagicTrigger has a null objectToDisable!");
							continue;
						}
						
						objectToDisable.SetActive(false);
					}
					return;
				case TriggerActionType.ChangeLevel:
					// ManagerScene is a flag that we don't want to change level in this direction
					if (levelForward != Levels.ManagerScene && LevelManager.instance.ActiveScene != levelForward) {
						LevelManager.instance.SwitchActiveScene(levelForward);
					}
					return;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					powerTrail.pwr.PowerIsOn = setPowerIsOn;
					return;
				case TriggerActionType.ChangeVisibilityState:
					foreach (var dimensionObject in dimensionObjects) {
						dimensionObject.SwitchVisibilityState(visibilityState);
					}
					break;
				case TriggerActionType.PlayCameraFlythrough:
					CameraFlythrough.instance.PlayForLevel(flythroughCameraLevel);
					break;
				case TriggerActionType.EnablePortalRendering:
					PortalRenderingToggle(portalsToEnable, false);
					PortalRenderingToggle(portalsToDisable, true);
					break;
				case TriggerActionType.UnityEvent:
					unityEvent?.Invoke();
					break;
				case TriggerActionType.ToggleColliders:
					ColliderToggle(collidersToEnable, true);
					ColliderToggle(collidersToDisable, false);
					break;
				default:
					return;
			}
		}

		public void NegativeExecute() {
			if (onlyTriggerForward) return;
			
			switch (action) {
				case TriggerActionType.ToggleScripts:
					foreach (var scriptToEnable in scriptsToEnable) {
						scriptToEnable.enabled = false;
					}
					foreach (var scriptToDisable in scriptsToDisable) {
						scriptToDisable.enabled = true;
					}
					return;
				case TriggerActionType.ToggleGameObjects:
					foreach (var objectToEnable in objectsToEnable) {
						objectToEnable.SetActive(false);
					}
					foreach (var objectToDisable in objectsToDisable) {
						objectToDisable.SetActive(true);
					}
					return;
				case TriggerActionType.ChangeLevel:
					// ManagerScene is a flag that we don't want to change level in this direction
					if (levelBackward != Levels.ManagerScene && LevelManager.instance.ActiveScene != levelBackward) {
						LevelManager.instance.SwitchActiveScene(levelBackward);
					}
					return;
				case TriggerActionType.DisableSelfScript:
				case TriggerActionType.DisableSelfGameObject:
				case TriggerActionType.EnableDisableScripts:
				case TriggerActionType.EnableDisableGameObjects:
				case TriggerActionType.PlayCameraFlythrough:
				case TriggerActionType.UnityEvent:
					return;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					powerTrail.pwr.PowerIsOn = !setPowerIsOn;
					return;
				case TriggerActionType.ChangeVisibilityState:
					foreach (var dimensionObject in dimensionObjects) {
						dimensionObject.SwitchVisibilityState(dimensionObject.startingVisibilityState);
					}
					break;
				case TriggerActionType.EnablePortalRendering:
					PortalRenderingToggle(portalsToEnable, true);
					PortalRenderingToggle(portalsToDisable, false);
					break;
				case TriggerActionType.ToggleColliders:
					ColliderToggle(collidersToEnable, false);
					ColliderToggle(collidersToDisable, true);
					break;
				default:
					return;
			}
		}

		string PillarKey(Levels level, string name) {
			return level.ToName() + " " + name;
		}

		bool HasTiming(ActionTiming timing) {
			return (int)timing > 0;
		}

		private void PortalRenderingToggle(IEnumerable<SerializableReference> portals, bool pauseRendering) {
			foreach (var portal in portals) {
				portal.Reference.MatchAction(
					saveableObject => {
						if (saveableObject is Portal loadedPortal) {
							loadedPortal.pauseRendering = pauseRendering;
						}
					},
					serializedSaveObject => {
						if (serializedSaveObject is Portal.PortalSave unloadedPortal) {
							unloadedPortal.pauseRendering = pauseRendering;
						}
					}
				);
			}
		}
		
		private void ColliderToggle(IEnumerable<Collider> colliders, bool enabled) {
			foreach (var collider in colliders) {
				collider.enabled = enabled;
			}
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TriggerAction))]
	public class TriggerActionDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			AddSeparator();
			float defaultWidth = EditorGUIUtility.labelWidth;

			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty actionTiming = property.FindPropertyRelative("actionTiming");
			SerializedProperty action = property.FindPropertyRelative("action");

			SerializedProperty objectsToEnable = property.FindPropertyRelative("objectsToEnable");
			SerializedProperty objectsToDisable = property.FindPropertyRelative("objectsToDisable");

			SerializedProperty scriptsToEnable = property.FindPropertyRelative("scriptsToEnable");
			SerializedProperty scriptsToDisable = property.FindPropertyRelative("scriptsToDisable");

			SerializedProperty onlyTriggerForward = property.FindPropertyRelative("onlyTriggerForward");
			SerializedProperty levelForward = property.FindPropertyRelative("levelForward");
			SerializedProperty levelBackward = property.FindPropertyRelative("levelBackward");

			SerializedProperty powerTrail = property.FindPropertyRelative("powerTrail");
			SerializedProperty setPowerIsOn = property.FindPropertyRelative("setPowerIsOn");

			SerializedProperty dimensionObjects = property.FindPropertyRelative("dimensionObjects");
			SerializedProperty visibilityState = property.FindPropertyRelative("visibilityState");

			SerializedProperty flythroughCameraLevel = property.FindPropertyRelative("flythroughCameraLevel");

			SerializedProperty portalsToEnable = property.FindPropertyRelative("portalsToEnable");
			SerializedProperty portalsToDisable = property.FindPropertyRelative("portalsToDisable");

			SerializedProperty unityEvent = property.FindPropertyRelative("unityEvent");
			
			SerializedProperty collidersToEnable = property.FindPropertyRelative("collidersToEnable");
			SerializedProperty collidersToDisable = property.FindPropertyRelative("collidersToDisable");

			GUIContent scriptsToEnableLabel = new GUIContent("Scripts to Enable:");
			GUIContent scriptsToDisableLabel = new GUIContent("Scripts to Disable:");
			GUIContent objectsToEnableLabel = new GUIContent("Objects to Enable:");
			GUIContent objectsToDisableLabel = new GUIContent("Objects to Disable:");
			GUIContent onlyTriggerForwardLabel = new GUIContent("Only trigger forward?");
			GUIContent forwardLevelLabel = new GUIContent("Forward Level:");
			GUIContent backwardLevelLabel = new GUIContent("Backward Level:");
			GUIContent powerTrailLabel = new GUIContent("Power trail:");
			GUIContent setPowerIsOnLabel = new GUIContent("Power On/Off:");
			GUIContent dimensionObjectsLabel = new GUIContent("Dimension Objects:");
			GUIContent visibilityStateLabel = new GUIContent("Visibility State:");
			GUIContent flythroughCameraLabel = new GUIContent("Flythrough Camera Level:");
			GUIContent portalsToEnableLabel = new GUIContent("Portals to Enable Rendering:");
			GUIContent portalsToDisableLabel = new GUIContent("Portals to Disable Rendering:");
			GUIContent unityEventLabel = new GUIContent("Unity events:");
			GUIContent collidersToEnableLabel = new GUIContent("Colliders to Enable:");
			GUIContent collidersToDisableLabel = new GUIContent("Colliders to Disable:");

			EditorGUILayout.PropertyField(action);
			EditorGUILayout.PropertyField(actionTiming);

			TriggerActionType currentAction =
				(TriggerActionType)Enum.GetValues(typeof(TriggerActionType)).GetValue(action.enumValueIndex);

			EditorGUILayout.Space();

			switch (currentAction) {
				case TriggerActionType.DisableSelfScript:
				case TriggerActionType.DisableSelfGameObject:
					break;
				case TriggerActionType.ToggleScripts:
				case TriggerActionType.EnableDisableScripts:
					EditorGUILayout.PropertyField(scriptsToEnable, scriptsToEnableLabel, true);
					EditorGUILayout.PropertyField(scriptsToDisable, scriptsToDisableLabel, true);
					break;
				case TriggerActionType.ToggleGameObjects:
				case TriggerActionType.EnableDisableGameObjects:
					EditorGUILayout.PropertyField(objectsToEnable, objectsToEnableLabel, true);
					EditorGUILayout.PropertyField(objectsToDisable, objectsToDisableLabel, true);
					break;
				case TriggerActionType.ChangeLevel:
					EditorGUILayout.PropertyField(onlyTriggerForward, onlyTriggerForwardLabel);
					EditorGUILayout.PropertyField(levelForward, forwardLevelLabel);
					if (!onlyTriggerForward.boolValue) {
						EditorGUILayout.PropertyField(levelBackward, backwardLevelLabel);
					}
					break;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					EditorGUILayout.PropertyField(powerTrail, powerTrailLabel);
					EditorGUILayout.PropertyField(setPowerIsOn, setPowerIsOnLabel);
					break;
				case TriggerActionType.ChangeVisibilityState:
					EditorGUILayout.PropertyField(dimensionObjects, dimensionObjectsLabel);
					EditorGUILayout.PropertyField(visibilityState, visibilityStateLabel);
					break;
				case TriggerActionType.PlayCameraFlythrough:
					EditorGUILayout.PropertyField(flythroughCameraLevel, flythroughCameraLabel);
					break;
				case TriggerActionType.EnablePortalRendering:
					EditorGUILayout.PropertyField(portalsToEnable, portalsToEnableLabel);
					EditorGUILayout.PropertyField(portalsToDisable, portalsToDisableLabel);
					break;
				case TriggerActionType.UnityEvent:
					EditorGUILayout.PropertyField(unityEvent, unityEventLabel);
					break;
				case TriggerActionType.ToggleColliders:
					EditorGUILayout.PropertyField(collidersToEnable, collidersToEnableLabel);
					EditorGUILayout.PropertyField(collidersToDisable, collidersToDisableLabel);
					break;
				default:
					break;
			}

			EditorGUI.EndProperty();

			EditorGUILayout.Space();
		}

		static void AddSeparator() {
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		}
	}
#endif
}
