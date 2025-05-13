using System.Collections;
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
		
		//SaveManager.AfterSave += 1
	}
	
	void Update() {
		if (dimensionObject == null) {
			debug.LogError("DimensionObjectCollisions script is missing a DimensionObject reference, self-destructing.", true);
			Destroy(gameObject);
		}

		transform.localPosition = Vector3.zero;
	}

	public void OnBetterTriggerEnter(Collider other) {
		if (SaveManager.isCurrentlyLoadingSave) {
			// Handle it after the game is loaded
			debug.LogWarning($"Game is loading, waiting to set collision with {other.FullPath()}.");
			StartCoroutine(AfterGameLoaded(other));
			return;
		}
		if (GameManager.instance.IsCurrentlyLoading) return;
		if (dimensionObject == null) return;
		
		debug.Log($"Determining collision between {dimensionObject.ID} and {other.FullPath()}...");

		foreach (Collider thisCollider in dimensionObject.colliders) {
			DimensionObjectManager.instance.SetCollision(other, thisCollider, dimensionObject.ID);
		}
	}

	public void OnBetterTriggerExit(Collider other) {}

	public void OnBetterTriggerStay(Collider c) {}

	private IEnumerator AfterGameLoaded(Collider other) {
		yield return new WaitWhile(() => SaveManager.isCurrentlyLoadingSave);
		if (dimensionObject == null || other == null) yield break;

		debug.LogWarning($"Game has loaded, setting collision with {other.FullPath()}.");
		foreach (Collider thisCollider in dimensionObject.colliders) {
			DimensionObjectManager.instance.SetCollision(other, thisCollider, dimensionObject.ID);
		}
	}
}
