using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class ObscurePillar : MonoBehaviour {
	Camera mainCamera;
	Collider sweepingCollider;
	Collider sweepingColliderSmooth;
	MeshRenderer visibleMaskWall;
	Renderer pillarRenderer;
	LayerMask roomBounds;

	public float maxColliderLengthOverride = -1;
	public float sweepingColliderHeightOverride = -1;
	private float colliderHeight;
	private float minColliderWidth = 0.01f;
	private float maxColliderWidth = 20f;
	private float distanceFromPillarForMaxColliderWidth = 10;
	//private int sweepingColliderResolution = 12;

	public List<PartiallyVisibleObject> partiallyRenderedObjects = new List<PartiallyVisibleObject>();

	float pillarHeight {
		get {
			return pillarRenderer.bounds.size.y;
		}
	}
	Vector3 bottomOfPillar;
	Vector3 topOfPillar;

	// Use this for initialization
	void Start () {
		Time.fixedDeltaTime = 0.005f;
		mainCamera = Camera.main;
		pillarRenderer = GetComponent<Renderer>();
		roomBounds = 1 << LayerMask.NameToLayer("WallOnly") | 1 << LayerMask.NameToLayer("RoomBounds");
		colliderHeight = sweepingColliderHeightOverride > 0 ? sweepingColliderHeightOverride : pillarHeight;
		bottomOfPillar = pillarRenderer.bounds.center - Vector3.up * pillarRenderer.bounds.size.y / 2f;
		topOfPillar = pillarRenderer.bounds.center + Vector3.up * pillarRenderer.bounds.size.y / 2f;

		InitializeSweepingCollider();
		InitializeVisibleMaskWall();
	}

	private void OnEnable() {
		if (sweepingCollider != null) {
			sweepingCollider.gameObject.SetActive(true);
		}
		if (visibleMaskWall != null) {
			visibleMaskWall.gameObject.SetActive(true);
		}
	}

	private void OnDisable() {
		if (sweepingCollider != null) {
			sweepingCollider.gameObject.SetActive(false);
		}
		if (visibleMaskWall != null) {
			visibleMaskWall.gameObject.SetActive(false);
		}

		foreach (var pvo in partiallyRenderedObjects) {
			if (pvo.visibilityState == VisibilityState.partiallyVisible) {
				pvo.ResetVisibilityState();
			}
		}
		partiallyRenderedObjects.Clear();
	}

	// Update is called once per frame
	void Update () {
		UpdateSweepingCollider();
		UpdateVisibleMaskWall();
	}

	private void UpdateSweepingCollider() {
		UpdateWallPosition(sweepingCollider.transform, Mathf.PI);
		UpdateWallRotation(sweepingCollider.transform);
		UpdateWallSize(sweepingCollider.transform);
		UpdateWallPosition(sweepingCollider.transform, Mathf.PI);
		UpdateColliderWidth();
	}

	private void UpdateVisibleMaskWall() {
		UpdateWallPosition(visibleMaskWall.transform, -0.125f * Mathf.PI);
		UpdateWallRotation(visibleMaskWall.transform);
		UpdateWallSize(visibleMaskWall.transform);
		UpdateWallPosition(visibleMaskWall.transform, -0.125f * Mathf.PI);
	}

	private void UpdateWallPosition(Transform wallTransform, float radsOffset) {
		Vector3 pillarToCamera = mainCamera.transform.position - bottomOfPillar;
		PolarCoordinate cameraPolar = PolarCoordinate.CartesianToPolar(pillarToCamera);
		float colliderLength = wallTransform.localScale.z;
		PolarCoordinate oppositePolar = new PolarCoordinate(colliderLength/2f, cameraPolar.angle + radsOffset);
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
		Physics.SphereCast(checkForWalls, 0.2f, out hitInfo, mainCamera.farClipPlane, roomBounds);
		//Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * maxColliderLength, Color.blue, 0.1f);

		Vector3 originalSize = wallTransform.localScale;
		if (hitInfo.collider != null) {
			Vector2 hitInfoPoint = new Vector2(hitInfo.point.x, hitInfo.point.z);
			Vector2 originalPosition = new Vector2(bottomOfPillar.x, bottomOfPillar.z);
			float distanceToWall = (hitInfoPoint - originalPosition).magnitude;
			wallTransform.localScale = new Vector3(originalSize.x, originalSize.y, distanceToWall);
		}
		else {
			wallTransform.localScale = new Vector3(originalSize.x, originalSize.y, Mathf.Min(mainCamera.farClipPlane, (maxColliderLengthOverride < 0) ? Mathf.Infinity : maxColliderLengthOverride));
			//print("Nothing hit"); Debug.DrawRay(checkForWalls.origin, checkForWalls.direction * mainCamera.farClipPlane, Color.blue, 10f);
		}
	}

	private void UpdateColliderWidth() {
		Vector3 cameraPosition = new Vector3(mainCamera.transform.position.x, bottomOfPillar.y, mainCamera.transform.position.z);
		float distanceFromPlayerToPillar = Vector3.Distance(cameraPosition, bottomOfPillar);

		Vector3 curScale = sweepingCollider.transform.localScale;
		curScale.x = Mathf.Lerp(minColliderWidth, maxColliderWidth, 1 - distanceFromPlayerToPillar / distanceFromPillarForMaxColliderWidth);
		sweepingCollider.transform.localScale = curScale;
	}

	//private void UpdateSweepingColliderShape() {
	//	Vector3 bottomOfPillar = pillarRenderer.bounds.center - Vector3.up * pillarRenderer.bounds.size.y / 2f;
	//	Vector3 topOfPillar = pillarRenderer.bounds.center + Vector3.up * pillarRenderer.bounds.size.y / 2f;

	//	sweepingCollider.gameObject.GetComponent<MeshFilter>().mesh = UpdateMeshBounds();
		
	//	((MeshCollider)sweepingCollider).sharedMesh = sweepingCollider.gameObject.GetComponent<MeshFilter>().mesh;
	//}

	//private Mesh UpdateMeshBounds() {
	//	Vector3 bottomOfPillar = pillarRenderer.bounds.center - Vector3.up * pillarRenderer.bounds.size.y / 2f;
	//	Vector3 topOfPillar = pillarRenderer.bounds.center + Vector3.up * pillarRenderer.bounds.size.y / 2f;

	//	// Vertices
	//	Vector3[] vertices = new Vector3[2 * sweepingColliderResolution];
	//	for (int i = 0; i < sweepingColliderResolution; i++) {
	//		Vector3 pillarLerpPosition = Vector3.Lerp(bottomOfPillar, topOfPillar, (float)i / (sweepingColliderResolution - 1));
	//		Ray testRay = new Ray(pillarLerpPosition, (pillarLerpPosition - mainCamera.transform.position).normalized);
	//		RaycastHit hitInfo = new RaycastHit();

	//		Vector3 hitLocation = Physics.SphereCast(testRay, 0.2f, out hitInfo, mainCamera.farClipPlane, roomBounds) ? hitInfo.point : testRay.origin + testRay.direction * mainCamera.farClipPlane;
	//		//print(hitLocation);
	//		vertices[2*i] = pillarLerpPosition-bottomOfPillar;
	//		vertices[2*i + 1] = hitLocation-bottomOfPillar;
	//		//Debug.DrawRay(testRay.origin, (hitLocation-pillarLerpPosition)*40, Color.red);
	//		//Debug.DrawRay(testRay.origin, testRay.direction * 40, Color.cyan);
	//	}

	//	Mesh newMesh = new Mesh();

	//	// Triangles
	//	int[] tris = new int[3 * (vertices.Length - 2)];
	//	for (int i = 0; i < tris.Length; i++) {
	//		switch (i%6) {
	//			case 0:
	//			case 1:
	//			case 2:
	//				tris[i] = i / 3 + i % 6;
	//				break;
	//			case 3:
	//				tris[i] = tris[i - 2];
	//				break;
	//			case 4:
	//				tris[i] = tris[i - 1] + 2;
	//				break;
	//			case 5:
	//				tris[i] = tris[i - 1] - 1;
	//				break;
	//		}
	//	}
	//	newMesh.vertices = vertices;
	//	newMesh.triangles = tris;

	//	return newMesh;
	//}

	//private void InitializeSweepingColliderProceduralMesh() {
	//	sweepingCollider = new GameObject().AddComponent<MeshCollider>();
	//	MeshFilter mf = sweepingCollider.gameObject.AddComponent<MeshFilter>();
	//	mf.mesh = UpdateMeshBounds();
	//	((MeshCollider)sweepingCollider).convex = true;
	//	((MeshCollider)sweepingCollider).inflateMesh = mf.mesh;

	//	sweepingCollider.gameObject.AddComponent<MeshRenderer>();
	//	sweepingCollider.gameObject.AddComponent<SweepingVisibilityTrigger>().pillar = this;
	//	sweepingCollider.transform.SetParent(transform, worldPositionStays: false);
	//	sweepingCollider.name = "SweepingCollider";

	//}

	private void InitializeSweepingCollider() {
		sweepingCollider = new GameObject().AddComponent<BoxCollider>();
		sweepingCollider.gameObject.layer = LayerMask.NameToLayer("Invisible");
		sweepingCollider.gameObject.AddComponent<SweepingVisibilityTrigger>().pillar = this;
		InitializeWallTransform(sweepingCollider.transform);
		sweepingCollider.name = "SweepingCollider";
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
		wallTransform.localScale = new Vector3(minColliderWidth, colliderHeight, 1);
		wallTransform.position = new Vector3(0, pillarHeight / 2f + bottomOfPillar.y, 0);
	}
}
