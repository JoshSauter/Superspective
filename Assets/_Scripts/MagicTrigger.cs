using System;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
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
        PlayerMovingDirection,
		PlayerMovingAndFacingDirection
    }
    public TriggerConditionType triggerCondition;
	public float playerFaceThreshold = 0.01f;

	public bool disableScriptOnTrigger = false;
    public bool disableGameObjectOnTrigger = false;
    
    // enum-specific target
    public Vector3 targetDirection;
    public Collider targetObject;
    public Vector3 targetPosition;

    public bool allowTriggeringWhileInsideObject = false;

#region events
	public delegate void MagicAction(Collider other);
	// These events are fired when the trigger condition specified is met
    public event MagicAction OnMagicTriggerStay;
    public event MagicAction OnMagicTriggerEnter;
	public event MagicAction OnMagicTriggerStayOneTime;
	// These events are fired whenever the opposite of the trigger condition is met (does not necessarily form a complete set with the events above, something may not be fired)
	public event MagicAction OnNegativeMagicTriggerStay;
	public event MagicAction OnNegativeMagicTriggerEnter;
	public event MagicAction OnNegativeMagicTriggerStayOneTime;

	public event MagicAction OnMagicTriggerExit;
#endregion

	private bool hasTriggeredOnStay = false;
	private bool hasNegativeTriggeredOnStay = false;


	private void OnTriggerStay(Collider other) {
		if (!enabled) return;

        if (other.TaggedAsPlayer()) {
            float facingAmount = FacingAmount(other);
            if (DEBUG) {
                print("Amount facing: " + facingAmount + "\nThreshold: " + playerFaceThreshold + "\nPass?: " + (facingAmount > playerFaceThreshold));
            }
			// Magic Events triggered
			if (facingAmount > playerFaceThreshold) {
				if (DEBUG) Debug.Log("Triggering MagicTrigger!", this.gameObject);
				if (OnMagicTriggerStay != null) {
					OnMagicTriggerStay(other);

					if (disableScriptOnTrigger) {
						enabled = false;
					}
					if (disableGameObjectOnTrigger) {
						gameObject.SetActive(false);
					}
				}
				if (!hasTriggeredOnStay) {
					hasTriggeredOnStay = true;
					hasNegativeTriggeredOnStay = false;

					if (OnMagicTriggerStayOneTime != null) {
						OnMagicTriggerStayOneTime(other);

						if (disableScriptOnTrigger) {
							enabled = false;
						}
						if (disableGameObjectOnTrigger) {
							gameObject.SetActive(false);
						}
					}
				}
			}
			// Negative Magic Events triggered (negative triggers cannot turn self off)
			else if (facingAmount < -playerFaceThreshold) {
				if (DEBUG) Debug.Log("Triggering NegativeMagicTrigger!");
				if (OnNegativeMagicTriggerStay != null) {
					OnNegativeMagicTriggerStay(other);
				}
				if (!hasNegativeTriggeredOnStay) {
					hasNegativeTriggeredOnStay = true;
					hasTriggeredOnStay = false;
					if (OnNegativeMagicTriggerStayOneTime != null) {
						OnNegativeMagicTriggerStayOneTime(other);
					}
				}
			}
        }
    }

    private void OnTriggerEnter(Collider other) {
		if (!enabled) return;

        if (other.TaggedAsPlayer()) {
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
			else if (facingAmount < -playerFaceThreshold && OnNegativeMagicTriggerEnter != null) {
				OnNegativeMagicTriggerEnter(other);
			}
        }
    }

	private void OnTriggerExit(Collider other) {
		if (!enabled) return;

		if (other.TaggedAsPlayer()) {
			if (OnMagicTriggerExit != null) {
				OnMagicTriggerExit(other);
			}

			if (hasTriggeredOnStay) {
				hasTriggeredOnStay = false;
			}
			if (hasNegativeTriggeredOnStay) {
				hasNegativeTriggeredOnStay = false;
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
                return Vector3.Dot(playerMovement.curVelocity.normalized, targetDirection.normalized);
            }

			case TriggerConditionType.PlayerMovingAndFacingDirection: {
				PlayerMovement playerMovement = player.gameObject.GetComponent<PlayerMovement>();
				float movingDirection = Vector3.Dot(playerMovement.curVelocity.normalized, targetDirection.normalized);
				float facingDirection = Vector3.Dot(cameraTransform.forward, targetDirection.normalized);
				return Mathf.Min(movingDirection, facingDirection);
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
	SerializedProperty DEBUG;

	SerializedProperty triggerCondition;
	SerializedProperty targetDirection;
	SerializedProperty targetObject;
	SerializedProperty targetPosition;
	SerializedProperty allowTriggeringWhileInsideObject;
	SerializedProperty playerFaceThreshold;

	SerializedProperty disableGameObjectOnTrigger;
	SerializedProperty disableScriptOnTrigger;

	protected virtual void OnEnable() {
		DEBUG = serializedObject.FindProperty("DEBUG");

		triggerCondition = serializedObject.FindProperty("triggerCondition");
		targetDirection = serializedObject.FindProperty("targetDirection");
		targetObject = serializedObject.FindProperty("targetObject");
		targetPosition = serializedObject.FindProperty("targetPosition");
		allowTriggeringWhileInsideObject = serializedObject.FindProperty("allowTriggeringWhileInsideObject");
		playerFaceThreshold = serializedObject.FindProperty("playerFaceThreshold");

		disableGameObjectOnTrigger = serializedObject.FindProperty("disableGameObjectOnTrigger");
		disableScriptOnTrigger = serializedObject.FindProperty("disableScriptOnTrigger");
	}

	public sealed override void OnInspectorGUI() {
		serializedObject.Update();
		float defaultWidth = EditorGUIUtility.labelWidth;

		EditorGUILayout.Space();

        DEBUG.boolValue = EditorGUILayout.Toggle("Debug logging?", DEBUG.boolValue);

        EditorGUILayout.Space();
        EditorGUILayout.Separator();
        EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(triggerCondition);
		MagicTrigger.TriggerConditionType currentTriggerCondition =
				(MagicTrigger.TriggerConditionType)Enum.GetValues(typeof(MagicTrigger.TriggerConditionType)).GetValue(triggerCondition.enumValueIndex);
		if (EditorGUI.EndChangeCheck()) {
			foreach (var obj in targets) {
				var trigger = ((MagicTrigger)obj);
				trigger.triggerCondition = currentTriggerCondition;
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
			}
		}
		EditorGUILayout.Space();

        switch (currentTriggerCondition) {
            case MagicTrigger.TriggerConditionType.PlayerFacingDirection:
				EditorGUILayout.PropertyField(targetDirection);
                //targetDirection.vector3Value = EditorGUILayout.Vector3Field("Player facing direction: ", targetDirection.vector3Value);
                break;
            case MagicTrigger.TriggerConditionType.PlayerFacingObject:
            case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromObject:
                targetObject.objectReferenceValue = EditorGUILayout.ObjectField("Target object: ", targetObject.objectReferenceValue, typeof(Collider), true) as Collider;

                EditorGUILayout.Space();
				
                EditorGUIUtility.labelWidth = 300;
                allowTriggeringWhileInsideObject.boolValue = EditorGUILayout.Toggle("Allow triggering while inside of target object?", allowTriggeringWhileInsideObject.boolValue);
                EditorGUIUtility.labelWidth = defaultWidth;
                break;
            case MagicTrigger.TriggerConditionType.PlayerFacingPosition:
            case MagicTrigger.TriggerConditionType.PlayerFacingAwayFromPosition:
                targetPosition.vector3Value = EditorGUILayout.Vector3Field("Player facing position: ", targetPosition.vector3Value);
                break;
            case MagicTrigger.TriggerConditionType.PlayerMovingDirection:
                targetDirection.vector3Value = EditorGUILayout.Vector3Field("Player moving towards: ", targetDirection.vector3Value);
                break;
			case MagicTrigger.TriggerConditionType.PlayerMovingAndFacingDirection:
				targetDirection.vector3Value = EditorGUILayout.Vector3Field("Player moving and facing towards: ", targetDirection.vector3Value);
				break;
        }

        EditorGUILayout.Space();

        playerFaceThreshold.floatValue = EditorGUILayout.Slider("Trigger threshold: ", playerFaceThreshold.floatValue, -1, 1);

        EditorGUILayout.Space();

		EditorGUIUtility.labelWidth = 300;
		disableGameObjectOnTrigger.boolValue = EditorGUILayout.Toggle("Disable this gameobject upon trigger? ", disableGameObjectOnTrigger.boolValue);

		EditorGUILayout.Space();

		disableScriptOnTrigger.boolValue = EditorGUILayout.Toggle("Disable this script upon trigger? ", disableScriptOnTrigger.boolValue);
		EditorGUIUtility.labelWidth = defaultWidth;

		EditorGUILayout.Space();

		MoreOnInspectorGUI();

		serializedObject.ApplyModifiedProperties();
	}

	public virtual void MoreOnInspectorGUI() { }
}

#endif
