using System;
using System.Linq;
using LevelManagement;
using NaughtyAttributes;
using UnityEngine;
using SuperspectiveUtils;
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
		RendererNotVisible,
		PlayerInDirectionFromPoint,
		LevelsAreActive,
		PlayerScaleWithinRange,
		PlayerWithinCollider,
		PlayerOutsideOfCollider
	}

	[Serializable]
	public class TriggerCondition_Deprecated {
		public TriggerConditionType triggerCondition;
		public bool useLocalCoordinates;
		[Range(-1.0f,1.0f)]
		public float triggerThreshold = 0.01f;

		// enum-specific target
		public Vector3 targetDirection;
		public Collider targetObject;
		public Renderer targetRenderer;
		public Vector3 targetPosition;
		public Levels[] targetLevels;
		[MinMaxSlider(0.0f, 64f)]
		public Vector2 targetPlayerScaleRange;

		public bool allowTriggeringWhileInsideObject = false;

		public float Evaluate(Transform triggerTransform, GameObject player) {
			Vector3 realTargetDirection = targetDirection;
			Transform cameraTransform = SuperspectiveScreen.instance.playerCamera.transform;
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
					Vector3 playerToPosition = (targetPosition - cameraTransform.position).normalized;
					return Vector3.Dot(cameraTransform.forward, playerToPosition);
				}
				case TriggerConditionType.PlayerFacingAwayFromPosition: {
					Vector3 positionToPlayer = (cameraTransform.position - targetPosition).normalized;
					return Vector3.Dot(cameraTransform.forward, positionToPlayer);
				}
				case TriggerConditionType.PlayerMovingDirection: {
					realTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
					PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
					return Vector3.Dot(playerMovement.CurVelocity.normalized, realTargetDirection.normalized);
				}
				case TriggerConditionType.RendererVisible:
					return targetRenderer.IsVisibleFrom(SuperspectiveScreen.instance.playerCamera) ? 1 : -1;
				case TriggerConditionType.RendererNotVisible:
					return targetRenderer.IsVisibleFrom(SuperspectiveScreen.instance.playerCamera) ? -1 : 1;
				case TriggerConditionType.PlayerInDirectionFromPoint:
					realTargetDirection = (useLocalCoordinates) ? triggerTransform.TransformDirection(targetDirection) : targetDirection;
					Vector3 realTargetPosition = (useLocalCoordinates) ? triggerTransform.TransformPoint(targetPosition) : targetPosition;
					Vector3 playerToPositionDirection = (player.transform.position - realTargetPosition).normalized;
					return Vector3.Dot(realTargetDirection.normalized, playerToPositionDirection);
				case TriggerConditionType.LevelsAreActive:
					return targetLevels.Contains(LevelManager.instance.ActiveScene) ? 1 : -1;
				case TriggerConditionType.PlayerScaleWithinRange:
					return Player.instance.Scale >= targetPlayerScaleRange.x && Player.instance.Scale <= targetPlayerScaleRange.y ? 1 : -1;
				case TriggerConditionType.PlayerWithinCollider:
					return targetObject.PlayerIsInCollider() ? 1 : 0;
				case TriggerConditionType.PlayerOutsideOfCollider:
					return targetObject.PlayerIsInCollider() ? 0 : 1;
				default:
					throw new Exception($"TriggerCondition: {triggerCondition} not handled!");
			}
		}

		public string GetDebugInfo(Transform transform, GameObject player) {
			float triggerValue = Evaluate(transform, player);
			string debugString = $"Type: {triggerCondition}\nTriggerValue: {triggerValue}\nThreshold: {triggerThreshold}\nPass ?: {(triggerValue > triggerThreshold)}\n";

			string worldOrLocal = useLocalCoordinates ? "local" : "world";
			switch (triggerCondition) {
				case TriggerConditionType.PlayerFacingDirection:
					debugString += $"Player facing direction ({worldOrLocal}): {(useLocalCoordinates ? transform.TransformDirection(targetDirection) : targetDirection):F3}";
					break;
				case TriggerConditionType.PlayerFacingObject:
					break;
				case TriggerConditionType.PlayerFacingAwayFromObject:
					break;
				case TriggerConditionType.PlayerFacingPosition:
					break;
				case TriggerConditionType.PlayerFacingAwayFromPosition:
					break;
				case TriggerConditionType.PlayerMovingDirection:
					break;
				case TriggerConditionType.RendererVisible:
					break;
				case TriggerConditionType.RendererNotVisible:
					break;
				case TriggerConditionType.PlayerInDirectionFromPoint:
					Vector3 playerPos = (useLocalCoordinates ? transform.InverseTransformPoint(player.transform.position) : player.transform.position);
					Vector3 playerToTargetPosition = playerPos - targetPosition;
					debugString += $"Player position ({worldOrLocal}): {playerPos}\nPlayer to target position: {playerToTargetPosition}\n";
					break;
				case TriggerConditionType.LevelsAreActive:
					break;
				case TriggerConditionType.PlayerScaleWithinRange:
					break;
				case TriggerConditionType.PlayerWithinCollider:
				case TriggerConditionType.PlayerOutsideOfCollider:
					debugString += $"Player in collider {targetObject.FullPath()}? {targetObject.PlayerIsInCollider()}\n";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			debugString += "--------\n";
			return debugString;
		}

		public bool IsTriggered(Transform triggerTransform, GameObject player) {
			return Evaluate(triggerTransform, player) > triggerThreshold;
		}

		public bool IsReverseTriggered(Transform triggerTransform, GameObject player) {
			return Evaluate(triggerTransform, player) < -triggerThreshold;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TriggerCondition_Deprecated))]
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
			SerializedProperty targetLevels = property.FindPropertyRelative("targetLevels");
			SerializedProperty targetPlayerScaleRange = property.FindPropertyRelative("targetPlayerScaleRange");

			EditorGUILayout.PropertyField(triggerCondition);
			EditorGUILayout.Space();
			TriggerConditionType currentTriggerCondition =
				(TriggerConditionType)Enum.GetValues(typeof(TriggerConditionType)).GetValue(triggerCondition.enumValueIndex);

			GUIContent directionLabel = new GUIContent("Target Direction:");
			GUIContent objectLabel = new GUIContent("Target Object:");
			GUIContent rendererLabel = new GUIContent("Target Object:");
			GUIContent positionLabel = new GUIContent("Target Position:");
			GUIContent thresholdLabel = new GUIContent("Trigger Threshold:");
			GUIContent targetLevelsLabel = new GUIContent("Target level(s):");
			GUIContent targetPlayerScaleRangeLabel = new GUIContent("Target Player scale range:");
			GUIContent allowTriggeringInsideObjectLabel = new GUIContent("Allow triggering while inside of target object?");
			GUIContent useLocalCoordinatesLabel = new GUIContent("Use local coordinates?");


			switch (currentTriggerCondition) {
				case TriggerConditionType.PlayerFacingDirection:
					EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);
					EditorGUILayout.PropertyField(useLocalCoordinates, useLocalCoordinatesLabel);
					EditorGUILayout.PropertyField(targetDirection, directionLabel);
					break;
				case TriggerConditionType.PlayerFacingObject:
				case TriggerConditionType.PlayerFacingAwayFromObject:
					EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);
					EditorGUILayout.PropertyField(targetObject, objectLabel);
					EditorGUILayout.Space();

					EditorGUIUtility.labelWidth = 300;
					EditorGUILayout.PropertyField(allowTriggeringWhileInsideObject, allowTriggeringInsideObjectLabel);
					EditorGUIUtility.labelWidth = defaultWidth;
					break;
				case TriggerConditionType.PlayerFacingPosition:
				case TriggerConditionType.PlayerFacingAwayFromPosition:
					EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);
					EditorGUILayout.PropertyField(targetPosition, positionLabel);
					break;
				case TriggerConditionType.PlayerMovingDirection:
					EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);
					EditorGUILayout.PropertyField(useLocalCoordinates, useLocalCoordinatesLabel);
					EditorGUILayout.PropertyField(targetDirection, directionLabel);
					break;
				case TriggerConditionType.RendererVisible:
					EditorGUILayout.PropertyField(targetRenderer, rendererLabel);
					break;
				case TriggerConditionType.RendererNotVisible:
					EditorGUILayout.PropertyField(targetRenderer, rendererLabel);
					break;
				case TriggerConditionType.PlayerInDirectionFromPoint:
					EditorGUILayout.PropertyField(triggerThreshold, thresholdLabel);
					EditorGUILayout.PropertyField(useLocalCoordinates, useLocalCoordinatesLabel);
					EditorGUILayout.PropertyField(targetPosition, positionLabel);
					EditorGUILayout.PropertyField(targetDirection, directionLabel);
					break;
				case TriggerConditionType.LevelsAreActive:
					EditorGUILayout.PropertyField(targetLevels, targetLevelsLabel);
					break;
				case TriggerConditionType.PlayerScaleWithinRange:
					EditorGUILayout.PropertyField(targetPlayerScaleRange, targetPlayerScaleRangeLabel);
					break;
				case TriggerConditionType.PlayerWithinCollider:
				case TriggerConditionType.PlayerOutsideOfCollider:
					EditorGUILayout.PropertyField(targetObject, objectLabel);
					break;
			}

			EditorGUI.EndProperty();

			EditorGUILayout.Space();
		}

		void AddSeparator() {
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		}
	}
#endif
}