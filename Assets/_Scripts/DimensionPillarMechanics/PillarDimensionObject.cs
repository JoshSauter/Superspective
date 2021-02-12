using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;
using System;
using Saving;
using SerializableClasses;

// DimensionObjectBase.baseDimension is the lower dimension that this object exists in
// If this object goes across the 180° + dimensionShiftAngle (when the player is standing in the direction of pillar's transform.forward from pillar),
// it will act as a baseDimension+1 object when the pillar is in that dimension.
[RequireComponent(typeof(UniqueId))]
public class PillarDimensionObject : DimensionObject {
	[SerializeField]
	[Range(0, 7)]
	int _dimension = 0;
	public int Dimension {
		get { return _dimension; }
		set {
			if (value != Dimension) {
				OnBaseDimensionChange?.Invoke();
			}
			_dimension = value;
		}
	}

	public enum Quadrant {
		Opposite,
		Left,
		SameSide,
		Right
	}
	[ReadOnly]
	public Quadrant playerQuadrant;
	[ReadOnly]
	public Quadrant dimensionShiftQuadrant;
	[SerializeField]
	[ReadOnly]
	float minAngle;
	[SerializeField]
	[ReadOnly]
	float maxAngle;

	// The active pillar that this object is setting its visibility state based off of
	public DimensionPillar pillar => pillarsSet.Contains(DimensionPillar.ActivePillar) || pillars.Length == 0 ? DimensionPillar.ActivePillar : null;
	public DimensionPillar[] pillars;
	HashSet<DimensionPillar> pillarsSet = new HashSet<DimensionPillar>();
	Dictionary<DimensionPillar, Plane> leftParallels = new Dictionary<DimensionPillar, Plane>();
	Dictionary<DimensionPillar, Plane> rightParallels = new Dictionary<DimensionPillar, Plane>();
	// Only used for drawing debug planes
	Dictionary<DimensionPillar, float> maxDistances = new Dictionary<DimensionPillar, float>();

	Vector3 minAngleVector, maxAngleVector;

	public bool thisObjectMoves = false;
	public Rigidbody thisRigidbody;
	public Collider colliderBoundsOverride;

	Vector3 camPos => EpitaphScreen.instance.playerCamera.transform.position;

	public delegate void DimensionObjectAction();
	public event DimensionObjectAction OnBaseDimensionChange;

	DimensionPillar.ActivePillarChangedEvent activePillarChangedHandler;

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
		pillarsSet = new HashSet<DimensionPillar>(pillars);
		renderers = GetAllEpitaphRenderers().ToArray();

		foreach (var p in pillars) {
			DeterminePlanes(p);
		}

		activePillarChangedHandler = (prev) => HandleNewPillar();
		DimensionPillar.OnActivePillarChanged += activePillarChangedHandler;

