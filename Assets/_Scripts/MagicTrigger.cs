using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MagicTrigger : MonoBehaviour {
    public bool DEBUG;

    public enum TriggerConditionType {
        PlayerFacingDirection,
        PlayerFacingObject,
        PlayerFacingAwayFromObject,
        PlayerFacingPosition,
        PlayerFacingAwayFromPosition,
        PlayerMovingDirection
    }
    public TriggerConditionType triggerCondition;
    public float playerFaceThreshold;

	public bool disableScriptOnTrigger = false;
    public bool disableGameObjectOnTrigger = false;
    
    // enum-specific target
    public Vector3 targetDirection;
    public Collider targetObject;
    public Vector3 targetPosition;

    public bool allowTriggeringWhileInsideObject = false;

    public delegate void MagicAction(Collider other);
    public event MagicAction OnMagicTriggerStay;
    public event MagicAction OnMagicTriggerEnter;

    private void OnTriggerStay(Collider other) {
		if (!enabled) return;

        if (other.gameObject.tag == "Player") {
            float facingAmount = FacingAmount(other);
            if (DEBUG) {
                print("Amount facing: " + facingAmount + "\nThreshold: " + playerFaceThreshold + "\nPass?: " + (facingAmount > playerFaceThreshold));
            }
            if (facingAmount > playerFaceThreshold && OnMagicTriggerStay != null) {
                OnMagicTriggerStay(other);

				if (disableScriptOnTrigger) {
					enabled = false;
				}
				if (disableGameObjectOnTrigger) {
                    gameObject.SetActive(false);
                }
            }
        }
    }

	// There is a bug right now where if a player walks into a trigger zone but doesn't meet the enter conditions immediately, the trigger will not happen until the player leaves and re-enters
	// FIX: Right your own logic within OnTriggerStay using OnTriggerLeave to re-create the proper functionality for one-time triggers
    private void OnTriggerEnter(Collider other) {
		if (!enabled) return;

        if (other.gameObject.tag == "Player") {
            float facingAmount = FacingAmount(other);
            if (DEBUG) {
                print("Amount facing: " + facingAmount + "\nThreshold: " + playerFaceThreshold + "\nPass?: " + (facingAmount > playerFaceThreshold));
            }
            if (facingAmount > playerFaceThreshold && OnMagicTriggerEnter != null) {
                OnMagicTriggerEnter(other);

				if (disableScriptOnTrigger) {
					enabled = false;
				}
                if (disableGameObjectOnTrigger) {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    private float FacingAmount(Collider player) {
		Transform cameraTransform = player.transform.Find("Main Camera");

        switch (triggerCondition) {
            case TriggerConditionType.PlayerFacingDirection:
                return Vector3.Dot(cameraTransform.forward, targetDirection.normalized);

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
                PlayerMovement playerMovement = player.gameObject.GetComponent<PlayerMovement>();
                return Vector3.Dot(playerMovement.HorizontalVelocity3().normalized, targetDirection.normalized);
            }

            default:
                throw new System.Exception("TriggerCondition: " + triggerCondition + " not handled!");
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MagicTrigger))]
[CanEditMultipleObjects]
public class MagicTriggerEditor : Editor {
    public override void OnInspectorGUI() {
        MagicTrigger script = target as MagicTrigger;
		float defaultWidth = EditorGUIUtility.labelWidth;

		EditorGUILayout.Space();

        script.DEBUG = EditorGUILayout.Toggle("Debug logging?", script.DEBUG);

        EditorGUILayout.Space();
        EditorGUILayout.Separator();
        EditorGUILayout.Space();

        script.triggerCondition = (MagicTrigger.TriggerConditionType)EditorGUILayout.EnumPopup("Trigger Condition Type", script.triggerCondition);

        EditorGUILayout.Space();

        switch (script.triggerCondition) {
            case MagicTrigger.TriggerConditionType.PlayerFacingDirection:
                script.targetDirection = EditorGUILayout.Vector3Field("Player facing direction: ", script.targetDirection);
                break;
            case MagicTrigger.TriggerConditionType.PlayerFacingObject:
            case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromObject:
                script.targetObject = EditorGUILayout.ObjectField("Target object: ", script.targetObject, typeof(Collider), true) as Collider;

                EditorGUILayout.Space();
				
                EditorGUIUtility.labelWidth = 300;
                script.allowTriggeringWhileInsideObject = EditorGUILayout.Toggle("Allow triggering while inside of target object?", script.allowTriggeringWhileInsideObject);
                EditorGUIUtility.labelWidth = defaultWidth;
                break;
            case MagicTrigger.TriggerConditionType.PlayerFacingPosition:
            case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromPosition:
                script.targetPosition = EditorGUILayout.Vector3Field("Player facing position: ", script.targetPosition);
                break;
            case MagicTrigger.TriggerConditionType.PlayerMovingDirection:
                script.targetDirection = EditorGUILayout.Vector3Field("Player moving towards: ", script.targetDirection);
                break;
        }

        EditorGUILayout.Space();

        script.playerFaceThreshold = EditorGUILayout.Slider("Trigger threshold: ", script.playerFaceThreshold, -1, 1);

        EditorGUILayout.Space();

		EditorGUIUtility.labelWidth = 300;
		script.disableGameObjectOnTrigger = EditorGUILayout.Toggle("Disable this gameobject upon trigger? ", script.disableGameObjectOnTrigger);

		EditorGUILayout.Space();

		script.disableScriptOnTrigger = EditorGUILayout.Toggle("Disable this script upon trigger? ", script.disableScriptOnTrigger);
		EditorGUIUtility.labelWidth = defaultWidth;

		EditorGUILayout.Space();
	}
}

#endif
