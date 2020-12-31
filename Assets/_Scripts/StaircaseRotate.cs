using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using NaughtyAttributes;

public class StaircaseRotate : MonoBehaviour {
	public bool DEBUG;

	// Solves a problem where the t value never hit 0 or 1 (because bottom of player was marginally too high to be exactly floor level)
	readonly float startEndGap = 0.25f;
	[ShowNativeProperty]
	public Vector3 currentGravity => Physics.gravity;
	public Vector3 pivotPoint { get { return transform.parent.position; } }

	[ShowIf("DEBUG")]
	[ReadOnly]
	public bool treatedAsADownStair;

	[ShowIf("DEBUG")]
	[ReadOnly]
	public float t;

	[ShowIf("DEBUG")][ReadOnly]
	public Vector3 startPosition;
	[ShowIf("DEBUG")][ReadOnly]
	public Vector3 endPosition;

	public Vector3 startGravityDirection = Vector3.zero;
	public Vector3 endGravityDirection = Vector3.zero;

	PlayerMovement playerMovement;

	// Gravity amplification is use to stop players from "flying" over down-stairs
	float minDistanceForGravAmplification = 4f;
	float maxDistanceForGravAmplification = 8f;
	float gravAmplificationMagnitude = 8f;

    void Start() {
		playerMovement = PlayerMovement.instance;

		if (startGravityDirection == Vector3.zero) {
			startGravityDirection = -transform.parent.up;
		}
		if (endGravityDirection == Vector3.zero) {
			endGravityDirection = transform.parent.forward;
		}

		Bounds initialBounds = transform.parent.GetComponent<Renderer>().bounds;

		startPosition = pivotPoint - endGravityDirection * Mathf.Abs(Vector3.Dot(initialBounds.size, endGravityDirection));
		endPosition = pivotPoint - startGravityDirection * Mathf.Abs(Vector3.Dot(initialBounds.size, startGravityDirection));

		Vector3 stairLine = endPosition - startPosition;
		startPosition += stairLine.normalized * startEndGap;
		endPosition -= stairLine.normalized * startEndGap;
    }

	private void OnTriggerEnter(Collider other) {
		GravityObject gravityObj = other.gameObject.GetComponent<GravityObject>();
		float testLerpValue = 0;
		if (other.TaggedAsPlayer()) {
			testLerpValue = GetLerpPositionOfPoint(playerMovement.bottomOfPlayer);
		}
		else if (gravityObj != null) {
			testLerpValue = GetLerpPositionOfPoint(other.transform.position);
		}
		Vector3 testGravityDirection1 = Vector3.Lerp(startGravityDirection, endGravityDirection, testLerpValue).normalized;
		Vector3 testGravityDirection2 = Vector3.Lerp(startGravityDirection, endGravityDirection, 1 - testLerpValue).normalized;

		treatedAsADownStair = Vector3.Angle(testGravityDirection1, Physics.gravity.normalized) > Vector3.Angle(testGravityDirection2, Physics.gravity.normalized);
	}

	private void OnTriggerStay(Collider other) {
		GravityObject gravityObj = other.gameObject.GetComponent<GravityObject>();
		if (other.TaggedAsPlayer()) {
			t = GetLerpPositionOfPoint(playerMovement.bottomOfPlayer);
			if (treatedAsADownStair) {
				t = 1 - t;
			}

			float gravAmplificationFactor = 1;
			if (treatedAsADownStair) {
				Vector3 projectedPlayerPos = ClosestPointOnLine(startPosition, endPosition, playerMovement.bottomOfPlayer);
				float distanceFromPlayerToStairs = Vector3.Distance(playerMovement.bottomOfPlayer, projectedPlayerPos);
				gravAmplificationFactor = 1 + gravAmplificationMagnitude * Mathf.InverseLerp(minDistanceForGravAmplification, maxDistanceForGravAmplification, distanceFromPlayerToStairs);
			}
			// TODO: Maybe store this information when player first enters zone
			float baseGravMagnitude = 32f;
			Physics.gravity = (baseGravMagnitude * gravAmplificationFactor) * Vector3.Lerp(startGravityDirection, endGravityDirection, t).normalized;

			float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
			if (treatedAsADownStair) {
				angleBetween = -angleBetween;
			}
			playerMovement.transform.rotation = Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) * playerMovement.transform.rotation;

			PlayerLook playerLook = PlayerLook.instance;
			playerLook.rotationY -= angleBetween * Vector3.Dot(playerMovement.transform.forward, playerMovement.ProjectedHorizontalVelocity().normalized);
			playerLook.rotationY = Mathf.Clamp(playerLook.rotationY, -playerLook.yClamp, playerLook.yClamp);
		}
		else if (gravityObj != null) {
			float objT = GetLerpPositionOfPoint(other.transform.position);
			gravityObj.gravityDirection = Vector3.Lerp(startGravityDirection, endGravityDirection, objT).normalized;
		}
	}

	private void OnTriggerExit(Collider other) {
		float angleToStartDirection = Vector3.Angle(startGravityDirection, Physics.gravity.normalized);
		float angleToEndDirection = Vector3.Angle(endGravityDirection, Physics.gravity.normalized);

		Vector3 exitGravity = angleToStartDirection < angleToEndDirection ? startGravityDirection : endGravityDirection;

		GravityObject gravityObj = other.gameObject.GetComponent<GravityObject>();
		if (other.TaggedAsPlayer()) {
			Physics.gravity = Physics.gravity.magnitude * exitGravity;

			float angleBetween = Vector3.Angle(playerMovement.transform.up, -Physics.gravity.normalized);
			if (treatedAsADownStair) {
				angleBetween = -angleBetween;
			}
			playerMovement.transform.rotation = Quaternion.FromToRotation(playerMovement.transform.up, -Physics.gravity.normalized) * playerMovement.transform.rotation;

			PlayerLook playerLook = PlayerLook.instance;
			playerLook.rotationY -= angleBetween * Vector3.Dot(playerMovement.transform.forward, playerMovement.ProjectedHorizontalVelocity().normalized);
			playerLook.rotationY = Mathf.Clamp(playerLook.rotationY, -playerLook.yClamp, playerLook.yClamp);
		}
		else if (gravityObj != null) {
			gravityObj.gravityDirection = exitGravity;
		}
	}

	float GetLerpPositionOfPoint(Vector3 point) {
		Vector3 projectedPlayerPos = ClosestPointOnLine(startPosition, endPosition, point);
		float t = Utils.Vector3InverseLerp(startPosition, endPosition, projectedPlayerPos);

		//debug.Log("StartPos: " + stairStartPos + ", EndPos: " + stairEndPos + ", PlayerPos: " + playerPos + ", t=" + t);
		return t;
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
