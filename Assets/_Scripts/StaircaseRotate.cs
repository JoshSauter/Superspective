using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

// NOTE: There is an assumption that rotating staircases are always in the default vertex layout provided by the ProBuilder Create Staircase tool
// If a staircase needs to be rotated, do not modify or mirror the mesh, instead rotate the staircase as needed with its Transform
public class StaircaseRotate : MonoBehaviour {
	public bool DEBUG = false;
	public DebugLogger debug;

	// Settings
	public RotationAxes localAxisOfRotation;
	public bool avoidDoubleRotation = false;
	public bool staircaseDown = false;
	public UnityEngine.Transform parentToRotate;
	public UnityEngine.Transform[] otherObjectsToRotate;
	float rotateLerpSpeed = 15f;
	float startEndGap = 0.25f;

	// State
	public float currentRotation = 0;

	// Miscellaneous
	private UnityEngine.Transform playerInZone;
	private Vector3 pivot { get { return transform.parent.position; } }
	private Vector3 globalAxis { get { return GetRotationAxis(localAxisOfRotation); } }
	private MeshRenderer stairRenderer;
	private Bounds initialBounds;

	public enum RotationAxes {
		right,
		left,
		up,
		down,
		forward,
		back
	}

	// Use this for initialization
	void Start () {
		debug = new DebugLogger(this, () => DEBUG);
        stairRenderer = transform.parent.GetComponent<MeshRenderer>();

		initialBounds = transform.parent.GetComponent<MeshFilter>().mesh.bounds;
	}

