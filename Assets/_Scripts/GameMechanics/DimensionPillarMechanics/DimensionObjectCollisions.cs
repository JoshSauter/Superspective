using DimensionObjectMechanics;
using Saving;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(BetterTrigger))]
public class DimensionObjectCollisions : SuperspectiveObject, BetterTriggers {
	public DimensionObject dimensionObject;

	protected override void Init() {
		base.Init();

		if (id == null) id = gameObject.GetOrAddComponent<UniqueId>();
	}
	
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
