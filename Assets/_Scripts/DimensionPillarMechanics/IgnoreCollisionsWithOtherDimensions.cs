using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class IgnoreCollisionsWithOtherDimensions : MonoBehaviour {
	PillarDimensionObject thisDimensionObject;
	Collider thisCollider;
	Collider invertColorsCollider;
	Collider kinematicCollider;
	SphereCollider thisTrigger;

	const float triggerZoneSize = 1.5f;
	const float minTriggerRadius = 1;

	HashSet<Collider> collidersBeingIgnored = new HashSet<Collider>();

    void Start() {
		thisDimensionObject = Utils.FindDimensionObjectRecursively(transform);
		thisCollider = GetComponent<Collider>();
		invertColorsCollider = transform.Find("InvertColors")?.GetComponent<Collider>();
		kinematicCollider = invertColorsCollider?.transform.Find("KinematicCollider").GetComponent<Collider>();

		if (thisDimensionObject != null) {
			thisDimensionObject.OnBaseDimensionChange += HandleBaseDimensionChange;
		}

		CreateTriggerZone();
    }

	void CreateTriggerZone() {
		GameObject triggerGO = new GameObject("IgnoreCollisionsTriggerZone");
		triggerGO.layer = LayerMask.NameToLayer("Ignore Raycast");
		triggerGO.transform.SetParent(transform, false);
		thisTrigger = triggerGO.AddComponent<SphereCollider>();
		thisTrigger.isTrigger = true;
		SetTriggerZoneSize();
	}

	private void OnTriggerStay(Collider other) {
		if (thisDimensionObject == null) return;
		PillarDimensionObject otherDimensionObj = Utils.FindDimensionObjectRecursively(other.gameObject.transform);
		if (otherDimensionObj != null && otherDimensionObj.baseDimension != thisDimensionObject.baseDimension) {
			if (!collidersBeingIgnored.Contains(other)) {
				collidersBeingIgnored.Add(other);
				Physics.IgnoreCollision(thisCollider, other, true);
				if (invertColorsCollider != null) Physics.IgnoreCollision(invertColorsCollider, other, true);
				if (kinematicCollider != null) Physics.IgnoreCollision(kinematicCollider, other, true);
			}
		}
	}

	void HandleBaseDimensionChange() {
		foreach (var collider in collidersBeingIgnored) {
			Physics.IgnoreCollision(thisCollider, collider, false);
			if (invertColorsCollider != null) Physics.IgnoreCollision(invertColorsCollider, collider, false);
			if (kinematicCollider != null) Physics.IgnoreCollision(kinematicCollider, collider, false);
		}
		collidersBeingIgnored.Clear();
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
