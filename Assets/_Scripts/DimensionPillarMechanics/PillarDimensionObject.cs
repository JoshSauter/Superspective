using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using SuperspectiveUtils;
using System;
using LevelManagement;
using Saving;
using PillarReference = SerializableClasses.SerializableReference<DimensionPillar, DimensionPillar.DimensionPillarSave>;

// DimensionObjectBase.baseDimension is the lower dimension that this object exists in
// If this object goes across the 180° + dimensionShiftAngle (when the player is standing in the direction of pillar's transform.forward from pillar),
// it will act as a baseDimension+1 object when the pillar is in that dimension.
[RequireComponent(typeof(UniqueId))]
public class PillarDimensionObject : DimensionObject {
	public static readonly HashSet<PillarDimensionObject> allPillarDimensionObjects = new HashSet<PillarDimensionObject>();
	
	[SerializeField]
	[Range(0, 7)]
	int _dimension = 0;
	public int Dimension {
		get => _dimension;
		set => _dimension = value;
	}

	public enum Quadrant {
		Opposite,
		Left,
		SameSide,
		Right
	}
	[ReadOnly]
	public Quadrant camQuadrant;
	[ReadOnly]
	public Quadrant dimensionShiftQuadrant;
	[SerializeField]
	[ReadOnly]
	float minAngle;
	[SerializeField]
	[ReadOnly]
	float maxAngle;

	struct DimensionPillarPlanes {
		public Plane leftParallel;
		public Plane rightParallel;
		// Only used for drawing debug planes
		public float maxDistance;
	}

	// The active pillar that this object is setting its visibility state based off of
	public DimensionPillar activePillar;
	// Optional allow-list of pillars to react to, else will react to any active pillar
	public PillarReference[] pillars;
	// Key == pillar.ID
	readonly Dictionary<string, DimensionPillarPlanes> pillarPlanes = new Dictionary<string, DimensionPillarPlanes>();

	Vector3 minAngleVector, maxAngleVector;

	public bool collideWithPlayerWhileInvisible = false;
	public bool thisObjectMoves = false;
	public Rigidbody thisRigidbody;
	public Collider colliderBoundsOverride;

	Camera playerCam => SuperspectiveScreen.instance.playerCamera;

	// For context about rest of current state
	public Camera camSetUpFor;

	void OnEnable() {
		allPillarDimensionObjects.Add(this);
	}

	void OnDisable() {
		allPillarDimensionObjects.Remove(this);
	}

	protected override void Awake() {
		base.Awake();
		// PillarDimensionObjects continue to interact with other objects even while in other dimensions
		disableColliderWhileInvisible = false;
		if (thisObjectMoves) {
			if (thisRigidbody == null && GetComponent<Rigidbody>() == null) {
				DebugLogger tempDebug = new DebugLogger(gameObject, () => true);
				tempDebug.LogError($"{gameObject.name} moves, but no rigidbody could be found (Consider setting it manually)");
				thisObjectMoves = false;
			}
			else if (thisRigidbody == null) {
				thisRigidbody = GetComponent<Rigidbody>();
			}
		}
	}

	protected override void Init() {
		base.Init();
		renderers = GetAllSuperspectiveRenderers().ToArray();
		if (colliders == null || colliders.Length == 0) {
			colliders = GetAllColliders().ToArray();
		}

		HandlePillarChanged();
	}

	void HandlePillarChanged() {
		if (activePillar != null) {
			camSetUpFor = playerCam;
			camQuadrant = DetermineQuadrant(playerCam.transform.position);
			dimensionShiftQuadrant = DetermineQuadrant(activePillar.transform.position + activePillar.DimensionShiftVector);
			UpdateState(activePillar.curDimension, true);
		}
	}

	public override bool ShouldCollideWithPlayer() {
		if (collideWithPlayerWhileInvisible) return true;
		
		return effectiveVisibilityState != VisibilityState.invisible;
	}

	public override bool ShouldCollideWithNonDimensionObject() {
		return true;
	}

	public override bool ShouldCollideWith(DimensionObject other) {
		if (other is PillarDimensionObject otherPillarDimensionObj) {
			int testDimension = GetPillarDimensionWhereThisObjectWouldBeInVisibilityState(v => v == VisibilityState.visible || v == VisibilityState.partiallyVisible);
			if (testDimension == -1) {
				return false;
			}

			VisibilityState test1 = DetermineVisibilityState(camQuadrant, dimensionShiftQuadrant, testDimension);
			VisibilityState test2 = otherPillarDimensionObj.DetermineVisibilityState(otherPillarDimensionObj.camQuadrant, otherPillarDimensionObj.dimensionShiftQuadrant, testDimension);

			bool areOpposites = test1 == test2.Opposite();
			return !areOpposites;
		}
		else {
			VisibilityState effectiveVisibilityState = reverseVisibilityStates ? visibilityState.Opposite() : visibilityState;
			return effectiveVisibilityState != VisibilityState.invisible;
		}
	}

