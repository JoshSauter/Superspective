using System;
using UnityEngine;
using EpitaphUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTriggerMechanics {
	public enum TriggerConditionType {
		PlayerFacingDirection,
		PlayerFacingObject,
		PlayerFacingAwayFromObject,
		PlayerFacingPosition,
		PlayerFacingAwayFromPosition,
		PlayerMovingDirection,
		RendererVisible,
		RendererNotVisible
	}

	[Serializable]
	public class TriggerCondition {
		public TriggerConditionType triggerCondition;
		public bool useLocalCoordinates;
		[Range(-1.0f,1.0f)]
		public float triggerThreshold = 0.01f;

		// enum-specific target
		public Vector3 targetDirection;
		public Collider targetObject;
		public Renderer targetRenderer;
		public Vector3 targetPosition;

		public bool allowTriggeringWhileInsideObject = false;

		public float Evaluate(Transform triggerTransform, Collider player) {
			Vector3 realTargetDirection = targetDirection;
			Transform cameraTransform = EpitaphScreen.instance.playerCamera.transform;
			switch (triggerCondition) {
				case TriggerConditionType.PlayerFacingDirection:
					realTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
					return Vector3.Dot(cameraTransform.forward, realTargetDirection.normalized);
				case TriggerConditionType.PlayerFacingObject: {
					// TODO: Handle player being inside of object as a "always false" case
					Vector3 objectToPlayerVector = (targetObject.ClosestPointOnBounds(cameraTransform.position) - cameraTransform.position).normalized;
					bool insideTargetObject = false;
					return insideTargetObject ? -1 : Vector3.Dot(cameraTransform.forward, objectToPlayerVector);
				}
				case TriggerConditionType.PlayerFacingAwayFromObject: {
					// TODO: Handle player being inside of object as a "always false" case
					Vector3 playerToObjectVector = (cameraTransform.position - targetObject.ClosestPointOnBounds(cameraTransform.position)).normalized;
					bool insideTargetObject = false;
					return insideTargetObject ? -1 : Vector3.Dot(cameraTransform.forward, playerToObjectVector);
				}
				case TriggerConditionType.PlayerFacingPosition: {
					Vector3 positionToPlayerVector = (targetPosition - cameraTransform.position).normalized;
					return Vector3.Dot(cameraTransform.forward, positionToPlayerVector);
				}
				case TriggerConditionType.PlayerFacingAwayFromPosition: {
					Vector3 playerToPositionVector = (cameraTransform.position - targetPosition).normalized;
					return Vector3.Dot(cameraTransform.forward, playerToPositionVector);
				}
				case TriggerConditionType.PlayerMovingDirection: {
					realTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
					PlayerMovement playerMovement = player.gameObject.GetComponent<PlayerMovement>();
					return Vector3.Dot(playerMovement.curVelocity.normalized, realTargetDirection.normalized);
				}
				case TriggerConditionType.RendererVisible:
					return targetRenderer.IsVisibleFrom(EpitaphScreen.instance.playerCamera) ? 1 : -1;
				case TriggerConditionType.RendererNotVisible:
					return targetRenderer.IsVisibleFrom(EpitaphScreen.instance.playerCamera) ? -1 : 1;
				default:
					throw new Exception($"TriggerCondition: {triggerCondition} not handled!");
			}
		}

		public bool IsTriggered(Transform triggerTransform, Collider player) {
			return Evaluate(triggerTransform, player) > triggerThreshold;
		}

		public bool IsReverseTriggered(Transform triggerTransform, Collider player) {
			return Evaluate(triggerTransform, player) < -triggerThreshold;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TriggerCondition))]
	public class TriggerConditionDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			AddSeparator();
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);
			float defaultWidth = EditorGUIUtility.labelWidth;

			SerializedProperty triggerCondition = property.FindPropertyRelative("triggerCondition");
			SerializedProperty triggerThreshold = property.FindPropertyRelative("triggerThreshold");

			SerializedProperty useLocalCoordinates = property.FindPropertyRelative("useLocalCoordinates");
			SerializedProperty allowTriggeringWhileInsideObject = property.FindPropertyRelative("allowTriggeringWhileInsideObject");

			SerializedProperty targetDirection = property.FindPropertyRelative("targetDirection");
			SerializedProperty targetObject = property.FindPropertyRelative("targetObject");
			SerializedProperty targetRenderer = property.FindPropertyRelative("targetRenderer");
			SerializedProperty targetPosition = property.FindPropertyRelative("targetPosition");

			EditorGUILayout.PropertyField(triggerCondition);
			EditorGUILayout.Space();
			TriggerConditionType currentTriggerCondition =
				(TriggerConditionType)Enum.GetValues(typeof(TriggerConditionType)).GetValue(triggerCondition.enumValueIndex);

			GUIContent directionLabel = new GUIContent("Target Direction:");
			GUIContent objectLabel = new GUIContent("Target Object:");
			GUIContent rendererLabel = new GUIContent("Target Object:");
			GUIContent positionLabel = new GUIContent("Target Position:");
			GUIContent thresholdLabel = new GUIContent("Trigger Threshold:");
			GUIContent allowTriggeringInsideObjectLabel = new GUIContent("Allow triggering while inside of target object?");
			GUIContent useLocalCoordinatesLabel = new GUIContent("Use local coordinates?");

			EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);

			switch (currentTriggerCondition) {
				case TriggerConditionType.PlayerFacingDirection:
					EditorGUILayout.PropertyField(useLocalCoordinates, useLocalCoordinatesLabel);
					EditorGUILayout.PropertyField(targetDirection, directionLabel);
					break;
				case TriggerConditionType.PlayerFacingObject:
				case TriggerConditionType.PlayerFacingAwayFromObject:
					EditorGUILayout.PropertyField(targetObject, objectLabel);
					EditorGUILayout.Space();

					EditorGUIUtility.labelWidth = 300;
					EditorGUILayout.PropertyField(allowTriggeringWhileInsideObject, allowTriggeringInsideObjectLabel);
					EditorGUIUtility.labelWidth = defaultWidth;
					break;
				case TriggerConditionType.PlayerFacingPosition:
				case TriggerConditionType.PlayerFacingAwayFromPosition:
					EditorGUILayout.PropertyField(targetPosition, positionLabel);
					break;
				case TriggerConditionType.PlayerMovingDirection:
					EditorGUILayout.PropertyField(useLocalCoordinates, useLocalCoordinatesLabel);
					EditorGUILayout.PropertyField(targetDirection, directionLabel);
					break;
				case TriggerConditionType.RendererVisible:
					EditorGUILayout.PropertyField(targetRenderer, rendererLabel);
					break;
				case TriggerConditionType.RendererNotVisible:
					EditorGUILayout.PropertyField(targetRenderer, rendererLabel);
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