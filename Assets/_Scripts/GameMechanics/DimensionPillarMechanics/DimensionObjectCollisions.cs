using DimensionObjectMechanics;
using Saving;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class DimensionObjectCollisions : SaveableObject {
	public DimensionObject dimensionObject;
	
	void Update() {
		if (dimensionObject == null) {
			debug.LogError("DimensionObjectCollisions script is missing a DimensionObject reference, self-destructing.", true);
			Destroy(gameObject);
		}
	}

	void OnTriggerStay(Collider other) {
		if (GameManager.instance.IsCurrentlyLoading) return;
		if (dimensionObject == null) return;

		foreach (Collider thisCollider in dimensionObject.colliders) {
			DimensionObjectManager.instance.SetCollision(other, thisCollider, dimensionObject.ID, true);
		}
	}
}
