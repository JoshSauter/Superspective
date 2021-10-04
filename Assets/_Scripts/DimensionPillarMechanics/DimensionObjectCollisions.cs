using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using SuperspectiveUtils;

public class DimensionObjectCollisions : MonoBehaviour {
	public DimensionObject dimensionObject;

	HashSet<DimensionObject> dimensionObjectsIgnored = new HashSet<DimensionObject>();
	// Collider ignored -> whether or not it's ignored as part of dimensionObjectsIgnored
	Dictionary<Collider, bool> collidersIgnored = new Dictionary<Collider, bool>();

	[ShowNativeProperty]
	public int dimensionObjectsIgnoredCount => dimensionObjectsIgnored.Count;
	[ShowNativeProperty]
	public int collidersIgnoredCount => collidersIgnored.Count;

	[Button("Print current state")]
	void PrintIgnoreInfo() {
		Debug.Log($"DimensionObjects Ignored:\n{string.Join("\n", dimensionObjectsIgnored)}");
		Debug.Log($"Colliders Ignored:\n{string.Join("\n", collidersIgnored)}");
	}

	void Start() {
		dimensionObject.OnStateChangeSimple += OnThisDimensionObjectVisibilityStateChange;
	}

	void OnThisDimensionObjectVisibilityStateChange() {
		List<DimensionObject> dimensionObjectsIgnoredCopy = new List<DimensionObject>(dimensionObjectsIgnored);
		foreach (var otherDimensionObject in dimensionObjectsIgnoredCopy) {
			DetermineCollisionWithDimensionObject(otherDimensionObject);
		}

		List<Collider> remainingCollidersCopy = new List<Collider>(collidersIgnored.Where(kv => !kv.Value).ToDictionary().Keys);
		foreach (var collider in remainingCollidersCopy) {
			DetermineCollisionWithNonDimensionObject(collider);
		}
	}

	void Update() {
		if (dimensionObject == null) {
			Destroy(gameObject);
		}
	}

	void DetermineCollisionWithNonDimensionObject(Collider other) {
		bool shouldCollide = other.TaggedAsPlayer()
			? dimensionObject.ShouldCollideWithPlayer()
			: dimensionObject.ShouldCollideWithNonDimensionObject();
		
		if (!shouldCollide) {
			IgnoreCollisionWithCollider(other, false);
		}
		else {
			RestoreCollisionWithCollider(other);
		}
	}

	// Second parameter is unused but matches the event shape so we don't subscribe with an anonymous delegate
	void DetermineCollisionWithDimensionObject(DimensionObject otherDimensionObj) {
		// Make sure channels are different or we are in a different visibility state
		if (otherDimensionObj.isBeingDestroyed) {
			// If the other DimensionObject is being destroyed, treat it as a non DimensionObject
			if (!dimensionObject.ShouldCollideWithNonDimensionObject()) {
				IgnoreCollisionWithDimensionObject(otherDimensionObj);
			}
			else {
				RestoreCollisionWithDimensionObject(otherDimensionObj);
			}
		}
		else {
			if (!dimensionObject.ShouldCollideWith(otherDimensionObj)) {
				IgnoreCollisionWithDimensionObject(otherDimensionObj);
			}
			else {
				RestoreCollisionWithDimensionObject(otherDimensionObj);
			}
		}
	}

	void IgnoreCollisionWithCollider(Collider other, bool partOfDimensionObject) {
		if (collidersIgnored.ContainsKey(other)) {
			collidersIgnored[other] = collidersIgnored[other] || partOfDimensionObject;
			return;
		}
		
		foreach (var thisCollider in dimensionObject.colliders) {
			Physics.IgnoreCollision(thisCollider, other, true);
		}
		collidersIgnored.Add(other, partOfDimensionObject);
	}

	void IgnoreCollisionWithDimensionObject(DimensionObject other) {
		if (!dimensionObjectsIgnored.Contains(other)) {
			if (dimensionObject != null) {
				dimensionObject.debug.Log($"{dimensionObject.name} ignoring collision with {other.name}");
			}

			foreach (var otherCollider in other.colliders) {
				IgnoreCollisionWithCollider(otherCollider, true);
			}
			
			other.OnStateChange += DetermineCollisionWithDimensionObject;
			dimensionObjectsIgnored.Add(other);
		}
	}

	void RestoreCollisionWithCollider(Collider other) {
		if (!collidersIgnored.ContainsKey(other)) return;
		
		// Sometimes the object we were ignoring collisions with gets destroyed and thus we need a null check
		if (other != null) {
			foreach (var thisCollider in dimensionObject.colliders) {
				Physics.IgnoreCollision(thisCollider, other, false);
			}
		}
		
		collidersIgnored.Remove(other);
	}

	void RestoreCollisionWithDimensionObject(DimensionObject other) {
		if (dimensionObjectsIgnored.Contains(other)) {
			if (dimensionObject != null) {
				dimensionObject.debug.Log($"{dimensionObject.name} restoring collision with {other.name}");
			}

			foreach (var otherCollider in other.colliders) {
				RestoreCollisionWithCollider(otherCollider);
			}

			other.OnStateChange -= DetermineCollisionWithDimensionObject;
			dimensionObjectsIgnored.Remove(other);
		}
	}

	void OnDisable() {
		RestoreAllCollisions();

		if (dimensionObject != null) {
			dimensionObject.OnStateChangeSimple -= OnThisDimensionObjectVisibilityStateChange;
		}
	}

	void RestoreAllCollisions() {
		List<DimensionObject> copyListOfDimensionObjectsIgnored = new List<DimensionObject>(dimensionObjectsIgnored);
		foreach (var otherDimensionObject in copyListOfDimensionObjectsIgnored) {
			RestoreCollisionWithDimensionObject(otherDimensionObject);
		}
	}

	void OnTriggerStay(Collider other) {
		if (dimensionObject == null || collidersIgnored.ContainsKey(other)) return;
		DimensionObject otherDimensionObj = other.FindDimensionObjectRecursively<DimensionObject>();
		if (otherDimensionObj != null) {
			DetermineCollisionWithDimensionObject(otherDimensionObj);
		}
		else {
			DetermineCollisionWithNonDimensionObject(other);
		}
	}
}
