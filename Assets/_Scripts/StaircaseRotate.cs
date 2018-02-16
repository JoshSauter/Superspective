using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaircaseRotate : MonoBehaviour {
	public bool avoidDoubleRotation = false;
	public enum RotationAxes {
		right,
		left,
		up,
		down,
		forward,
		back
	}

    Vector3 startRot;
	public Transform[] otherObjectsToRotate;
	public RotationAxes axisOfRotation;
	float currentRotation = 0;

    Collider collider;

	float startEndGap = 0.25f;

	// Use this for initialization
	void Start () {
        startRot = transform.parent.rotation.eulerAngles;
        collider = GetComponent<Collider>();
	}

    private void OnTriggerStay(Collider other) {
        if (other.tag == "Player") {
			float t = GetPlayerLerpPosition(other);
			float desiredRotation = 90 * t;

			if (!avoidDoubleRotation) {
				transform.parent.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), currentRotation - desiredRotation);
			}

			// Player should rotate around the pivot but without rotating the player's actual rotation (just position)
			other.transform.position = RotateAroundPivot(other.transform.position, transform.parent.position, Quaternion.Euler(GetRotationAxis(axisOfRotation) * (currentRotation - desiredRotation)));
			// Adjust the player's look direction up or down to further the effect
			PlayerLook playerLook = other.transform.GetComponentInChildren<PlayerLook>();
			PlayerMovement playerMovement = other.transform.GetComponent<PlayerMovement>();
			int lookDirection = (axisOfRotation == RotationAxes.forward || axisOfRotation == RotationAxes.right) ? 1 : -1;
			float lookMultiplier = Vector2.Dot(new Vector2(other.transform.forward.x, other.transform.forward.z).normalized, playerMovement.HorizontalVelocity().normalized);
			playerLook.rotationY += lookDirection * lookMultiplier * Mathf.Abs(currentRotation - desiredRotation);

			// Move the global directional light
			GameObject.Find("Directional Light").transform.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), currentRotation - desiredRotation);

			foreach (var obj in otherObjectsToRotate) {
				// All other objects rotate as well as translate
				obj.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), currentRotation - desiredRotation);
			}

			currentRotation = desiredRotation;
            //transform.parent.rotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, t));
        }
    }

	Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle) {
		return angle * (point - pivot) + pivot;
	}

	float GetPlayerLerpPosition(Collider player) {
		float playerStartPos = GetStartPosition();
		float playerEndPos = GetEndPosition();

		switch (axisOfRotation) {
			case RotationAxes.right:
			case RotationAxes.left:
				return Mathf.InverseLerp(playerStartPos, playerEndPos, player.transform.position.z);
			case RotationAxes.up:
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.forward:
			case RotationAxes.back:
				return Mathf.InverseLerp(playerStartPos, playerEndPos, player.transform.position.x);
			default:
				Debug.LogError("Unreachable");
				return 0;
		}
	}

	float GetStartPosition() {
		switch (axisOfRotation) {
			case RotationAxes.right:
			case RotationAxes.left:
				return collider.bounds.min.z + startEndGap;
			case RotationAxes.up:
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.forward:
				return collider.bounds.min.x + startEndGap;
			case RotationAxes.back:
				return collider.bounds.max.x - startEndGap;
			default:
				Debug.LogError("Unreachable");
				return 0;
		}
	}

	float GetEndPosition() {
		switch (axisOfRotation) {
			case RotationAxes.right:
			case RotationAxes.left:
				return collider.bounds.max.z - startEndGap;
			case RotationAxes.up:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.forward:
				return collider.bounds.max.x - startEndGap;
			case RotationAxes.back:
				return collider.bounds.min.x + startEndGap;
			default:
				Debug.LogError("Unreachable");
				return 0;
		}
	}

	Vector3 GetRotationAxis(RotationAxes axisOfRotation) {
		switch (axisOfRotation) {
			case RotationAxes.right:
				return Vector3.right;
			case RotationAxes.left:
				return Vector3.left;
			case RotationAxes.up:
				return Vector3.up;
			case RotationAxes.down:
				return Vector3.down;
			case RotationAxes.forward:
				return Vector3.forward;
			case RotationAxes.back:
				return Vector3.back;
			default:
				Debug.LogError("Unreachable");
				return Vector3.zero;
		}
	}
}
