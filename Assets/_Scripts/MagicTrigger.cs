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
        PlayerMovingDirection,
		PlayerMovingAndFacingDirection
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
#endregion

	private bool hasTriggeredOnStay = false;
	private bool hasNegativeTriggeredOnStay = false;


	private void OnTriggerStay(Collider other) {
		if (!enabled) return;

        if (other.gameObject.tag == "Player") {
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
			else if (facingAmount < -playerFaceThreshold && OnNegativeMagicTriggerEnter != null) {
				OnNegativeMagicTriggerEnter(other);
			}
        }
    }

	private void OnTriggerExit(Collider other) {
		if (!enabled) return;

		if (other.gameObject.tag == "Player") {
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
			case MagicTrigger.TriggerConditionType.PlayerMovingAndFacingDirection:
				script.targetDirection = EditorGUILayout.Vector3Field("Player moving and facing towards: ", script.targetDirection);
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
