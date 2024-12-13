using DimensionObjectMechanics;
using Saving;
using UnityEngine;

[RequireComponent(typeof(UniqueId), typeof(BetterTrigger))]
public class DimensionObjectCollisions : SaveableObject, BetterTriggers {
	public DimensionObject dimensionObject;
	
	void Update() {
		if (dimensionObject == null) {
			debug.LogError("DimensionObjectCollisions script is missing a DimensionObject reference, self-destructing.", true);
			Destroy(gameObject);
		}
	}

	public void OnBetterTriggerEnter(Collider other) {
		if (GameManager.instance.IsCurrentlyLoading) return;
		if (dimensionObject == null) return;

		foreach (Collider thisCollider in dimensionObject.colliders) {
			DimensionObjectManager.instance.SetCollision(other, thisCollider, dimensionObject.ID);
		}
	}

	public void OnBetterTriggerExit(Collider other) {}

	public void OnBetterTriggerStay(Collider c) {}
}
