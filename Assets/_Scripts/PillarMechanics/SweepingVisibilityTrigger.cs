using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public enum MovementDirection {
	clockwise,
	counterclockwise
}

[RequireComponent(typeof(Rigidbody))]
public class SweepingVisibilityTrigger : MonoBehaviour {
	public ObscurePillar pillar;

	private void Start() {
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
		GetComponent<Collider>().isTrigger = true;
	}

	private void OnTriggerEnter(Collider other) {
		PartiallyVisibleObject pvo = other.gameObject.GetComponent<PartiallyVisibleObject>();
		if (pvo != null) {
			bool stateChangedToPartiallyVisible = pvo.HitBySweepingCollider(GetMovementDirection(other, true));
			if (stateChangedToPartiallyVisible) {
				pillar.partiallyRenderedObjects.Add(pvo);
			}
		}
	}

	private void OnTriggerExit(Collider other) {
		PartiallyVisibleObject pvo = other.gameObject.GetComponent<PartiallyVisibleObject>();
		if (pvo != null) {
			bool stateChangedFromPartiallyVisible = pvo.SweepingColliderExit(GetMovementDirection(other, false));
			if (stateChangedFromPartiallyVisible) {
				pillar.partiallyRenderedObjects.Remove(pvo);
			}
		}
	}

	private MovementDirection GetMovementDirection(Collider other, bool entering) {
		Vector3 midwayPointOfOtherRenderer = other.gameObject.GetComponent<Renderer>().bounds.center;
		Vector3 pillarPosition = pillar.transform.position;
		PolarCoordinate sweepingColliderPolar = PolarCoordinate.CartesianToPolar(transform.position - pillar.transform.position);
		PolarCoordinate midwayPointPolar = PolarCoordinate.CartesianToPolar(midwayPointOfOtherRenderer - pillar.transform.position);
		return CompareAngles(sweepingColliderPolar.angle, midwayPointPolar.angle, entering);
	}

	private MovementDirection CompareAngles(float colliderRad, float objectRad, bool entering) {
		if (((objectRad - colliderRad + (2*Mathf.PI)) % (2 * Mathf.PI)) > Mathf.PI) {
			return entering ? MovementDirection.clockwise : MovementDirection.counterclockwise;
		}
		else {
			return entering ? MovementDirection.counterclockwise : MovementDirection.clockwise;
		}
	}
}
