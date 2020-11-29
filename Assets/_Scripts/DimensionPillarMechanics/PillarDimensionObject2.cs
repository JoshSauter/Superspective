using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// DimensionObjectBase.baseDimension is the lower dimension that this object exists in
// If this object goes across the 180° + dimensionShiftAngle (when the player is standing in the direction of pillar's transform.forward from pillar),
// it will act as a baseDimension+1 object when the pillar is in that dimension.
public class PillarDimensionObject2 : DimensionObjectBase {
	public enum Quadrant {
		Opposite,
		Left,
		SameSide,
		Right
	}
	public Quadrant playerQuadrant;
	public Quadrant dimensionShiftQuadrant;
	public DimensionPillar pillar;
	bool spansMultipleDimensions = false;
	Plane leftParallel;//, leftPerpindicular;
	Plane rightParallel;//, rightPerpindicular;

	// These are only used for debug purposes and are calcuated when the leftParallel and rightParallel planes are
	public bool showCorners = false;
	float minAngle, maxAngle;
	Vector3 minAngleVector, maxAngleVector;

	public bool useColliderBoundsInsteadOfRendererBounds = false;
	Collider[] colliders;

	public override IEnumerator Start() {
		renderers = GetAllEpitaphRenderers().ToArray();
		// TODO: Find colliders recursively if necessary
		colliders = new Collider[] { GetComponent<Collider>() };

		if (pillar == null) yield break;
		DeterminePlanes();

		playerQuadrant = DetermineQuadrant(Player.instance.transform.position);
		dimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.dimensionShiftVector);
		SwitchVisibilityState(DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, pillar.curDimension), true);
	}

	private void FixedUpdate() {
		if (pillar == null) return;
		if (DEBUG) {
			debug.Log($"Min: {minAngle}°, direction: {minAngleVector.normalized:F2}\nMax: {maxAngle}°, direction: {maxAngleVector.normalized:F2}\nSpans multiple dimensions: {spansMultipleDimensions}");
			Debug.DrawRay(pillar.transform.position, minAngleVector, Color.cyan);
			Debug.DrawRay(pillar.transform.position, maxAngleVector, Color.blue);
		}

		playerQuadrant = DetermineQuadrant(Player.instance.transform.position);
		dimensionShiftQuadrant = DetermineQuadrant(pillar.transform.position + pillar.dimensionShiftVector);
		int playerDimension = pillar.curDimension;

		//debug.Log(DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, playerDimension));
		SwitchVisibilityState(DetermineVisibilityState(playerQuadrant, dimensionShiftQuadrant, playerDimension));

		if (visibilityState == VisibilityState.partiallyVisible || visibilityState == VisibilityState.partiallyInvisible) {
			SetDimensionValuesInMaterials(pillar.curDimension);
		}
	}

	public override void SwitchVisibilityState(VisibilityState nextState, bool ignoreTransitionRules = false) {
		if (nextState != visibilityState || ignoreTransitionRules) {
			base.SwitchVisibilityState(nextState, ignoreTransitionRules);
		}
	}

	VisibilityState DetermineVisibilityState(Quadrant playerQuadrant, Quadrant dimensionShiftQuadrant, int dimension) {
		if (playerQuadrant == dimensionShiftQuadrant) {
			Vector3 dimensionShiftPlaneNormalVector = Vector3.Cross(pillar.dimensionShiftVector.normalized, pillar.axis);
			Plane dimensionShiftPlane = new Plane(dimensionShiftPlaneNormalVector, pillar.transform.position);
			bool hasGoneToNextDimension = !dimensionShiftPlane.GetSide(Player.instance.transform.position);

			// If the player has moved to the next dimension, determine visibility state as if dimension were 1 lower
			if (hasGoneToNextDimension) {
				dimension = pillar.PrevDimension(dimension);
			}
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
		bool leftPlaneTest = leftParallel.GetSide(position);
		bool rightPlaneTest = rightParallel.GetSide(position);

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

	void DeterminePlanes() {
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

		minAngle = float.MaxValue;
		maxAngle = float.MinValue;
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

				if (!showCorners) {
					Debug.DrawRay(pillarPos, projectedCorner - pillarPos, Color.black);
				}
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
		leftParallel = new Plane(minAngleNormalVector, pillarPos);
		rightParallel = new Plane(maxAngleNormalVector, pillarPos);
		spansMultipleDimensions = flipDimensionShiftAngle && (Mathf.Sign(minAngle) != Mathf.Sign(maxAngle));
	}

	void DrawPlane(Vector3 position, Vector3 normal) {
		Vector3 v3;

		if (normal.normalized != Vector3.forward) {
			v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
		}
		else {
			v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;
		}

		var corner0 = position + v3;
		var corner2 = position - v3;
		var q = Quaternion.AngleAxis(90.0f, normal);
		v3 = q * v3;
		var corner1 = position + v3;
		var corner3 = position - v3;

		Debug.DrawLine(corner0, corner2, Color.gray);
		Debug.DrawLine(corner1, corner3, Color.gray);
		Debug.DrawLine(corner0, corner1, Color.gray);
		Debug.DrawLine(corner1, corner2, Color.gray);
		Debug.DrawLine(corner2, corner3, Color.gray);
		Debug.DrawLine(corner3, corner0, Color.gray);
		Debug.DrawRay(position, normal, Color.green);
	}
}