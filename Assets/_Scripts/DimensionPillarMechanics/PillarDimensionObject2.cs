using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using EpitaphUtils;

// DimensionObjectBase.baseDimension is the lower dimension that this object exists in
// If this object goes across the 180° + dimensionShiftAngle (when the player is standing in the direction of pillar's transform.forward from pillar),
// it will act as a baseDimension+1 object when the pillar is in that dimension.
public class PillarDimensionObject2 : DimensionObject {
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
	DimensionPillar pillar => pillarsSet.Contains(DimensionPillar.activePillar) ? DimensionPillar.activePillar : null;
	public DimensionPillar[] pillars;
	HashSet<DimensionPillar> pillarsSet = new HashSet<DimensionPillar>();
	Dictionary<DimensionPillar, Plane> leftParallels = new Dictionary<DimensionPillar, Plane>();
	Dictionary<DimensionPillar, Plane> rightParallels = new Dictionary<DimensionPillar, Plane>();

	Vector3 minAngleVector, maxAngleVector;

	public bool useColliderBoundsInsteadOfRendererBounds = false;
	public bool thisObjectMoves = false;
	public Rigidbody thisRigidbody;
	Collider[] colliders;

	public enum SwitchedDimensions {
		Neither,
		Up,
		Down
	}
	SwitchedDimensions switchedDimensions = SwitchedDimensions.Neither;

	private void OnValidate() {
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

		foreach (var p in pillars) {
			p.OnDimensionChangeWithDirection += HandleDimensionChangeWithDirection;
		}
	}

	void HandleDimensionChangeWithDirection(int prev, int next, DimensionPillar.DimensionSwitch direction) {
		if (direction == DimensionPillar.DimensionSwitch.Up) {
			baseDimension = pillar.NextDimension(baseDimension);
		}
		else {
			baseDimension = pillar.PrevDimension(baseDimension);
		}
	}

