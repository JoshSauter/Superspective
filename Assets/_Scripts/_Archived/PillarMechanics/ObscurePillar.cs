using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public enum MovementDirection {
	clockwise,
	counterclockwise
}

public class ObscurePillar : MonoBehaviour {
	public bool DEBUG = false;

	public static Dictionary<string, ObscurePillar> pillars = new Dictionary<string, ObscurePillar>();
	public string pillarKey;

	[Tooltip("Setting this in inspector will NOT trigger events based off of active pillar changing")]
	public bool isActivePillar = false;
	// Can be null if there is no active pillar
	private static ObscurePillar _activePillar;
	public static ObscurePillar activePillar {
		get { return _activePillar; }
		set {
			ObscurePillar prevActive = _activePillar;
			_activePillar = value;
			if (OnActivePillarChanged != null) {
				OnActivePillarChanged(prevActive);
			}
		}
	}

	public float radsOffsetForVisibilityMask = 0.125f;

	public delegate void ActivePillarChangedEvent(ObscurePillar previousActivePillar);
	public static event ActivePillarChangedEvent OnActivePillarChanged;
	public delegate void PlayerMoveAroundPillarEvent(Angle previousAngle, Angle newAngle);
	public static event PlayerMoveAroundPillarEvent OnPlayerMoveAroundPillar;

	PartiallyVisibleObject thisPartiallyVisibleObject;

	Angle previousCameraAngleRelativeToPillar;

	Camera mainCamera;
	MeshRenderer visibleMaskWall;
	Renderer pillarRenderer;
	LayerMask roomBoundsMask;

	float visibilityMaskWidth = 0.01f;
	float pillarHeight {
		get {
			return pillarRenderer.bounds.size.y;
		}
	}
	Vector3 bottomOfPillar;

	void InitializeDictEntry() {
		if (pillarKey == "") {
			pillarKey = gameObject.scene.name + " " + gameObject.name;
		}
		if (!pillars.ContainsKey(pillarKey)) {
			pillars[pillarKey] = this;
		}
	}

	// Use this for initialization
	void Start () {
		mainCamera = EpitaphScreen.instance.playerCamera;
		pillarRenderer = GetComponent<Renderer>();
		roomBoundsMask = 1 << LayerMask.NameToLayer("WallOnly") | 1 << LayerMask.NameToLayer("RoomBounds");
		bottomOfPillar = pillarRenderer.bounds.center - Vector3.up * pillarRenderer.bounds.size.y / 2f;
		
		InitializeVisibleMaskWall();
		previousCameraAngleRelativeToPillar = Angle.Radians(-1);
		if (this == ObscurePillar.activePillar) {
			EnablePillar();
		}
		else {
			DisablePillar();
		}


	}

	private void HandlePillarVisibilityChange(VisibilityState_old newState) {
		if (newState == VisibilityState_old.visible) {
			ObscurePillar.activePillar = this;
		}
	}

	private void HandleActivePillarChanged(ObscurePillar previousPillar) {
		if (activePillar == this) {
			EnablePillar();
		}
		else {
			DisablePillar();
		}
	}

	private void OnEnable() {
		InitializeDictEntry();

		OnActivePillarChanged += HandleActivePillarChanged;

		thisPartiallyVisibleObject = GetComponent<PartiallyVisibleObject>();
		if (thisPartiallyVisibleObject != null) {
			thisPartiallyVisibleObject.OnVisibilityStateChange += HandlePillarVisibilityChange;
		}
	}

	private void OnDisable() {
		OnActivePillarChanged -= HandleActivePillarChanged;
		if (thisPartiallyVisibleObject != null) {
			thisPartiallyVisibleObject.OnVisibilityStateChange += HandlePillarVisibilityChange;
		}
	}

	private void EnablePillar() {
		isActivePillar = true;
		if (visibleMaskWall != null) {
			visibleMaskWall.gameObject.SetActive(true);
		}
	}

	private void DisablePillar() {
		isActivePillar = false;
		if (visibleMaskWall != null) {
			visibleMaskWall.gameObject.SetActive(false);
		}
	}

	// Update is called once per frame
	void Update () {
		UpdateVisibleMaskWall();
		UpdateCameraPosition();


        if (DEBUG && Input.GetKeyDown("r")) {
            ReportPlayerAngle();
        }
    }