	void Update() {
		if (!hasInitialized) return;
		if (LevelManager.instance.IsCurrentlyLoadingScenes) return;
		bool thisObjectMoving = thisObjectMoves && (thisRigidbody == null || !thisRigidbody.IsSleeping());
		
		DimensionPillar DetermineActivePillar() {
			DimensionPillar FindNearestDimensionPillar() {
				// Only look for a new closest pillar if the object moved or we don't already have one
				if (!(thisObjectMoving || activePillar == null)) {
					return activePillar;
				}
				
				if (DimensionPillar.allPillars.Count > 0) {
					return DimensionPillar.allPillars.Values
						// Only consider pillars which are loaded and enabled
						.Where(pillarRef => pillarRef.GetOrNull()?.enabled ?? false)
						.Select(pillarRef => pillarRef.GetOrNull())
						.Where(pillar => pillar.IsInActiveScene())
						// Find the closest pillar
						.OrderBy(pillar => Vector3.Distance(pillar.transform.position, transform.position))
						.FirstOrDefault();
				}
				else {
					return null;
				}
			}
			
			if (pillars != null && pillars.Length > 0) {
				// Only consider pillars which are loaded and enabled
				return pillars.Where(reference => reference.GetOrNull()?.enabled ?? false)
					.Select(reference => reference.GetOrNull())
					.FirstOrDefault();
			}
			else {
				return FindNearestDimensionPillar();
			}
		}
		
		DimensionPillar prevPillar = activePillar;
		activePillar = DetermineActivePillar();
		if (activePillar == null) return;
		
		if (DEBUG) {
			Vector3 pillarPos = activePillar.transform.position;
			Debug.DrawRay(pillarPos, minAngleVector, Color.cyan);
			Debug.DrawRay(pillarPos, maxAngleVector, Color.blue);
		}

		if (thisObjectMoving || !pillarPlanes.ContainsKey(activePillar.ID)) {
			DeterminePlanes(activePillar);
		}

		// If the pillar changed this frame, handle this immediately
		if (prevPillar != activePillar) {
			HandlePillarChanged();
		}

		// Used to determine first frame where dimensionShiftQuadrant == Quadrant.Opposite (moving objects only)
		Quadrant nextDimensionShiftQuadrant = DetermineQuadrant(activePillar.transform.position + activePillar.DimensionShiftVector);
		if (thisObjectMoving) {
			//debug.Log($"CurDimensionShiftQuadrant: {dimensionShiftQuadrant}\nNextDimensionShiftQuadrant: {nextDimensionShiftQuadrant}");
			if (dimensionShiftQuadrant == Quadrant.Opposite && nextDimensionShiftQuadrant == Quadrant.Right) {
				Dimension = activePillar.NextDimension(Dimension);
			}
			else if (dimensionShiftQuadrant == Quadrant.Right && nextDimensionShiftQuadrant == Quadrant.Opposite) {
				Dimension = activePillar.PrevDimension(Dimension);
			}
		}

		dimensionShiftQuadrant = nextDimensionShiftQuadrant;
		
		UpdateStateForCamera(playerCam, activePillar.curDimension);
	}

	public void UpdateStateForCamera(Camera cam, int pillarDimension) {
		camSetUpFor = cam;
		camQuadrant = DetermineQuadrant(cam.transform.position);
		
		UpdateState(pillarDimension);
	}

	void UpdateState(int pillarDimension, bool forceUpdate = false) {
		VisibilityState nextState = DetermineVisibilityState(camQuadrant, dimensionShiftQuadrant, pillarDimension);

		if (nextState != visibilityState || forceUpdate) {
			SwitchVisibilityState(nextState, true);
		}
	}