	private void OnTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer()) {
			playerInZone = other.transform;
			currentRotation = 0;
			SetAxisOfRotationBasedOnPlayerPosition(PlayerMovement.instance.bottomOfPlayer);
			StartCoroutine(UpdateTargetRotation());
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.TaggedAsPlayer()) {
			float closestMultipleOf90 = Mathf.RoundToInt(currentRotation / 90f) * 90;
			RotateObjects(closestMultipleOf90 - currentRotation);
			currentRotation = closestMultipleOf90;
			playerInZone = null;
		}
	}

	IEnumerator UpdateTargetRotation() {
		while (playerInZone != null) {
			float t = GetPlayerLerpPosition(PlayerMovement.instance.bottomOfPlayer);

			int staircaseDownMultiplier = staircaseDown ? -1 : 1;
			float desiredRotation = 90 * t * staircaseDownMultiplier;

			RotateObjects(desiredRotation - currentRotation);
			currentRotation = desiredRotation;

			yield return new WaitForFixedUpdate();
		}
	}

	void RotateObjects(float amountToRotate) {
		int staircaseDownMultiplier = staircaseDown ? -1 : 1;

		// Player should rotate around the pivot but without rotating the player's actual rotation (just position)
		UnityEngine.Transform player = PlayerMovement.instance.transform;
		Vector3 bottomOfPlayer = PlayerMovement.instance.bottomOfPlayer;
		Quaternion tempRotation = player.rotation;
		player.transform.position = (player.position - bottomOfPlayer) + RotateAroundPivot(bottomOfPlayer, pivot, Quaternion.Euler(globalAxis * amountToRotate));
		player.rotation = tempRotation;

		// Adjust the player's look direction up or down to further the effect
		PlayerLook playerLook = player.GetComponentInChildren<PlayerLook>();
		float lookMultiplier = Vector2.Dot(new Vector2(player.forward.x, player.forward.z).normalized, PlayerMovement.instance.ProjectedHorizontalVelocity().normalized);
		playerLook.rotationY -= lookMultiplier * Mathf.Abs(amountToRotate) * staircaseDownMultiplier;

		DirectionalLightSingleton.instance.transform.RotateAround(pivot, globalAxis, amountToRotate);

		Vector3 worldPosBefore = parentToRotate.position;
		parentToRotate.RotateAround(pivot, globalAxis, amountToRotate);
		Vector3 worldPosAfter = parentToRotate.position;
		foreach (var obj in otherObjectsToRotate) {
			// All other objects rotate as well as translate
			obj.RotateAround(pivot, globalAxis, amountToRotate);
		}

		parentToRotate.position -= (worldPosAfter - worldPosBefore);
		player.position -= (worldPosAfter - worldPosBefore);
		foreach (var obj in otherObjectsToRotate) {
			obj.position -= (worldPosAfter - worldPosBefore);
		}

		CameraFollow playerCam = player.GetComponentInChildren<CameraFollow>();
		playerCam.RecalculateWorldPositionLastFrame();
	}

	Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle) {
		return angle * (point - pivot) + pivot;
	}

	void SetAxisOfRotationBasedOnPlayerPosition(Vector3 playerPos) {
		if (GetPlayerLerpPosition(playerPos) > 0.5) {
			// Swap right/left, up/down, or forward/back
			localAxisOfRotation = (RotationAxes)((((int)localAxisOfRotation % 2) * -2 + 1) + (int)localAxisOfRotation);
		}
	}

	float GetPlayerLerpPosition(Vector3 playerPos) {
		Vector3 stairStartPos = GetStartPosition();
		Vector3 stairEndPos = GetEndPosition();
		Vector3 diff = stairEndPos - stairStartPos;
		stairStartPos += diff.normalized * startEndGap;
		stairEndPos -= diff.normalized * startEndGap;
		Vector3 projectedPlayerPos = ClosestPointOnLine(stairStartPos, stairEndPos, playerPos);

		if (DEBUG) {
			Debug.DrawRay(pivot, stairStartPos - pivot, Color.red);
			Debug.DrawRay(pivot, stairEndPos - pivot, Color.green);
			Debug.DrawRay(pivot, projectedPlayerPos - pivot, Color.blue);
		}

		float t = Utils.Vector3InverseLerp(stairStartPos, stairEndPos, projectedPlayerPos);

		//debug.Log("StartPos: " + stairStartPos + ", EndPos: " + stairEndPos + ", PlayerPos: " + playerPos + ", t=" + t);
		return t;
	}

	Vector3 GetStartPosition() {
		switch (localAxisOfRotation) {
			case RotationAxes.right:
				return pivot + -transform.parent.forward * initialBounds.size.z;
			case RotationAxes.left:
				return pivot + transform.parent.up * initialBounds.size.y;
			case RotationAxes.up:
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return Vector3.zero;
			case RotationAxes.forward:
				return pivot + transform.parent.up * initialBounds.size.y;
			case RotationAxes.back:
				return pivot + -transform.parent.forward * initialBounds.size.x;
			default:
				Debug.LogError("Unreachable");
				return Vector3.zero;
		}
	}

	Vector3 GetEndPosition() {
		switch (localAxisOfRotation) {
			case RotationAxes.right:
				return pivot + transform.parent.up * initialBounds.size.y;
			case RotationAxes.left:
				return pivot + -transform.parent.forward * initialBounds.size.z;
			case RotationAxes.up:
				Debug.LogError("Up/Down not handled yet");
				return Vector3.zero;
			case RotationAxes.down:
				Debug.LogError("Up/Down not handled yet");
				return Vector3.zero;
			case RotationAxes.forward:
				return pivot + -transform.parent.forward * initialBounds.size.x;
			case RotationAxes.back:
				return pivot + transform.parent.up * initialBounds.size.y;
			default:
				Debug.LogError("Unreachable");
				return Vector3.zero;
		}
	}

	Vector3 GetRotationAxis(RotationAxes axisOfRotation) {
		switch (axisOfRotation) {
			case RotationAxes.right:
				return transform.parent.TransformDirection(Vector3.right);
			case RotationAxes.left:
				return transform.parent.TransformDirection(Vector3.left);
			case RotationAxes.up:
				return transform.parent.TransformDirection(Vector3.up);
			case RotationAxes.down:
				return transform.parent.TransformDirection(Vector3.down);
			case RotationAxes.forward:
				return transform.parent.TransformDirection(Vector3.back);
			case RotationAxes.back:
				return transform.parent.TransformDirection(Vector3.forward);
			default:
				Debug.LogError("Unreachable");
				return Vector3.zero;
		}
	}

	Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint) {
		Vector3 vVector1 = vPoint - vA;
		Vector3 vVector2 = (vB - vA).normalized;

		float d = Vector3.Distance(vA, vB);
		float t = Vector3.Dot(vVector2, vVector1);

		if (t <= 0)
			return vA;

		if (t >= d)
			return vB;

		var vVector3 = vVector2 * t;

		var vClosestPoint = vA + vVector3;

		return vClosestPoint;
	}

}
