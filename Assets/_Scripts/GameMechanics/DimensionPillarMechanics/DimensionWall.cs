using System;
using PortalMechanics;
using UnityEngine;
using SuperspectiveUtils;

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

	Vector3 BottomOfPillar => pillarRenderer.bounds.center - pillar.transform.up * pillarRenderer.bounds.size.y / 2f;

	Vector3 PlayerCamWorldPos => SuperspectiveScreen.instance.playerCamera.transform.position;

	public float PillarHeight => TopOfPillar.y - BottomOfPillar.y;

	public const float RADS_OFFSET_FOR_DIMENSION_WALL = -0.1f;
	const float DIMENSION_WALL_WIDTH = 0.01f;
	LayerMask roomBoundsMask;

	void Awake() {
		pillar = GetComponentInParent<DimensionPillar>();
		pillarRenderer = pillar.GetComponent<Renderer>();
		thisRenderer = GetComponent<Renderer>();

		roomBoundsMask = 1 << LayerMask.NameToLayer("RoomBounds");

		InitializeWallTransform();
    }

	void OnEnable() {
		SubscribeToEvents();
	}

	private void OnDisable() {
		UnsubscribeFromEvents();
	}

	private void SubscribeToEvents() {
		VirtualPortalCamera.OnPreRenderPortal += OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal += OnPostRenderPortal;
	}

	private void UnsubscribeFromEvents() {
		VirtualPortalCamera.OnPreRenderPortal -= OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal -= OnPostRenderPortal;
	}

	void InitializeWallTransform() {
		//transform.SetParent(transform);
		transform.localScale = new Vector3(DIMENSION_WALL_WIDTH / transform.localScale.x, PillarHeight / transform.localScale.y, 1 / transform.localScale.z);
		transform.localPosition = new Vector3(0, 0, 0);
	}

	void Update() {
		UpdateStateForCamera(SuperspectiveScreen.instance.playerCamera);
	}

	void OnPreRenderPortal(Portal _) {
		// Offset the wall angle by more when rendering for portals,
		// since the DimensionWall can get cut off by the oblique projection matrix of the virtual portal camera
		UpdateStateForCamera(VirtualPortalCamera.instance.portalCamera, 4 * RADS_OFFSET_FOR_DIMENSION_WALL);
	}

	void OnPostRenderPortal(Portal _) {
		UpdateStateForCamera(Player.instance.PlayerCam);
	}

	public void UpdateStateForCamera(Camera cam, float offset = RADS_OFFSET_FOR_DIMENSION_WALL) {
		thisRenderer.enabled = pillar.enabled;
		if (!pillar.enabled) return;

		UpdateWallRotation(cam, offset);
		UpdateWallSize();
	}

	Angle AngleOfCamera(Camera cam) {
		Vector3 pillarToPoint = cam.transform.position - transform.position;
		PolarCoordinate polar = PolarCoordinate.CartesianToPolar(pillarToPoint);
		PolarCoordinate dimensionShiftPolar = PolarCoordinate.CartesianToPolar(pillar.DimensionShiftVector);
		return dimensionShiftPolar.angle - polar.angle;
	}

	void UpdateWallRotation(Camera cam, float offset) {
		transform.localEulerAngles = new Vector3(0, AngleOfCamera(cam).degrees - offset * Mathf.Rad2Deg, 0);
	}

	void UpdateWallSize() {
		RaycastHit hitInfo;

		Vector3 origin = new Vector3(BottomOfPillar.x, transform.position.y, BottomOfPillar.z);
		Ray checkForWalls = new Ray(origin, transform.position - origin);
		Physics.SphereCast(checkForWalls, 0.2f, out hitInfo, SuperspectiveScreen.instance.playerCamera.farClipPlane, roomBoundsMask);
		//Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * maxColliderLength, Color.blue, 0.1f);

		Vector3 originalSize = transform.localScale;
		if (hitInfo.collider != null) {
			Vector2 hitInfoPoint = new Vector2(hitInfo.point.x, hitInfo.point.z);
			Vector2 originalPosition = new Vector2(BottomOfPillar.x, BottomOfPillar.z);
			float distanceToWall = (hitInfoPoint - originalPosition).magnitude;
			transform.localScale = new Vector3(originalSize.x, originalSize.y, distanceToWall);
		}
		else {
			transform.localScale = new Vector3(originalSize.x, originalSize.y, SuperspectiveScreen.instance.playerCamera.farClipPlane);
			//print("Nothing hit"); Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * mainCamera.farClipPlane, Color.blue, 10f);
		}
	}
}