	private void UpdateCameraPosition() {
		Angle newCameraAngleRelativeToPillar = CameraToPillar().angle;
		// Don't trigger subscribers of OnPlayerMoveAroundPillar if there was no previous position
		if (previousCameraAngleRelativeToPillar.radians < 0) {
			previousCameraAngleRelativeToPillar = newCameraAngleRelativeToPillar;
		}
		else if (OnPlayerMoveAroundPillar != null && activePillar == this) {
			OnPlayerMoveAroundPillar(previousCameraAngleRelativeToPillar, newCameraAngleRelativeToPillar);
		}
		previousCameraAngleRelativeToPillar = newCameraAngleRelativeToPillar;
	}

	private void UpdateVisibleMaskWall() {
		UpdateWallPosition(visibleMaskWall.transform, -radsOffsetForVisibilityMask * Mathf.PI);
		UpdateWallRotation(visibleMaskWall.transform);
		UpdateWallSize(visibleMaskWall.transform);
		UpdateWallPosition(visibleMaskWall.transform, -radsOffsetForVisibilityMask * Mathf.PI);
	}

	private PolarCoordinate CameraToPillar() {
		Vector3 pillarToCamera = mainCamera.transform.position - bottomOfPillar;
		return PolarCoordinate.CartesianToPolar(pillarToCamera);
	}

	private void UpdateWallPosition(Transform wallTransform, float radsOffset) {
		PolarCoordinate cameraPolar = CameraToPillar();
		float colliderLength = wallTransform.lossyScale.z;
		PolarCoordinate oppositePolar = new PolarCoordinate(colliderLength/2f, Angle.Radians(cameraPolar.angle.radians + radsOffset));
		oppositePolar.y = wallTransform.position.y;
		wallTransform.position = oppositePolar.PolarToCartesian() + new Vector3(bottomOfPillar.x, 0, bottomOfPillar.z);
	}

	private void UpdateWallRotation(Transform wallTransform) {
		wallTransform.LookAt(new Vector3(bottomOfPillar.x, wallTransform.position.y, bottomOfPillar.z));
	}

	private void UpdateWallSize(Transform wallTransform) {
		RaycastHit hitInfo;

		Vector3 origin = new Vector3(bottomOfPillar.x, wallTransform.position.y, bottomOfPillar.z);
		Ray checkForWalls = new Ray(origin, wallTransform.position - origin);
		Physics.SphereCast(checkForWalls, 0.2f, out hitInfo, mainCamera.farClipPlane, roomBoundsMask);
		//Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * maxColliderLength, Color.blue, 0.1f);

		Vector3 originalSize = wallTransform.localScale;
		if (hitInfo.collider != null) {
			Vector2 hitInfoPoint = new Vector2(hitInfo.point.x, hitInfo.point.z);
			Vector2 originalPosition = new Vector2(bottomOfPillar.x, bottomOfPillar.z);
			float distanceToWall = (hitInfoPoint - originalPosition).magnitude;
			wallTransform.localScale = new Vector3(originalSize.x, originalSize.y, distanceToWall / transform.localScale.z);
		}
		else {
			wallTransform.localScale = new Vector3(originalSize.x, originalSize.y,mainCamera.farClipPlane);
			//print("Nothing hit"); Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * mainCamera.farClipPlane, Color.blue, 10f);
		}
	}
	
	private void InitializeVisibleMaskWall() {
		visibleMaskWall = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
		InitializeWallTransform(visibleMaskWall.transform);
		visibleMaskWall.name = "VisibleMaskWall";

		visibleMaskWall.gameObject.layer = LayerMask.NameToLayer("VisibilityMask");
		visibleMaskWall.GetComponent<Collider>().enabled = false;
		visibleMaskWall.material = Resources.Load<Material>("Materials/Unlit/ObscureShader/OSRed");
	}

	private void InitializeWallTransform(Transform wallTransform) {
		wallTransform.SetParent(transform);
		wallTransform.localScale = new Vector3(visibilityMaskWidth / transform.localScale.x, pillarHeight / transform.localScale.y, 1 / transform.localScale.z);
		wallTransform.position = new Vector3(0, pillarHeight / 2f + bottomOfPillar.y, 0);
	}

    // Debug
    [ContextMenu("Report Player's current angle relative to this Pillar")]
    private void ReportPlayerAngle() {
        Debug.Log(CameraToPillar().angle);
    }
}