		HandleNewPillar();
	}

	void HandleNewPillar() {
		if (pillar != null) {
			DeterminePlanes(pillar);

			playerQuadrant = DetermineQuadrant(camPos);
			dimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.DimensionShiftVector);
			UpdateState(true);
		}
	}

	protected override void OnDestroy() {
		base.OnDestroy();
		DimensionPillar.OnActivePillarChanged -= activePillarChangedHandler;
	}

	void GetCollidersRecursivelyHelper(Transform parent, ref List<Collider> collidersSoFar) {
		Collider thisCollider = parent.GetComponent<Collider>();
		if (thisCollider != null) {
			collidersSoFar.Add(thisCollider);
		}

		if (parent.childCount > 0) {
			foreach (Transform child in parent) {
				GetCollidersRecursivelyHelper(child, ref collidersSoFar);
			}
		}
	}

	void FixedUpdate() {
		if (pillar == null) return;
		if (DEBUG) {
			Debug.DrawRay(pillar.transform.position, minAngleVector, Color.cyan);
			Debug.DrawRay(pillar.transform.position, maxAngleVector, Color.blue);
		}

		bool thisObjectMoving = thisObjectMoves && (thisRigidbody == null || !thisRigidbody.IsSleeping());
		if (thisObjectMoving) {
			DeterminePlanes(pillar);
		}

		// Used to determine first frame where dimensionShiftQuadrant == Quadrant.Opposite (moving objects only)
		Quadrant nextDimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.DimensionShiftVector);
		if (thisObjectMoving) {
			if (dimensionShiftQuadrant == Quadrant.Opposite && nextDimensionShiftQuadrant == Quadrant.Right) {
				Dimension = pillar.NextDimension(Dimension);
			}
			else if (dimensionShiftQuadrant == Quadrant.Right && nextDimensionShiftQuadrant == Quadrant.Opposite) {
				Dimension = pillar.PrevDimension(Dimension);
			}
		}

		playerQuadrant = DetermineQuadrant(camPos);
		dimensionShiftQuadrant = nextDimensionShiftQuadrant;

		UpdateState();
	}

	void UpdateState(bool forceUpdate = false) {
		VisibilityState nextState = DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, pillar.curDimension);

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
			return !dimensionShiftPlane.GetSide(camPos);
		}
		else {
			return false;
		}
	}

	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this the player were in that dimension. This is not very performant but there's probably a smarter way to do this.
	// This is identical to the method below it except that it holds the object's dimension constant and tries different values of pillar.curDimension
	public int GetPillarDimensionWhereThisObjectWouldBeInVisibilityState(Predicate<VisibilityState> desiredVisibility) {
		if (pillar == null) {
			return -1;
		}

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return pillar.curDimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= pillar.maxDimension; i++) {
			if (desiredVisibility(DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, i))) {
				return i;
			}
		}

		// No suitable dimension was found
		return -1;
	}

	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this object were in that dimension. This is not very performant but there's probably a smarter way to do this.
	public int GetDimensionWhereThisObjectWouldBeInVisibilityState(Predicate<VisibilityState> desiredVisibility) {
		int tempDimension = _dimension;

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return Dimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= pillar.maxDimension; i++) {
			_dimension = i;
			if (desiredVisibility(DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, pillar.curDimension))) {
				_dimension = tempDimension;
				return i;
			}
		}
		_dimension = tempDimension;

		// No suitable dimension was found
		return -1;
	}

	public VisibilityState DetermineVisibilityState(Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant, int dimension) {
		if (pillar == null) {
			return visibilityState;
		}
		if (HasGoneToNextDimension(pillar, playerQuadrant, dimensionShiftQuadrant)) {
			dimension = pillar.PrevDimension(dimension);
		}

		switch (playerQuadrant) {
			case Quadrant.Opposite:
				if (dimension == this.Dimension) {
					return VisibilityState.partiallyVisible;
				}
				else if (dimension == pillar.NextDimension(this.Dimension)) {
					return VisibilityState.partiallyInvisible;
				}
				else {
					return VisibilityState.invisible;
				}
			case Quadrant.Left:
			case Quadrant.SameSide:
			case Quadrant.Right:
				if ((int)dimensionShiftQuadrant < (int)playerQuadrant) {
					if (dimension == pillar.NextDimension(this.Dimension)) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
				else {
					if (dimension == this.Dimension) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
			default:
				throw new Exception($"Unhandled case: {this.playerQuadrant}");
		}
	}

	Quadrant DetermineQuadrant(Vector3 position) {
		bool leftPlaneTest = leftParallels[pillar].GetSide(position);
		bool rightPlaneTest = rightParallels[pillar].GetSide(position);

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

		List<Vector3> allCorners = renderers.SelectMany(r => CornersOfRenderer(r)).ToList();
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
		leftParallels[pillar] = new Plane(minAngleNormalVector, pillar.transform.position);
		rightParallels[pillar] = new Plane(maxAngleNormalVector, pillar.transform.position);

		maxDistances[pillar] = maxDistance;

		// Don't spam the console with a moving object updating this info every frame
		if (DEBUG && !thisObjectMoves) {
			debug.Log($"Min: {minAngle}°, direction: {minAngleVector.normalized:F2}\nMax: {maxAngle}°, direction: {maxAngleVector.normalized:F2}");
		}
	}

	Vector3[] CornersOfRenderer(EpitaphRenderer renderer) {
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
		if (DEBUG && pillar != null) {
			DrawPlanes(pillar.transform.position, leftParallels[pillar].normal, pillar.dimensionWall.PillarHeight, 2f*maxDistances[pillar]);
			DrawPlanes(pillar.transform.position, rightParallels[pillar].normal, pillar.dimensionWall.PillarHeight, 2f*maxDistances[pillar]);
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
	public override string ID => $"PillarDimensionObject_{id.uniqueId}";

	[Serializable]
	public class PillarDimensionObjectSave : DimensionObjectSave {
		SerializableReference<DimensionPillar>[] pillars;
		int dimension;

		public PillarDimensionObjectSave(PillarDimensionObject dimensionObj) : base(dimensionObj) {
			this.pillars = dimensionObj.pillars.Select<DimensionPillar, SerializableReference<DimensionPillar>>(p => p).ToArray();
			this.dimension = dimensionObj.Dimension;
		}

		public void LoadSave(PillarDimensionObject dimensionObj) {
			base.LoadSave(dimensionObj);
			dimensionObj.pillars = this.pillars.Select<SerializableReference<DimensionPillar>, DimensionPillar>(p => p).ToArray();

			dimensionObj.Dimension = this.dimension;
		}
	}
	
	public override object GetSaveObject() {
		return new PillarDimensionObjectSave(this);
	}

	public override void RestoreStateFromSave(object savedObject) {
		PillarDimensionObjectSave save = savedObject as PillarDimensionObjectSave;

		save?.LoadSave(this);
	}
	#endregion
}