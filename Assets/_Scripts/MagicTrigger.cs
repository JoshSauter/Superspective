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
        PlayerFacingAwayFromPosition
    }
    public TriggerConditionType triggerCondition;
    public float playerFaceThreshold;

    public bool disableOnTrigger = false;
    
    // enum-specific target
    public Vector3 targetDirection;
    public Collider targetObject;
    public Vector3 targetPosition;

    public bool allowTriggeringWhileInsideObject = false;

    public delegate void MagicAction(Collider other);
    public event MagicAction OnMagicTrigger;

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.tag == "Player") {
            float facingAmount = FacingAmount(other);
            if (DEBUG) {
                print("Amount facing: " + facingAmount + "\nThreshold: " + playerFaceThreshold + "\nPass?: " + (facingAmount > playerFaceThreshold));
            }
            if (facingAmount > playerFaceThreshold && OnMagicTrigger != null) {
                OnMagicTrigger(other);
                if (disableOnTrigger) {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    private float FacingAmount(Collider player) {
        switch (triggerCondition) {
            case TriggerConditionType.PlayerFacingDirection:
                return Vector3.Dot(player.transform.forward, targetDirection.normalized);

            case TriggerConditionType.PlayerFacingObject: {
                // TODO: Handle player being inside of object as a "always false" case
                Vector3 objectToPlayerVector = (targetObject.ClosestPointOnBounds(player.transform.position) - player.transform.position).normalized;
                bool insideTargetObject = false;
                return insideTargetObject ? -1 : Vector3.Dot(player.transform.forward, objectToPlayerVector);
            }
            case TriggerConditionType.PlayerFacingAwayFromObject: {
                // TODO: Handle player being inside of object as a "always false" case
                Vector3 playerToObjectVector = (player.transform.position - targetObject.ClosestPointOnBounds(player.transform.position)).normalized;
                bool insideTargetObject = false;
                return insideTargetObject ? -1 : Vector3.Dot(player.transform.forward, playerToObjectVector);
            }

            case TriggerConditionType.PlayerFacingPosition: {
                Vector3 positionToPlayerVector = (targetPosition - player.transform.position).normalized;
                return Vector3.Dot(player.transform.forward, positionToPlayerVector);
            }
            case TriggerConditionType.PlayerFacingAwayFromPosition: {
                Vector3 playerToPositionVector = (player.transform.position - targetPosition).normalized;
                return Vector3.Dot(player.transform.forward, playerToPositionVector);
            }

            default:
                throw new System.Exception("TriggerCondition: " + triggerCondition + " not handled!");
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MagicTrigger))]
public class MagicTriggerEditor : Editor {
    public override void OnInspectorGUI() {
        MagicTrigger script = target as MagicTrigger;

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

                float defaultWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 300;
                script.allowTriggeringWhileInsideObject = EditorGUILayout.Toggle("Allow triggering while inside of target object?", script.allowTriggeringWhileInsideObject);
                EditorGUIUtility.labelWidth = defaultWidth;
                break;
            case MagicTrigger.TriggerConditionType.PlayerFacingPosition:
            case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromPosition:
                script.targetPosition = EditorGUILayout.Vector3Field("Player facing position: ", script.targetPosition);
                break;
        }

        EditorGUILayout.Space();

        script.playerFaceThreshold = EditorGUILayout.Slider("Trigger threshold: ", script.playerFaceThreshold, -1, 1);

        EditorGUILayout.Space();

        script.disableOnTrigger = EditorGUILayout.Toggle("Disable self upon trigger? ", script.disableOnTrigger);

        EditorGUILayout.Space();
    }
}

#endif
