using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using SuperspectiveUtils;
using System;
using PortalMechanics;
using Saving;
using SuperspectiveAttributes;
using PillarReference = SerializableClasses.SuperspectiveReference<DimensionPillar, DimensionPillar.DimensionPillarSave>;

// DimensionObjectBase.baseDimension is the lower dimension that this object exists in
// If this object goes across the 180° + dimensionShiftAngle (when the player is standing in the direction of pillar's transform.forward from pillar),
// it will act as a baseDimension+1 object when the pillar is in that dimension.
[RequireComponent(typeof(UniqueId))]
public class PillarDimensionObject : DimensionObject {
	public static readonly HashSet<PillarDimensionObject> allPillarDimensionObjects = new HashSet<PillarDimensionObject>();

	[DoNotSave, SerializeField]
	[Range(0, 7)]
	int _dimension = 0;
	public int Dimension {
		get => _dimension;
		set {
			if (_dimension != value) {
				debug.Log($"Dimension changing from {_dimension} to {value}");
			}
			int diff = value - _dimension;
			_dimension = value;
			
			// Instantly update the object bounds to reflect the new dimension so we don't have a frame of old bounds causing the wrong visibility state to be used
			if (activePillar != null) {
				DimensionPillarPlanes prev = pillarPlanes[activePillar.ID];
				pillarPlanes[activePillar.ID] = new DimensionPillarPlanes() {
					leftParallel = prev.leftParallel,
					rightParallel = prev.rightParallel,
					maxDistance = prev.maxDistance,
					objectBounds = new DimensionRange() {
						min = activePillar.WrappedValue(prev.objectBounds.min + diff),
						max = activePillar.WrappedValue(prev.objectBounds.max + diff)
					},
					invisibleRange = new DimensionRange() {
						min = activePillar.WrappedValue(prev.invisibleRange.min + diff),
						max = activePillar.WrappedValue(prev.invisibleRange.max + diff)
					},
					partiallyInvisibleRange = new DimensionRange() {
						min = activePillar.WrappedValue(prev.partiallyInvisibleRange.min + diff),
						max = activePillar.WrappedValue(prev.partiallyInvisibleRange.max + diff)
					},
					partiallyVisibleRange = new DimensionRange() {
						min = activePillar.WrappedValue(prev.partiallyVisibleRange.min + diff),
						max = activePillar.WrappedValue(prev.partiallyVisibleRange.max + diff)
					},
					visibleRange = new DimensionRange() {
						min = activePillar.WrappedValue(prev.visibleRange.min + diff),
						max = activePillar.WrappedValue(prev.visibleRange.max + diff)
					}
				};
			}
		}
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
	private float minAngle;
	[SerializeField]
	[ReadOnly]
	private float maxAngle;

	[SerializeField]
	[ReadOnly]
	private DimensionRange objectBounds;
	[SerializeField]
	[ReadOnly]
	private DimensionRange visibleRange;
	[SerializeField]
	[ReadOnly]
	private DimensionRange partiallyVisibleRange;
	[SerializeField]
	[ReadOnly]
	private DimensionRange partiallyInvisibleRange;
	[SerializeField]
	[ReadOnly]
	private DimensionRange invisibleRange;

	[Serializable]
	public struct DimensionRange {
		public float min;
		public float max;
		
		public bool WrapsAround => min > max;
		
		/// <summary>
		/// Returns true if the given value is within the range, taking into account wrap-around values
		/// </summary>
		/// <param name="value">Value to test if it is in the range or not</param>
		/// <returns>True if the value is within the range, taking into account wrap-around values, false otherwise</returns>
		public bool Contains(float value) {
			if (WrapsAround) {
				return value >= min || value <= max;
			}
			else {
				return value >= min && value <= max;
			}
		}
		
		/// <summary>
		/// Returns true if the two sets of DimensionRanges overlap at all
		/// </summary>
		/// <param name="other">Other DimensionRange to compare overlap with</param>
		/// <returns>True if the ranges have any overlap, taking into account wrap-around values</returns>
		public bool HasOverlapWith(DimensionRange other) {
			switch (WrapsAround, other.WrapsAround) {
				case (true, true):
					// Any wrap-around intervals always overlap
					return true;
				case (true, false):
					return min <= other.max || max >= other.min;
				case (false, true):
					return other.min <= max || other.max >= min;
				case (false, false):
					return min <= other.max && max >= other.min;
			}
		}
	}

	[Serializable]
	struct DimensionPillarPlanes {
		public Plane leftParallel;
		public Plane rightParallel;

		// Tells us how many trips around the pillar (starting from dimension 0) this object exists in
		// e.g. if this object exists a little bit in dimension 1, it might be minDimension = 1.2, maxDimension = 1.8
		public DimensionRange objectBounds;

		public DimensionRange visibleRange;
		public DimensionRange partiallyVisibleRange;
		public DimensionRange partiallyInvisibleRange;
		public DimensionRange invisibleRange;
		
		// Only used for drawing debug planes
		public float maxDistance;
	}

	// The active pillar that this object is setting its visibility state based off of
	public DimensionPillar activePillar;
	// Optional allow-list of pillars to react to, else will react to any active pillar
	public PillarReference[] pillars;
	// Key == pillar.ID
	readonly Dictionary<string, DimensionPillarPlanes> pillarPlanes = new Dictionary<string, DimensionPillarPlanes>();
	[SerializeField]
	private DimensionPillarPlanes lastPillarPlanes;

	Vector3 minAngleVector, maxAngleVector;

	public bool thisObjectMoves = false;
	bool IsMoving => thisObjectMoves && (thisRigidbody == null || !thisRigidbody.IsSleeping());
	public Rigidbody thisRigidbody;
	public Collider colliderBoundsOverride;

	Vector3 PlayerCamPos => Player.instance.AdjustedCamPos;

	// For context about rest of current state
	public Cam camSetUpFor;

	protected override void OnEnable() {
		base.OnEnable();
		allPillarDimensionObjects.Add(this);

		VirtualPortalCamera.OnPreRenderPortal += OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal += OnPostRenderPortal;
	}

	protected override void OnDisable() {
		base.OnDisable();
		allPillarDimensionObjects.Remove(this);
		
		VirtualPortalCamera.OnPreRenderPortal -= OnPreRenderPortal;
		VirtualPortalCamera.OnPostRenderPortal -= OnPostRenderPortal;
	}

	protected override void Awake() {
		base.Awake();
		
		// PillarDimensionObjects continue to interact with other objects even while in other dimensions
		disableColliderWhileInvisible = false;
		if (thisObjectMoves) {
			if (thisRigidbody == null && GetComponent<Rigidbody>() == null) {
				debug.LogError($"{gameObject.name} moves, but no rigidbody could be found (Consider setting it manually)", true);
				thisObjectMoves = false;
			}
			else if (thisRigidbody == null) {
				thisRigidbody = GetComponent<Rigidbody>();
			}
		}
	}

	protected override void Init() {
		base.Init();

		HandlePillarChanged();
	}

	void HandlePillarChanged() {
		if (activePillar != null) {
			camSetUpFor = Cam.Player;
			DetermineQuadrantForPlayerCam();
			dimensionShiftQuadrant = DetermineQuadrant(activePillar.transform.position + activePillar.DimensionShiftVector);
			UpdateState(activePillar, true);
		}
	}

	public override bool ShouldCollideWithPlayer() {
		return EffectiveVisibilityState != VisibilityState.Invisible;
	}

	public override bool ShouldCollideWithNonDimensionObjects() {
		return true;
		return EffectiveVisibilityState != VisibilityState.Invisible;
	}

	public override bool ShouldCollideWithDimensionObject(DimensionObject other) {
		if (other is PillarDimensionObject otherPillarDimensionObj) {
			if (activePillar == null) {
				activePillar = DetermineActivePillar();
				if (activePillar == null) {
					return true;
				}
			}

			if (!pillarPlanes.ContainsKey(activePillar.ID)) {
				DeterminePlanes(activePillar);
			}

			if (!otherPillarDimensionObj.pillarPlanes.ContainsKey(activePillar.ID)) {
				otherPillarDimensionObj.DeterminePlanes(activePillar);
			}
			
			bool shouldCollide = pillarPlanes[activePillar.ID].objectBounds.HasOverlapWith(otherPillarDimensionObj.pillarPlanes[activePillar.ID].objectBounds);
			return shouldCollide;
		}
		else {
			return other.ShouldCollideWithDimensionObject(this);
		}
	}
	
	DimensionPillar DetermineActivePillar() {
		DimensionPillar FindNearestDimensionPillar() {
			// Only look for a new closest pillar if the object moved or we don't already have one
			if (!(IsMoving || activePillar == null)) {
				return activePillar;
			}
				
			if (DimensionPillar.allPillars.Count > 0) {
				return DimensionPillar.allPillars.Values
					// Only consider pillars which are loaded and enabled
					.Where(pillarRef => pillarRef.GetOrNull()?.enabled ?? false)
					.Select(pillarRef => pillarRef.GetOrNull())
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

	void Update() {
		if (!hasInitialized) return;
		if (GameManager.instance.IsCurrentlyLoading) return;
		
		DimensionPillar prevPillar = activePillar;
		activePillar = DetermineActivePillar();
		if (activePillar == null) return;
		
		if (DEBUG) {
			Vector3 pillarPos = activePillar.transform.position;
			Debug.DrawRay(pillarPos, minAngleVector, Color.cyan);
			Debug.DrawRay(pillarPos, maxAngleVector, Color.blue);
		}
		
		// Recalculate planes if the object is moving or the pillar changed
		if (IsMoving || !pillarPlanes.ContainsKey(activePillar.ID)) {
			DeterminePlanes(activePillar);
		}
		
		// Used to determine first frame where dimensionShiftQuadrant == Quadrant.SameSide (moving objects only)
		// Represents the moment when the object has fully moved into the next dimension (not just partially)
		Quadrant nextDimensionShiftQuadrant = DetermineQuadrant(activePillar.transform.position + activePillar.DimensionShiftVector);
		if (IsMoving) {
			if (dimensionShiftQuadrant == Quadrant.SameSide && nextDimensionShiftQuadrant == Quadrant.Right) {
				Dimension = activePillar.PrevDimension(Dimension);
			}
			else if (dimensionShiftQuadrant == Quadrant.Right && nextDimensionShiftQuadrant == Quadrant.SameSide) {
				Dimension = activePillar.NextDimension(Dimension);
			}
		}
		dimensionShiftQuadrant = nextDimensionShiftQuadrant;

		// If the pillar changed this frame, handle this immediately
		if (prevPillar != activePillar) {
			HandlePillarChanged();
		}
		
		UpdateStateForCamera(Cam.Player, activePillar, true);
	}
	
	/////////////////////////////
	/// Portal Rendering Logic //
    /////////////////////////////
    #region Portal Logic
	void OnPreRenderPortal(Portal portal) {
		bool isSeenByPortalCam = false;
		foreach (SuperspectiveRenderer renderer in renderers) {
			if (renderer.r.IsVisibleFrom(VirtualPortalCamera.instance.portalCamera)) {
				isSeenByPortalCam = true;
				break;
			}
		}
		if (!isSeenByPortalCam) return;
		
		PillarDimensionObject portalDimensionObj = portal?.otherPortal?.pillarDimensionObject;
		DimensionPillar activePillar = portalDimensionObj?.activePillar;
		if (portalDimensionObj != null && activePillar != null && this.activePillar != null) {
			// Make sure that the portal has its own dimension object to avoid this condition yielding false positives
			if (this == portalDimensionObj) return; // Don't update the state for the Portal being rendered
			
			// Assumes the out portal's activePillar is the same as this object's activePillar
			// TODO: Add support for different activePillars
			int outOfPortalDimension = portalDimensionObj.Dimension;
			UpdateStateForCamera(Cam.Portal, activePillar, false, true, outOfPortalDimension);
		}
	}

	public void UpdateStateForPlayerCamera(bool sendEvents = true, bool suppressLogs = false) {
		if (activePillar == null) return;
		UpdateStateForCamera(Cam.Player, activePillar, false, true, -1);
	}

	void OnPostRenderPortal(Portal _) => UpdateStateForPlayerCamera(false, true);
	#endregion
	
	/// <summary>
	/// Sets the camSetUpFor and camQuadrant for this object, then updates the visibility state based on the given pillar and camera position.
	/// </summary>
	/// <param name="cam">Cam to use for camSetUpFor. What camera are we trying to update state for?</param>
	/// <param name="pillar">Active DimensionPillar to update state for</param>
	/// <param name="sendEvents">If false, will suppress OnStateChange events</param>
	/// <param name="suppressLogs">If true, will suppress debug logs (to avoid spamming the console log)</param>
	/// <param name="baseDimensionOverride">If provided, will use this value as the base dimension instead of the current one</param>
	public void UpdateStateForCamera(Cam cam, DimensionPillar pillar, bool sendEvents = false, bool suppressLogs = false, int baseDimensionOverride = -1) {
		camSetUpFor = cam;
		camQuadrant = DetermineQuadrant(cam.CamPos());
		
		// Don't trigger state change events when we're just doing it for the rendering
		UpdateState(pillar, false, sendEvents, suppressLogs, baseDimensionOverride);
	}

	/// <summary>
	/// Uses the camSetUpFor and the provided DimensionPillar to determine the visibility state of this object.
	/// Will update the visibility state if it has changed.
	/// </summary>
	/// <param name="pillar">DimensionPillar to update state with respect to</param>
	/// <param name="forceUpdate">If true, will call SwitchVisibilityState even if the state has not changed</param>
	/// <param name="sendEvents">If false, will suppress OnStateChange events</param>
	/// <param name="suppressLogs">If true, will suppress debug logs (to avoid spamming the console log)</param>
	/// <param name="baseDimensionOverride">If provided, will use this value as the base dimension instead of the current one</param>
	void UpdateState(DimensionPillar pillar, bool forceUpdate = false, bool sendEvents = true, bool suppressLogs = false, int baseDimensionOverride = -1) {
		int baseDimension = baseDimensionOverride >= 0 ? baseDimensionOverride : pillar.curBaseDimension;
		VisibilityState nextState = DetermineVisibilityState(pillar, camSetUpFor.CamPos(), baseDimension);

		if (nextState != visibilityState || forceUpdate) {
			SwitchVisibilityState(nextState, true, sendEvents, suppressLogs);
		}
	}

	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this the player were in that dimension. This is not very performant but there's probably a smarter way to do this.
	public int GetPillarDimensionWhere(Predicate<VisibilityState> desiredVisibility) {
		if (activePillar == null) {
			return -1;
		}

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return activePillar.curBaseDimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= activePillar.maxBaseDimension; i++) {
			if (desiredVisibility(DetermineVisibilityState(activePillar, camSetUpFor.CamPos(), i))) {
				return i;
			}
		}

		// No suitable dimension was found
		return -1;
	}
	
	// Iterates through the dimension pillar's possible dimensions and checks what the visibility state would be
	// if this object were in that dimension. This is not very performant but there's probably a smarter way to do this.
	public int GetDimensionWhere(Predicate<VisibilityState> desiredVisibility) {
		if (activePillar == null) {
			return -1;
		}

		int tempDimension = Dimension;

		// Don't need to check all the dimensions if we already are in the right one
		if (desiredVisibility(visibilityState)) {
			return Dimension;
		}

		// Try each dimension, test what the visibility state would be there
		for (int i = 0; i <= activePillar.maxBaseDimension; i++) {
			Dimension = i;
			if (desiredVisibility(DetermineVisibilityState(activePillar, camSetUpFor.CamPos(), activePillar.curBaseDimension))) {
				Dimension = tempDimension;
				return i;
			}
		}
		
		// Restore the original dimension
		Dimension = tempDimension;
		
		// No suitable dimension was found
		return -1;
	}

	/// <summary>
	/// Determines the visibility state of this object based on the given pillar, camera position, and test base dimension.
	/// Also takes reverseVisibilityState into account, which will flip the visibility state if true.
	/// </summary>
	/// <param name="pillar">DimensionPillar to use for the test</param>
	/// <param name="camPos">Position to test</param>
	/// <param name="testBaseDimension">Base dimension for the position</param>
	/// <returns>VisibilityState where the testDimension falls within the range of</returns>
	/// <exception cref="ArgumentException">If the testDimension doesn't fall within any of the ranges</exception>
	public VisibilityState DetermineVisibilityState(DimensionPillar pillar, Vector3 camPos, int testBaseDimension) {
		if (pillar == null) return visibilityState;
		if (!pillarPlanes.ContainsKey(pillar.ID)) {
			DeterminePlanes(pillar);
		}

		float testDimension = pillar.GetDimension(testBaseDimension, camPos);

		DimensionPillarPlanes thisPillarPlanes = pillarPlanes[pillar.ID];
		if (thisPillarPlanes.visibleRange.Contains(testDimension)) {
			return VisibilityState.Visible;
		}
		if (thisPillarPlanes.partiallyVisibleRange.Contains(testDimension)) {
			return VisibilityState.PartiallyVisible;
		}
		if (thisPillarPlanes.partiallyInvisibleRange.Contains(testDimension)) {
			return VisibilityState.PartiallyInvisible;
		}
		if (thisPillarPlanes.invisibleRange.Contains(testDimension)) {
			return VisibilityState.Invisible;
		}
		
		throw new ArgumentException($"Test dimension {testDimension} was not in any of the ranges!");
	}

	/// <summary>
	/// Recalculates camQuadrant based on the playerCam's position.
	/// Can be called by anyone who needs this state to be updated before further logic is run.
	/// </summary>
	public void DetermineQuadrantForPlayerCam() {
		camQuadrant = DetermineQuadrant(Cam.Player.CamPos());
	}

	private Quadrant DetermineQuadrant(Vector3 position) {
		if (!pillarPlanes.ContainsKey(activePillar.ID)) {
			DeterminePlanes(activePillar);
		}
		
		bool leftPlaneTest = pillarPlanes[activePillar.ID].leftParallel.GetSide(position);
		bool rightPlaneTest = pillarPlanes[activePillar.ID].rightParallel.GetSide(position);

		switch (leftPlaneTest, rightPlaneTest) {
			case (true, true):
				return Quadrant.Left;
			case (true, false):
				return Quadrant.Opposite;
			case (false, true):
				return Quadrant.SameSide;
			case (false, false):
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
		float objectBoundsMin = float.MaxValue;
		float objectBoundsMax = float.MinValue;
		foreach (Vector3 corner in allCorners) {
			Vector3 projectedCorner = Vector3.ProjectOnPlane(corner, pillar.Axis) + projectedVerticalPillarOffset;
			float signedAngle = Vector3.SignedAngle(dimensionShiftVector, projectedCorner - pillar.transform.position, pillar.Axis);
			float dimension = pillar.GetDimension(Dimension, projectedCorner);
			if (signedAngle < minAngle) {
				minAngle = signedAngle;
				minAngleVector = (projectedCorner - pillar.transform.position);
				objectBoundsMin = dimension;
			}
			else if (signedAngle > maxAngle) {
				maxAngle = signedAngle;
				maxAngleVector = (projectedCorner - pillar.transform.position);
				objectBoundsMax = dimension;
			}

			float distance = Vector3.Distance(projectedCorner, pillar.transform.position);
			if (distance > maxDistance) {
				maxDistance = distance;
			}

			if (DEBUG) {
				Debug.DrawRay(pillar.transform.position, projectedCorner - pillar.transform.position, Color.green);
			}
		}

		Vector3 minAngleNormalVector = Vector3.Cross(minAngleVector.normalized, pillar.Axis);
		Vector3 maxAngleNormalVector = Vector3.Cross(maxAngleVector.normalized, pillar.Axis);
		Plane leftParallelPlane = new Plane(minAngleNormalVector, pillar.transform.position);
		Plane rightParallelPlane = new Plane(maxAngleNormalVector, pillar.transform.position);

		float WrappedValue(float value) {
			if (value < 0) value += pillar.maxBaseDimension + 1;
			if (value >= pillar.maxBaseDimension + 1) value -= pillar.maxBaseDimension + 1;

			return value;
		}
		
		// Object bounds:
		if (objectBoundsMin > objectBoundsMax) {
			objectBoundsMin = WrappedValue(objectBoundsMin - 1);
		}
		
		// Fully visible range:
		float fullyVisibleMin = WrappedValue(objectBoundsMax - .5f);
		float fullyVisibleMax = WrappedValue(objectBoundsMin + .5f);
		
		// Partially visible range:
		float partiallyVisibleMin = WrappedValue(objectBoundsMin - .5f);
		float partiallyVisibleMax = WrappedValue(objectBoundsMax - .5f);
		
		float partiallyInvisibleMin = WrappedValue(objectBoundsMin + .5f);
		float partiallyInvisibleMax = WrappedValue(objectBoundsMax + .5f);
		
		float invisibleMin = WrappedValue(objectBoundsMax + .5f);
		float invisibleMax = WrappedValue(objectBoundsMin - .5f);
		
		// Take reverseVisibilityStates into account
		if (reverseVisibilityStates) {
			// Swap object bounds for inverse pillar dimension objects
			(objectBoundsMin, objectBoundsMax) = (objectBoundsMax, objectBoundsMin);
		}

		objectBounds = new DimensionRange() {
			min = objectBoundsMin,
			max = objectBoundsMax
		};
		visibleRange = new DimensionRange() {
			min = fullyVisibleMin,
			max = fullyVisibleMax
		};
		partiallyVisibleRange = new DimensionRange() {
			min = partiallyVisibleMin,
			max = partiallyVisibleMax
		};
		partiallyInvisibleRange = new DimensionRange() {
			min = partiallyInvisibleMin,
			max = partiallyInvisibleMax
		};
		invisibleRange = new DimensionRange() {
			min = invisibleMin,
			max = invisibleMax
		};
		
		DimensionPillarPlanes thisPillarPlanes = new DimensionPillarPlanes {
			leftParallel = leftParallelPlane,
			rightParallel = rightParallelPlane,
			objectBounds = objectBounds,
			visibleRange = visibleRange,
			partiallyVisibleRange = partiallyVisibleRange,
			partiallyInvisibleRange = partiallyInvisibleRange,
			invisibleRange = invisibleRange,
			maxDistance = maxDistance
		};

		pillarPlanes[pillar.ID] = thisPillarPlanes;
		lastPillarPlanes = thisPillarPlanes;

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
		if (renderer.r is MeshRenderer && meshFilter != null && meshFilter.sharedMesh != null) {
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
			ExtDebug.DrawPlane(activePillar.transform.position, thisPillarPlanes.leftParallel.normal, activePillar.dimensionWall.PillarHeight, 2f*thisPillarPlanes.maxDistance, Color.blue);
			ExtDebug.DrawPlane(activePillar.transform.position, thisPillarPlanes.rightParallel.normal, activePillar.dimensionWall.PillarHeight, 2f*thisPillarPlanes.maxDistance, Color.blue);
		}
	}

	#region Saving
	// When inheriting from a SuperspectiveObject, we need to override the CreateSave method to return the type of SaveObject that has the data we want to save
	// Otherwise the SaveObject will be of the base class save object type
	public override SaveObject CreateSave() {
		return new PillarDimensionObjectSave(this);
	}
	
	public override void LoadSave(DimensionObjectSave save) {
		base.LoadSave(save);

		if (save is not PillarDimensionObjectSave pillarDimObjSave) {
			Debug.LogError($"Expected save object of type {nameof(PillarDimensionObjectSave)} but got {save.GetType()} instead");
			return;
		}
		
		pillars = pillarDimObjSave.pillars;
		Dimension = pillarDimObjSave.dimension;
	}

	[Serializable]
	public class PillarDimensionObjectSave : DimensionObjectSave {
		public PillarReference[] pillars;
		public int dimension;

		public PillarDimensionObjectSave(PillarDimensionObject dimensionObj) : base(dimensionObj) {
			this.pillars = dimensionObj.pillars;
			this.dimension = dimensionObj.Dimension;
		}
	}
	#endregion
}