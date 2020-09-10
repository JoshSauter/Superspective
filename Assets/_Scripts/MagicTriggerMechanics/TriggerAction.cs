using System;
using UnityEngine;
using NaughtyAttributes;
using PowerTrailMechanics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	public enum TriggerActionType {
		DisableSelfScript,
		DisableSelfGameObject,
		EnableDisableScripts,
		EnableDisableGameObjects,
		ToggleScripts,              // Enables scripts when triggered forward, disables when triggered negatively
		ToggleGameObjects,          // Enables GameObjects when triggered forward, disables when triggered negatively
		ChangeLevel,
		ChangeActivePillar,
		PowerOrDepowerPowerTrail
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
		public Level levelForward;
		public Level levelBackward;
		public bool forwardSameScenePillar = true;
		public bool backwardSameScenePillar = true;
		// Setting up references for pillars in same scene can be done directly
		public DimensionPillar forwardPillar;
		public DimensionPillar backwardPillar;
		// Setting up references for pillars in different scenes has to be done through scene + gameObject names
		public Level forwardPillarLevel;
		public string forwardPillarName;
		public Level backwardPillarLevel;
		public string backwardPillarName;
		public PowerTrail powerTrail;
		public bool setPowerIsOn = true;

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
						objectToDisable.SetActive(false);
					}
					return;
				case TriggerActionType.ChangeLevel:
					LevelManager.instance.SwitchActiveScene(levelForward);
					return;
				case TriggerActionType.ChangeActivePillar:
					TriggerPillarChangeForward();
					return;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					powerTrail.powerIsOn = setPowerIsOn;
					return;
				default:
					return;
			}
		}

		public void NegativeExecute() {
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
					LevelManager.instance.SwitchActiveScene(levelBackward);
					return;
				case TriggerActionType.ChangeActivePillar:
					TriggerPillarChangeBackward();
					return;
				case TriggerActionType.DisableSelfScript:
				case TriggerActionType.DisableSelfGameObject:
				case TriggerActionType.EnableDisableScripts:
				case TriggerActionType.EnableDisableGameObjects:
					return;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					powerTrail.powerIsOn = !setPowerIsOn;
					return;
				default:
					return;
			}
		}

		private void TriggerPillarChangeForward() {
			if (forwardPillar == null && !forwardSameScenePillar) {
				string pillarKey = PillarKey(forwardPillarLevel, forwardPillarName);
				if (DimensionPillar.pillars.ContainsKey(pillarKey)) {
					forwardPillar = DimensionPillar.pillars[pillarKey];
				}
			}

			DimensionPillar.activePillar = forwardPillar;
		}

		private void TriggerPillarChangeBackward() {
			if (backwardPillar == null && !backwardSameScenePillar) {
				string pillarKey = PillarKey(backwardPillarLevel, backwardPillarName);
				if (DimensionPillar.pillars.ContainsKey(pillarKey)) {
					backwardPillar = DimensionPillar.pillars[pillarKey];
				}
			}

			DimensionPillar.activePillar = backwardPillar;
		}

		private string PillarKey(Level level, string name) {
			return LevelManager.instance.GetSceneName(level) + " " + name;
		}

		private bool HasTiming(ActionTiming timing) {
			return (int)timing > 0;
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

			SerializedProperty levelForward = property.FindPropertyRelative("levelForward");
			SerializedProperty levelBackward = property.FindPropertyRelative("levelBackward");

			SerializedProperty forwardSameScenePillar = property.FindPropertyRelative("forwardSameScenePillar");
			SerializedProperty backwardSameScenePillar = property.FindPropertyRelative("backwardSameScenePillar");
			SerializedProperty forwardPillar = property.FindPropertyRelative("forwardPillar");
			SerializedProperty backwardPillar = property.FindPropertyRelative("backwardPillar");
			SerializedProperty forwardPillarLevel = property.FindPropertyRelative("forwardPillarLevel");
			SerializedProperty forwardPillarName = property.FindPropertyRelative("forwardPillarName");
			SerializedProperty backwardPillarLevel = property.FindPropertyRelative("backwardPillarLevel");
			SerializedProperty backwardPillarName = property.FindPropertyRelative("backwardPillarName");

			SerializedProperty powerTrail = property.FindPropertyRelative("powerTrail");
			SerializedProperty setPowerIsOn = property.FindPropertyRelative("setPowerIsOn");

			GUIContent scriptsToEnableLabel = new GUIContent("Scripts to Enable:");
			GUIContent scriptsToDisableLabel = new GUIContent("Scripts to Disable:");
			GUIContent objectsToEnableLabel = new GUIContent("Objects to Enable:");
			GUIContent objectsToDisableLabel = new GUIContent("Objects to Disable:");
			GUIContent forwardLevelLabel = new GUIContent("Forward Level:");
			GUIContent backwardLevelLabel = new GUIContent("Backward Level:");
			GUIContent forwardSameScenePillarLabel = new GUIContent("Forward Pillar is in same scene?");
			GUIContent backwardSameScenePillarLabel = new GUIContent("Backward Pillar is in same scene?");
			GUIContent forwardPillarLabel = new GUIContent("Forward Pillar:");
			GUIContent backwardPillarLabel = new GUIContent("Backward Pillar:");
			GUIContent levelOfPillarLabel = new GUIContent("Level of Pillar:");
			GUIContent nameOfPillarLabel = new GUIContent("Name of Pillar:");
			GUIContent powerTrailLabel = new GUIContent("Power trail:");
			GUIContent setPowerIsOnLabel = new GUIContent("Power On/Off:");

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
					EditorGUILayout.PropertyField(levelForward, forwardLevelLabel);
					EditorGUILayout.PropertyField(levelBackward, backwardLevelLabel);
					break;
				case TriggerActionType.ChangeActivePillar:
					EditorGUIUtility.labelWidth = 250;
					EditorGUILayout.PropertyField(forwardSameScenePillar, forwardSameScenePillarLabel);
					EditorGUIUtility.labelWidth = defaultWidth;
					if (forwardSameScenePillar.boolValue) {
						EditorGUILayout.PropertyField(forwardPillar, forwardPillarLabel);
					}
					else {
						EditorGUILayout.PropertyField(forwardPillarLevel, levelOfPillarLabel);
						EditorGUILayout.PropertyField(forwardPillarName, nameOfPillarLabel);
					}

					EditorGUILayout.Space();

					EditorGUIUtility.labelWidth = 250;
					EditorGUILayout.PropertyField(backwardSameScenePillar, backwardSameScenePillarLabel);
					EditorGUIUtility.labelWidth = defaultWidth;
					if (forwardSameScenePillar.boolValue) {
						EditorGUILayout.PropertyField(backwardPillar, backwardPillarLabel);
					}
					else {
						EditorGUILayout.PropertyField(backwardPillarLevel, levelOfPillarLabel);
						EditorGUILayout.PropertyField(backwardPillarName, nameOfPillarLabel);
					}
					break;
				case TriggerActionType.PowerOrDepowerPowerTrail:
					EditorGUILayout.PropertyField(powerTrail, powerTrailLabel);
					EditorGUILayout.PropertyField(setPowerIsOn, setPowerIsOnLabel);
					break;
				default:
					break;
			}

			EditorGUI.EndProperty();

			EditorGUILayout.Space();
		}

		// This is weird code smell but without it there is empty space at the start of a List of these
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			return 0f;
		}

		private void AddSeparator() {
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		}
	}
#endif
}
