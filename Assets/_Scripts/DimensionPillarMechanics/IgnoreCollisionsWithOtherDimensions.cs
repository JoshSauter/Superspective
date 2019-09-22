using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollisionsWithOtherDimensions : MonoBehaviour {
	DimensionObject thisDimensionObject;
	Collider thisCollider;
	SphereCollider thisTrigger;

	float triggerZoneSize = 1.5f;
	float minTriggerRadius = 1;

	HashSet<Collider> collidersBeingIgnored = new HashSet<Collider>();

    void Start() {
		thisDimensionObject = GetComponent<DimensionObject>();
		thisCollider = GetComponent<Collider>();

		thisDimensionObject.OnBaseDimensionChange += HandleBaseDimensionChange;

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

	private void OnTriggerEnter(Collider other) {
		DimensionObject otherDimensionObj = FindDimensionObjectRecursively(other.gameObject.transform);
		if (otherDimensionObj != null && otherDimensionObj.baseDimension != thisDimensionObject.baseDimension) {
			print("Uh oh spaghettios!");
			if (!collidersBeingIgnored.Contains(other)) {
				collidersBeingIgnored.Add(other);
				Physics.IgnoreCollision(thisCollider, other, true);
			}
		}
	}

	void HandleBaseDimensionChange() {
		foreach (var collider in collidersBeingIgnored) {
			Physics.IgnoreCollision(thisCollider, collider, false);
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

	private DimensionObject FindDimensionObjectRecursively(Transform go) {
		DimensionObject dimensionObj = go.GetComponent<DimensionObject>();
		Transform parent = go.parent;
		if (dimensionObj != null) {
			return dimensionObj;
		}
		else if (parent != null) {
			return FindDimensionObjectRecursively(parent);
		}
		else return null;
	}
}