	bool HasGoneToNextDimension(DimensionPillar pillar, Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant) {
		if (pillar == null) {
			return false;
		}
		if (playerQuadrant == dimensionShiftQuadrant) {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(pillar.DimensionShiftVector.normalized, pillar.Axis);
			Plane dimensionShiftPlane = new Plane(dimensionShiftPlaneNormalVector, pillar.transform.position);
			//debug.Log($"GetSide: {dimensionShiftPlane.GetSide(camPos)}\nPillar.curDimension: {pillar.curDimension}");
			return !dimensionShiftPlane.GetSide(camSetUpFor.transform.position);
		}
		else {
			return false;
		}
	}

	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this the player were in that dimension. This is not very performant but there's probably a smarter way to do this.
	// This is identical to the method below it except that it holds the object's dimension constant and tries different values of pillar.curDimension
	public int GetPillarDimensionWhereThisObjectWouldBeInVisibilityState(Predicate<VisibilityState> desiredVisibility) {
		if (activePillar == null) {
			return -1;
		}

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return activePillar.curDimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= activePillar.maxDimension; i++) {
			if (desiredVisibility(DetermineVisibilityState(camQuadrant, dimensionShiftQuadrant, i))) {
				return i;
			}
		}

		// No suitable dimension was found
		return -1;
	}

	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this object were in that dimension. This is not very performant but there's probably a smarter way to do this.
	public int GetDimensionWhereThisObjectWouldBeInVisibilityState(Predicate<VisibilityState> desiredVisibility) {
		if (activePillar == null) {
			return -1;
		}
		int tempDimension = _dimension;

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return Dimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= activePillar.maxDimension; i++) {
			_dimension = i;
			if (desiredVisibility(DetermineVisibilityState(camQuadrant, dimensionShiftQuadrant, activePillar.curDimension))) {
				_dimension = tempDimension;
				return i;
			}
		}
		_dimension = tempDimension;

		// No suitable dimension was found
		return -1;
	}

	public VisibilityState DetermineVisibilityState(Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant, int dimension) {
		if (activePillar == null) {
			return visibilityState;
		}
		if (HasGoneToNextDimension(activePillar, playerQuadrant, dimensionShiftQuadrant)) {
			dimension = activePillar.PrevDimension(dimension);
		}

		switch (playerQuadrant) {
			case Quadrant.Opposite:
				if (dimension == Dimension) {
					return VisibilityState.partiallyVisible;
				}
				else if (dimension == activePillar.NextDimension(Dimension)) {
					return VisibilityState.partiallyInvisible;
				}
				else {
					return VisibilityState.invisible;
				}
			case Quadrant.Left:
			case Quadrant.SameSide:
			case Quadrant.Right:
				if ((int)dimensionShiftQuadrant < (int)playerQuadrant) {
					if (dimension == activePillar.NextDimension(Dimension)) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
				else {
					if (dimension == Dimension) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
			default:
				throw new Exception($"Unhandled case: {this.camQuadrant}");
		}
	}

	Quadrant DetermineQuadrant(Vector3 position) {
		bool leftPlaneTest = pillarPlanes[activePillar.ID].leftParallel.GetSide(position);
		bool rightPlaneTest = pillarPlanes[activePillar.ID].rightParallel.GetSide(position);

		if (leftPlaneTest && rightPlaneTest) {
			return Quadrant.Left;
		}
		else if (leftPlaneTest && !rightPlaneTest) {
			return Quadrant.Opposite;
		}
		else if (!leftPlaneTest && rightPlaneTest) {
			return Quadrant.SameSide;
		}
		else { // if (!leftPlaneTest && !rightPlaneTest) {
			return Quadrant.Right;
		}
	}

	void DeterminePlanes(DimensionPillar pillar) {
		if (pillar == null) return;
		
		Vector3 projectedPillarCenter = Vector3.ProjectOnPlane(pillar.transform.position, pillar.Axis);
		Vector3 projectedVerticalPillarOffset = pillar.transform.position - projectedPillarCenter;

		List<Vector3> allCorners = renderers.SelectMany(CornersOfRenderer).ToList();
		Vector3 positionAvg = renderers.Aggregate(Vector3.zero, (acc, r) => acc + Vector3.ProjectOnPlane(r.GetRendererBounds().center, pillar.Axis));
		positionAvg /= renderers.Length;
		positionAvg += projectedVerticalPillarOffset;

		Debug.DrawRay(pillar.transform.position, positionAvg - pillar.transform.position, Color.magenta);

		bool flipDimensionShiftAngle = Vector3.Dot(pillar.DimensionShiftVector, positionAvg - pillar.transform.position) < 0;
		Vector3 dimensionShiftVector = flipDimensionShiftAngle ? Quaternion.AngleAxis(180, pillar.Axis) * pillar.DimensionShiftVector : pillar.DimensionShiftVector;

		float maxDistance = 0f;

		minAngle = float.MaxValue;
		maxAngle = float.MinValue;
		minAngleVector = Vector3.zero;
		maxAngleVector = Vector3.zero;
		foreach (Vector3 corner in allCorners) {
			Vector3 projectedCorner = Vector3.ProjectOnPlane(corner, pillar.Axis) + projectedVerticalPillarOffset;
			float signedAngle = Vector3.SignedAngle(dimensionShiftVector, projectedCorner - pillar.transform.position, pillar.Axis);
			if (signedAngle < minAngle) {
				minAngle = signedAngle;
				minAngleVector = (projectedCorner - pillar.transform.position);
			}
			else if (signedAngle > maxAngle) {
				maxAngle = signedAngle;
				maxAngleVector = (projectedCorner - pillar.transform.position);
			}

			float distance = (projectedCorner - pillar.transform.position).magnitude;
			if (distance > maxDistance) {
				maxDistance = distance;
			}

			if (DEBUG) {
				Debug.DrawRay(pillar.transform.position, projectedCorner - pillar.transform.position, Color.green);
			}
		}

		Vector3 minAngleNormalVector = Vector3.Cross(minAngleVector.normalized, pillar.Axis);
		Vector3 maxAngleNormalVector = Vector3.Cross(maxAngleVector.normalized, pillar.Axis);
		DimensionPillarPlanes thisPillarPlanes = new DimensionPillarPlanes {
			leftParallel = new Plane(minAngleNormalVector, pillar.transform.position),
			rightParallel = new Plane(maxAngleNormalVector, pillar.transform.position),
			maxDistance = maxDistance
		};

		pillarPlanes[pillar.ID] = thisPillarPlanes;

		// Don't spam the console with a moving object updating this info every frame
		if (DEBUG && !thisObjectMoves) {
			debug.Log($"Min: {minAngle}°, direction: {minAngleVector.normalized:F2}\nMax: {maxAngle}°, direction: {maxAngleVector.normalized:F2}");
		}
	}

	Vector3[] CornersOfRenderer(SuperspectiveRenderer renderer) {
		// Use Collider as bounds if override specified
		if (colliderBoundsOverride != null) {
			Bounds bounds = colliderBoundsOverride.bounds;
			return new Vector3[] {
				new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
				new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
				new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
				new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
				new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
				new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
				new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
				new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
			};
		}

		MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
		if (renderer.r is MeshRenderer && meshFilter != null) {
			Bounds localBounds = meshFilter.sharedMesh.bounds;
			return new Vector3[] {
				meshFilter.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.min.y, localBounds.min.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z)),
				meshFilter.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.max.y, localBounds.max.z))
			};
		}
		else {
			Bounds bounds = renderer.GetRendererBounds();
			return new Vector3[] {
				new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
				new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
				new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
				new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
				new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
				new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
				new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
				new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
			};
		}
	}

	void OnDrawGizmosSelected() {
		if (DEBUG && activePillar != null) {
			DimensionPillarPlanes thisPillarPlanes = pillarPlanes[activePillar.ID];
			DrawPlanes(activePillar.transform.position, thisPillarPlanes.leftParallel.normal, activePillar.dimensionWall.PillarHeight, 2f*thisPillarPlanes.maxDistance);
			DrawPlanes(activePillar.transform.position, thisPillarPlanes.rightParallel.normal, activePillar.dimensionWall.PillarHeight, 2f*thisPillarPlanes.maxDistance);
		}
	}

	void DrawPlanes(Vector3 point, Vector3 normal, float height = 16f, float width = 100f) {
		Quaternion rotation = Quaternion.LookRotation(normal);
		Matrix4x4 trs = Matrix4x4.TRS(point, rotation, Vector3.one);
		Gizmos.matrix = trs;
		Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
		float depth = 0.0001f;
		Gizmos.DrawCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = Color.white;
	}

	#region Saving

	[Serializable]
	public class PillarDimensionObjectSave : DimensionObjectSave {
		PillarReference[] pillars;
		int dimension;

		public PillarDimensionObjectSave(PillarDimensionObject dimensionObj) : base(dimensionObj) {
			this.pillars = dimensionObj.pillars;
			this.dimension = dimensionObj.Dimension;
		}

		public void LoadSave(PillarDimensionObject dimensionObj) {
			base.LoadSave(dimensionObj);
			dimensionObj.pillars = this.pillars;

			dimensionObj.Dimension = this.dimension;
		}
	}
	
	public override SerializableSaveObject GetSaveObject() {
		return new PillarDimensionObjectSave(this);
	}

	public override void RestoreStateFromSave(SerializableSaveObject savedObject) {
		PillarDimensionObjectSave save = savedObject as PillarDimensionObjectSave;

		save?.LoadSave(this);
	}
	#endregion
}