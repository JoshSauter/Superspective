using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using UnityEngine.Rendering;

public class DimensionWall : MonoBehaviour {
	Renderer thisRenderer;
	DimensionPillar pillar;
	Renderer pillarRenderer;

	Vector3 TopOfPillar {
		get {
			if (pillar.HeightOverridden) {
				return BottomOfPillar + pillar.heightOverride * pillar.transform.up;
			}
			else {
				return pillarRenderer.bounds.center + pillar.transform.up * pillarRenderer.bounds.size.y / 2f;
			}
		}
	}

	Vector3 BottomOfPillar { get { return pillarRenderer.bounds.center - pillar.transform.up * pillarRenderer.bounds.size.y / 2f; } }

	public float PillarHeight {
		get {
			return TopOfPillar.y - BottomOfPillar.y;
		}
	}

	readonly float radsOffsetForDimensionWall = -0.1f;
	readonly float dimensionWallWidth = 0.01f;
	LayerMask roomBoundsMask;

	void Awake() {
		pillar = GetComponentInParent<DimensionPillar>();
		pillarRenderer = pillar.GetComponent<Renderer>();
		thisRenderer = GetComponent<Renderer>();

		roomBoundsMask = 1 << LayerMask.NameToLayer("RoomBounds");

		InitializeWallTransform();
    }

	void InitializeWallTransform() {
		transform.SetParent(transform);
		transform.localScale = new Vector3(dimensionWallWidth / transform.localScale.x, PillarHeight / transform.localScale.y, 1 / transform.localScale.z);
		transform.position = new Vector3(0, PillarHeight / 2f + BottomOfPillar.y, 0);
	}

	void Update() {
		UpdateWallPosition(radsOffsetForDimensionWall * Mathf.PI);
		UpdateWallRotation();
		UpdateWallSize();
		UpdateWallPosition(radsOffsetForDimensionWall * Mathf.PI);

		thisRenderer.enabled = (pillar == DimensionPillar.ActivePillar);
	}

	void UpdateWallPosition(float radsOffset) {
		Angle cameraAngle = pillar.dimensionShiftAngle - pillar.PillarAngleOfPlayerCamera();
		float colliderLength = transform.lossyScale.z;
		PolarCoordinate oppositePolar = new PolarCoordinate(colliderLength / 2f, Angle.Radians(cameraAngle.radians + radsOffset)) {
			y = transform.position.y
		};
		transform.position = oppositePolar.PolarToCartesian() + new Vector3(BottomOfPillar.x, 0, BottomOfPillar.z);
		transform.localPosition = new Vector3(transform.localPosition.x, PillarHeight / 2f, transform.localPosition.z);
	}

	void UpdateWallRotation() {
		transform.LookAt(new Vector3(BottomOfPillar.x, transform.position.y, BottomOfPillar.z));
	}

	void UpdateWallSize() {
		RaycastHit hitInfo;

		Vector3 origin = new Vector3(BottomOfPillar.x, transform.position.y, BottomOfPillar.z);
		Ray checkForWalls = new Ray(origin, transform.position - origin);
		Physics.SphereCast(checkForWalls, 0.2f, out hitInfo, EpitaphScreen.instance.playerCamera.farClipPlane, roomBoundsMask);
		//Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * maxColliderLength, Color.blue, 0.1f);

		Vector3 originalSize = transform.localScale;
		if (hitInfo.collider != null) {
			Vector2 hitInfoPoint = new Vector2(hitInfo.point.x, hitInfo.point.z);
			Vector2 originalPosition = new Vector2(BottomOfPillar.x, BottomOfPillar.z);
			float distanceToWall = (hitInfoPoint - originalPosition).magnitude;
			transform.localScale = new Vector3(originalSize.x, originalSize.y, distanceToWall);
		}
		else {
			transform.localScale = new Vector3(originalSize.x, originalSize.y, EpitaphScreen.instance.playerCamera.farClipPlane);
			//print("Nothing hit"); Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * mainCamera.farClipPlane, Color.blue, 10f);
		}
	}
}
