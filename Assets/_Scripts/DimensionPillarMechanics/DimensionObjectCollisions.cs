using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using SuperspectiveUtils;

public class DimensionObjectCollisions : MonoBehaviour {
	public DimensionObject dimensionObject;

	HashSet<DimensionObject> dimensionObjectsIgnored = new HashSet<DimensionObject>();
	HashSet<Collider> collidersIgnored = new HashSet<Collider>();

	[ShowNativeProperty]
	public int dimensionObjectsIgnoredCount => dimensionObjectsIgnored.Count;
	[ShowNativeProperty]
	public int collidersIgnoredCount => collidersIgnored.Count;

	void Start() {
		dimensionObject.OnStateChangeSimple += OnThisDimensionObjectVisibilityStateChange;
	}

	void OnThisDimensionObjectVisibilityStateChange(VisibilityState newState) {
		List<DimensionObject> dimensionObjectsIgnoredCopy = new List<DimensionObject>(dimensionObjectsIgnored);
		foreach (var otherDimensionObject in dimensionObjectsIgnoredCopy) {
			DetermineCollision(otherDimensionObject);
		}

		List<Collider> remainingCollidersCopy = new List<Collider>(collidersIgnored);
		foreach (var collider in remainingCollidersCopy) {
			DetermineCollision(collider);
		}
	}

	void Update() {
		if (dimensionObject == null) {
			Destroy(gameObject);
		}
	}

	void DetermineCollision(Collider other) {
		bool shouldCollide = other.TaggedAsPlayer()
			? dimensionObject.ShouldCollideWithPlayer()
			: dimensionObject.ShouldCollideWithNonDimensionObject();
		
		if (!shouldCollide) {
			IgnoreCollision(other);
		}
		else {
			RestoreCollision(other);
		}
	}

	// Second parameter is unused but matches the event shape so we don't subscribe with an anonymous delegate
	void DetermineCollision(DimensionObject otherDimensionObj, VisibilityState _ = VisibilityState.visible) {
		// Make sure channels are different or we are in a different visibility state
		if (otherDimensionObj.isBeingDestroyed) {
			// If the other DimensionObject is being destroyed, treat it as a non DimensionObject
			if (!dimensionObject.ShouldCollideWithNonDimensionObject()) {
				IgnoreCollision(otherDimensionObj);
			}
			else {
				RestoreCollision(otherDimensionObj);
			}
		}
		else {
			if (!dimensionObject.ShouldCollideWith(otherDimensionObj)) {
				IgnoreCollision(otherDimensionObj);
			}
			else {
				RestoreCollision(otherDimensionObj);
			}
		}
	}

	void IgnoreCollision(Collider other) {
		foreach (var thisCollider in dimensionObject.colliders) {
			Physics.IgnoreCollision(thisCollider, other, true);
		}
		collidersIgnored.Add(other);
	}

	void IgnoreCollision(DimensionObject other) {
		if (!dimensionObjectsIgnored.Contains(other)) {
			if (dimensionObject != null) {
				dimensionObject.debug.Log($"{dimensionObject.name} ignoring collision with {other.name}");
			}

			foreach (var otherCollider in other.colliders) {
				IgnoreCollision(otherCollider);
			}
			
			other.OnStateChange += DetermineCollision;
			dimensionObjectsIgnored.Add(other);
		}
	}

	void RestoreCollision(Collider other) {
		// Sometimes the object we were ignoring collisions with gets destroyed and thus we need a null check
		if (other != null) {
			foreach (var thisCollider in dimensionObject.colliders) {
				Physics.IgnoreCollision(thisCollider, other, false);
			}
		}
		
		collidersIgnored.Remove(other);
	}

	void RestoreCollision(DimensionObject other) {
		if (dimensionObjectsIgnored.Contains(other)) {
			if (dimensionObject != null) {
				dimensionObject.debug.Log($"{dimensionObject.name} restoring collision with {other.name}");
			}

			foreach (var otherCollider in other.colliders) {
				RestoreCollision(otherCollider);
			}

			other.OnStateChange -= DetermineCollision;
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
			RestoreCollision(otherDimensionObject);
		}
	}

	void OnTriggerStay(Collider other) {
		if (dimensionObject == null || collidersIgnored.Contains(other)) return;
		DimensionObject otherDimensionObj = other.FindDimensionObjectRecursively<DimensionObject>();
		if (otherDimensionObj != null) {
			DetermineCollision(otherDimensionObj, otherDimensionObj.visibilityState);
		}
		else {
			DetermineCollision(other);
		}
	}
}