	public override IEnumerator Start() {
		pillarsSet = new HashSet<DimensionPillar>(pillars);
		renderers = GetAllEpitaphRenderers().ToArray();

		colliders = new Collider[] { GetComponent<Collider>() };
		if (treatChildrenAsOneObjectRecursively) {
			colliders = GetCollidersRecursively().ToArray();
		}

		foreach (var p in pillars) {
			DeterminePlanes(p);
		}

		if (pillar == null) yield break;

		playerQuadrant = DetermineQuadrant(EpitaphScreen.instance.playerCamera.transform.position);
		dimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.dimensionShiftVector);
		UpdateState();
	}

	List<Collider> GetCollidersRecursively() {
		List<Collider> colliders = new List<Collider>();
		GetCollidersRecursivelyHelper(transform, ref colliders);
		return colliders;
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

	private void FixedUpdate() {
		if (pillar == null) return;
		if (DEBUG) {
			Debug.DrawRay(pillar.transform.position, minAngleVector, Color.cyan);
			Debug.DrawRay(pillar.transform.position, maxAngleVector, Color.blue);
		}

		bool thisObjectMoving = thisObjectMoves && !thisRigidbody.IsSleeping();
		// Used to determine first frame where dimensionShiftQuadrant == Quadrant.Opposite (moving objects only)
		bool dimensionShiftWasNotInOppositeQuadrant = dimensionShiftQuadrant != Quadrant.Opposite;
		if (thisObjectMoving) {
			DeterminePlanes(pillar);
		}

		playerQuadrant = DetermineQuadrant(EpitaphScreen.instance.playerCamera.transform.position);
		dimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.dimensionShiftVector);


		UpdateState();
	}

	private SwitchedDimensions ThisObjectSwitchedDimensions() {
		if (dimensionShiftQuadrant == Quadrant.Opposite) {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(pillar.dimensionShiftVector.normalized, pillar.axis);
			Plane dimensionShiftPlane = new Plane(dimensionShiftPlaneNormalVector, pillar.transform.position);
			return dimensionShiftPlane.GetSide(transform.position) ? SwitchedDimensions.Up : SwitchedDimensions.Down;
		}
		else {
			return SwitchedDimensions.Neither;
		}
	}

	private void UpdateState() {
		VisibilityState nextState = DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, pillar.curDimension);

		if (nextState != visibilityState) {
			SwitchVisibilityState(nextState, true);
		}

		if (visibilityState == VisibilityState.partiallyVisible || visibilityState == VisibilityState.partiallyInvisible) {
			SetDimensionValuesInMaterials(pillar.curDimension);
		}
	}

	bool HasGoneToNextDimension(DimensionPillar pillar, Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant) {
		if (playerQuadrant == dimensionShiftQuadrant) {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(pillar.dimensionShiftVector.normalized, pillar.axis);
			Plane dimensionShiftPlane = new Plane(dimensionShiftPlaneNormalVector, pillar.transform.position);
			return !dimensionShiftPlane.GetSide(EpitaphScreen.instance.playerCamera.transform.position);
		}
		else {
			return false;
		}
	}

	VisibilityState DetermineVisibilityState(Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant, int dimension) {
		if (HasGoneToNextDimension(pillar, playerQuadrant, dimensionShiftQuadrant)) {
			dimension = pillar.PrevDimension(dimension);
		}

		switch (playerQuadrant) {
			case Quadrant.Opposite:
				if (dimension == baseDimension) {
					return VisibilityState.partiallyVisible;
				}
				else if (dimension == pillar.NextDimension(baseDimension)) {
					return VisibilityState.partiallyInvisible;
				}
				else {
					return VisibilityState.invisible;
				}
			case Quadrant.Left:
			case Quadrant.SameSide:
			case Quadrant.Right:
				if ((int)dimensionShiftQuadrant < (int)playerQuadrant) {
					if (dimension == pillar.NextDimension(baseDimension)) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
				else {
					if (dimension == baseDimension) {
						return VisibilityState.visible;
					}
					else {
						return VisibilityState.invisible;
					}
				}
			default:
				throw new System.Exception($"Unhandled case: {this.playerQuadrant}");
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
		Vector3 pillarPos = pillar.transform.position;
		Vector3 projectedPillarCenter = Vector3.ProjectOnPlane(pillarPos, pillar.axis);
		Vector3 projectedVerticalPillarOffset = pillarPos - projectedPillarCenter;

		List<Bounds> allBounds = new List<Bounds>();
		Vector3 positionAvg = Vector3.zero;
		if (!useColliderBoundsInsteadOfRendererBounds) {
			foreach (var r in renderers) {
				Bounds b = r.GetRendererBounds();
				allBounds.Add(b);
				positionAvg += Vector3.ProjectOnPlane(b.center, pillar.axis);
			}
		}
		else {
			foreach (var c in colliders) {
				Bounds b = c.bounds;
				allBounds.Add(b);
				positionAvg += Vector3.ProjectOnPlane(b.center, pillar.axis);
			}
		}
		positionAvg /= allBounds.Count;
		positionAvg += projectedVerticalPillarOffset;

		Debug.DrawRay(pillarPos, positionAvg - pillarPos, Color.magenta);

		bool flipDimensionShiftAngle = Vector3.Dot(pillar.dimensionShiftVector, positionAvg - pillar.transform.position) < 0;
		Vector3 dimensionShiftVector = flipDimensionShiftAngle ? Quaternion.AngleAxis(180, pillar.axis) * pillar.dimensionShiftVector : pillar.dimensionShiftVector;

		float minAngle = float.MaxValue;
		float maxAngle = float.MinValue;
		minAngleVector = Vector3.zero;
		maxAngleVector = Vector3.zero;
		foreach (var b in allBounds) {
			Vector3[] corners = new Vector3[] {
				new Vector3(b.min.x, b.min.y, b.min.z),
				new Vector3(b.min.x, b.min.y, b.max.z),
				new Vector3(b.min.x, b.max.y, b.min.z),
				new Vector3(b.min.x, b.max.y, b.max.z),
				new Vector3(b.max.x, b.min.y, b.min.z),
				new Vector3(b.max.x, b.min.y, b.max.z),
				new Vector3(b.max.x, b.max.y, b.min.z),
				new Vector3(b.max.x, b.max.y, b.max.z)
			};
			foreach (var c in corners) {
				Vector3 projectedCorner = Vector3.ProjectOnPlane(c, pillar.axis) + projectedVerticalPillarOffset;
				float signedAngle = Vector3.SignedAngle(dimensionShiftVector, projectedCorner - pillarPos, pillar.axis);
				if (signedAngle < minAngle) {
					minAngle = signedAngle;
					minAngleVector = (projectedCorner - pillarPos);
				}
				else if (signedAngle > maxAngle) {
					maxAngle = signedAngle;
					maxAngleVector = (projectedCorner - pillarPos);
				}
			}
		}

		Vector3 minAngleNormalVector = Vector3.Cross(minAngleVector.normalized, pillar.axis);
		Vector3 maxAngleNormalVector = Vector3.Cross(maxAngleVector.normalized, pillar.axis);
		leftParallels[pillar] = new Plane(minAngleNormalVector, pillarPos);
		rightParallels[pillar] = new Plane(maxAngleNormalVector, pillarPos);

		// Don't spam the console with a moving object updating this info every frame
		if (DEBUG && !thisObjectMoves) {
			debug.Log($"Min: {minAngle}°, direction: {minAngleVector.normalized:F2}\nMax: {maxAngle}°, direction: {maxAngleVector.normalized:F2}");
		}
	}

	private void OnDrawGizmos() {
		if (DEBUG && pillar != null) {
			DrawPlanes(pillar.transform.position, leftParallels[pillar].normal);
			DrawPlanes(pillar.transform.position, rightParallels[pillar].normal);
		}
	}

	void DrawPlanes(Vector3 point, Vector3 normal) {
		Quaternion rotation = Quaternion.LookRotation(normal);
		Matrix4x4 trs = Matrix4x4.TRS(point, rotation, Vector3.one);
		Gizmos.matrix = trs;
		Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
		float height = 16f;
		float width = 100f;
		float depth = 0.0001f;
		Gizmos.DrawCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, depth));
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = Color.white;
	}
}