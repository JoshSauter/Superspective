using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using UnityEngine.Rendering;

public class DimensionWall : MonoBehaviour {
	Renderer thisRenderer;
	DimensionPillar pillar;
	Renderer pillarRenderer;
	Vector3 topOfPillar { get { return pillarRenderer.bounds.center + Vector3.up * pillarRenderer.bounds.size.y / 2f; } }
	Vector3 bottomOfPillar { get { return pillarRenderer.bounds.center - Vector3.up * pillarRenderer.bounds.size.y / 2f; } }

	public float pillarHeight {
		get {
			return topOfPillar.y - bottomOfPillar.y;
		}
	}

	readonly float radsOffsetForDimensionWall = -0.1f;
	readonly float dimensionWallWidth = 0.01f;
	LayerMask roomBoundsMask;

	void Awake() {
		pillar = GetComponentInParent<DimensionPillar>();
		pillarRenderer = pillar.GetComponent<Renderer>();
		thisRenderer = GetComponent<Renderer>();

		roomBoundsMask = 1 << LayerMask.NameToLayer("WallOnly") | 1 << LayerMask.NameToLayer("RoomBounds");

		InitializeWallTransform();
    }

	private void InitializeWallTransform() {
		transform.SetParent(transform);
		transform.localScale = new Vector3(dimensionWallWidth / transform.localScale.x, pillarHeight / transform.localScale.y, 1 / transform.localScale.z);
		transform.position = new Vector3(0, pillarHeight / 2f + bottomOfPillar.y, 0);
	}

	void Update() {
		UpdateWallPosition(radsOffsetForDimensionWall * Mathf.PI);
		UpdateWallRotation();
		UpdateWallSize();
		UpdateWallPosition(radsOffsetForDimensionWall * Mathf.PI);

		thisRenderer.enabled = (pillar == DimensionPillar.activePillar);
	}

	private void UpdateWallPosition(float radsOffset) {
		Angle cameraAngle = pillar.dimensionShiftAngle - pillar.PillarAngleOfPlayerCamera();
		float colliderLength = transform.lossyScale.z;
		PolarCoordinate oppositePolar = new PolarCoordinate(colliderLength / 2f, Angle.Radians(cameraAngle.radians + radsOffset)) {
			y = transform.position.y
		};
		transform.position = oppositePolar.PolarToCartesian() + new Vector3(bottomOfPillar.x, 0, bottomOfPillar.z);
		transform.localPosition = new Vector3(transform.localPosition.x, pillarHeight / 2f, transform.localPosition.z);
	}

	private void UpdateWallRotation() {
		transform.LookAt(new Vector3(bottomOfPillar.x, transform.position.y, bottomOfPillar.z));
	}

	private void UpdateWallSize() {
		RaycastHit hitInfo;

		Vector3 origin = new Vector3(bottomOfPillar.x, transform.position.y, bottomOfPillar.z);
		Ray checkForWalls = new Ray(origin, transform.position - origin);
		Physics.SphereCast(checkForWalls, 0.2f, out hitInfo, EpitaphScreen.instance.playerCamera.farClipPlane, roomBoundsMask);
		//Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * maxColliderLength, Color.blue, 0.1f);

		Vector3 originalSize = transform.localScale;
		if (hitInfo.collider != null) {
			Vector2 hitInfoPoint = new Vector2(hitInfo.point.x, hitInfo.point.z);
			Vector2 originalPosition = new Vector2(bottomOfPillar.x, bottomOfPillar.z);
			float distanceToWall = (hitInfoPoint - originalPosition).magnitude;
			transform.localScale = new Vector3(originalSize.x, originalSize.y, distanceToWall);
		}
		else {
			transform.localScale = new Vector3(originalSize.x, originalSize.y, EpitaphScreen.instance.playerCamera.farClipPlane);
			//print("Nothing hit"); Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * mainCamera.farClipPlane, Color.blue, 10f);
		}
	}
}
