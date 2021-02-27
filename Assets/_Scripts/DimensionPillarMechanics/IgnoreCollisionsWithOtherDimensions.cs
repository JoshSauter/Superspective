using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class IgnoreCollisionsWithOtherDimensions : MonoBehaviour {
	public PillarDimensionObject corporealDimensionObject;
	Collider thisCollider;
	public Collider kinematicCollider;
	SphereCollider thisTrigger;

	const float triggerZoneSize = 1.5f;
	const float minTriggerRadius = 1;

	HashSet<Collider> collidersBeingIgnored = new HashSet<Collider>();

    void Start() {
		thisCollider = GetComponent<Collider>();

		CreateTriggerZone();
    }

	void IgnoreCollision(Collider collider) {
		if (!collidersBeingIgnored.Contains(collider)) {
			//Debug.Log($"{gameObject.name} ignoring collision with {collider.gameObject.name}");
			Physics.IgnoreCollision(thisCollider, collider, true);
			if (kinematicCollider != null) Physics.IgnoreCollision(kinematicCollider, collider, true);

			collidersBeingIgnored.Add(collider);
		}
	}

	void RestoreCollision(Collider collider) {
		if (collidersBeingIgnored.Contains(collider)) {
			//Debug.Log($"{gameObject.name} restoring collision with {collider.gameObject.name}");
			Physics.IgnoreCollision(thisCollider, collider, false);
			if (kinematicCollider != null) Physics.IgnoreCollision(kinematicCollider, collider, false);

			collidersBeingIgnored.Remove(collider);
		}
	}

	void CreateTriggerZone() {
		GameObject triggerGO = new GameObject("IgnoreCollisionsTriggerZone");
		triggerGO.layer = LayerMask.NameToLayer("Ignore Raycast");
		triggerGO.transform.SetParent(transform, false);
		thisTrigger = triggerGO.AddComponent<SphereCollider>();
		thisTrigger.isTrigger = true;
		SetTriggerZoneSize();
	}

	void OnTriggerStay(Collider other) {
		if (corporealDimensionObject == null) return;
		PillarDimensionObject otherDimensionObj = Utils.FindDimensionObjectRecursively(other.gameObject.transform);
		if (otherDimensionObj != null) {
			int testDimension = corporealDimensionObject.GetPillarDimensionWhereThisObjectWouldBeInVisibilityState(v => v == VisibilityState.visible || v == VisibilityState.partiallyVisible);
			if (testDimension == -1) {
				return;
			}

			VisibilityState test1 = corporealDimensionObject.DetermineVisibilityState(corporealDimensionObject.playerQuadrant, corporealDimensionObject.dimensionShiftQuadrant, testDimension);
			VisibilityState test2 = otherDimensionObj.DetermineVisibilityState(otherDimensionObj.playerQuadrant, otherDimensionObj.dimensionShiftQuadrant, testDimension);

			bool areOpposites = Mathf.Abs((int)test2 - (int)test1) == 2;

			//Debug.Log($"Corporeal test: {test1}\nOther test: {test2}");
			if (Mathf.Abs((int)test2 - (int)test1) == 2) {
				IgnoreCollision(other);
			}
			else {
				RestoreCollision(other);
			}
		}
	}

	void OnTriggerExit(Collider other) {
		RestoreCollision(other);
	}

	void FixedUpdate() {
		SetTriggerZoneSize();
    }

	void SetTriggerZoneSize() {
		Vector3 colliderBounds = thisCollider.bounds.size;
		float maxBounds = Mathf.Max(colliderBounds.x, colliderBounds.y, colliderBounds.z);
		float maxScale = Mathf.Max(thisTrigger.transform.lossyScale.x, thisTrigger.transform.lossyScale.y, thisTrigger.transform.lossyScale.z, 0.01f);

		thisTrigger.radius = Mathf.Max(minTriggerRadius, maxBounds * triggerZoneSize) / maxScale;
	}
}
