using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class StaircaseRotate : MonoBehaviour {
	public bool avoidDoubleRotation = false;
	public bool staircaseDown = false;
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
	Transform globalDirectionalLight;
	public RotationAxes axisOfRotation;
	public float currentRotation = 0;

    MeshRenderer stairRenderer;

	float startEndGap = 0.25f;

	// Use this for initialization
	void Start () {
        startRot = transform.parent.rotation.eulerAngles;
        stairRenderer = transform.parent.GetComponent<MeshRenderer>();
		globalDirectionalLight = GameObject.Find("Directional Light").transform;
	}

	private void OnTriggerEnter(Collider other) {
		SetAxisOfRotationBasedOnPlayerPosition(other.transform.position);
	}

	private void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
			float t = GetPlayerLerpPosition(other);
			int staircaseDownMultiplier = staircaseDown ? -1 : 1;
			float desiredRotation = 90 * t * staircaseDownMultiplier;
			float amountToRotate = desiredRotation - currentRotation;

			if (!avoidDoubleRotation) {
				transform.parent.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), amountToRotate);
			}

			// Player should rotate around the pivot but without rotating the player's actual rotation (just position)
			other.transform.position = RotateAroundPivot(other.transform.position, transform.parent.position, Quaternion.Euler(GetRotationAxis(axisOfRotation) * amountToRotate));
			// Adjust the player's look direction up or down to further the effect
			PlayerLook playerLook = other.transform.GetComponentInChildren<PlayerLook>();
			PlayerMovement playerMovement = other.transform.GetComponent<PlayerMovement>();
			//int lookDirection = (axisOfRotation == RotationAxes.right) ? 1 : -1;
			float lookMultiplier = Vector2.Dot(new Vector2(other.transform.forward.x, other.transform.forward.z).normalized, playerMovement.HorizontalVelocity().normalized);
			playerLook.rotationY -= lookMultiplier * Mathf.Abs(amountToRotate) * staircaseDownMultiplier;

			// Move the global directional light
			globalDirectionalLight.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), amountToRotate);

			foreach (var obj in otherObjectsToRotate) {
				// All other objects rotate as well as translate
				obj.RotateAround(transform.parent.position, GetRotationAxis(axisOfRotation), amountToRotate);
			}

			currentRotation = desiredRotation;
            //transform.parent.rotation = Quaternion.Euler(Vector3.Lerp(startRot, endRot, t));
        }
    }

	void SetAxisOfRotationBasedOnPlayerPosition(Vector3 playerPos) {
		float stairCaseStart = GetStartPosition();
		float stairCaseEnd = GetEndPosition();

		float distanceFromStart = 0;
		float distanceFromEnd = 0;
		switch (axisOfRotation) {
			case RotationAxes.left:
			case RotationAxes.right:
				distanceFromStart = Mathf.Abs(stairCaseStart - playerPos.z);
				distanceFromEnd = Mathf.Abs(stairCaseEnd - playerPos.z);
				break;
			case RotationAxes.up:
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return;
			case RotationAxes.forward:
			case RotationAxes.back:
				distanceFromStart = Mathf.Abs(stairCaseStart - playerPos.x);
				distanceFromEnd = Mathf.Abs(stairCaseEnd - playerPos.x);
				break;
		}

		currentRotation = 0;
		if (distanceFromStart > distanceFromEnd) {
			// Swap right/left, up/down, or forward/back
			axisOfRotation = (RotationAxes)((((int)axisOfRotation % 2) * -2 + 1) + (int)axisOfRotation);
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
				return stairRenderer.bounds.min.z + startEndGap;
			case RotationAxes.left:
				return stairRenderer.bounds.max.z - startEndGap;
			case RotationAxes.up:
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.forward:
				return stairRenderer.bounds.min.x + startEndGap;
			case RotationAxes.back:
				return stairRenderer.bounds.max.x - startEndGap;
			default:
				Debug.LogError("Unreachable");
				return 0;
		}
	}

	float GetEndPosition() {
		switch (axisOfRotation) {
			case RotationAxes.right:
				return stairRenderer.bounds.max.z - startEndGap;
			case RotationAxes.left:
				return stairRenderer.bounds.min.z + startEndGap;
			case RotationAxes.up:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return 0;
			case RotationAxes.forward:
				return stairRenderer.bounds.max.x - startEndGap;
			case RotationAxes.back:
				return stairRenderer.bounds.min.x + startEndGap;
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
				return Vector3.back;
			case RotationAxes.back:
				return Vector3.forward;
			default:
				Debug.LogError("Unreachable");
				return Vector3.zero;
		}
	}
}
