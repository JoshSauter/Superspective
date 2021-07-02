using System;
using System.Collections.Generic;
using UnityEngine;
using SuperspectiveUtils;

public class DimensionObjectCollisions : MonoBehaviour {
	// This is all of the DimensionObjects affecting this object, either on it directly or an ancestor in the hierarchy
	public List<DimensionObject> effectiveDimensionObjects = new List<DimensionObject>();
	public Rigidbody rigidbodyOfObject;
	public Collider colliderOfObject;
	SphereCollider thisTrigger;

	const float triggerZoneSize = 1.5f;
	const float minTriggerRadius = 1;

	List<Collider> collidersBeingIgnored = new List<Collider>();

	void Awake() {
		thisTrigger = GetComponent<SphereCollider>();
	}

	void Start() {
		// Find all DimensionObjects affecting this object above it in the hierarchy
	    void PopulateEffectiveDimensionObjects(Transform curNode) {
		    if (curNode == null) return;

		    DimensionObject dimensionObject = curNode.GetComponent<DimensionObject>();
		    if (dimensionObject != null && !effectiveDimensionObjects.Contains(dimensionObject)) {
			    effectiveDimensionObjects.Add(dimensionObject);
		    }

		    if (curNode.parent != null) {
			    PopulateEffectiveDimensionObjects(curNode.parent);
		    }
	    }
	    
		PopulateEffectiveDimensionObjects(transform);

		if (rigidbodyOfObject == null || colliderOfObject == null) {
			Debug.LogError("Pointless to do collision logic on an object with no rigidbody");
			this.enabled = false;
			return;
		}

		thisTrigger = GetComponent<SphereCollider>();
		SetTriggerZoneSize();
    }

	void IgnoreCollision(Collider collider) {
		if (!collidersBeingIgnored.Contains(collider)) {
			Debug.LogWarning($"{colliderOfObject.name} ignoring collision with {collider.gameObject.name}");
			Physics.IgnoreCollision(colliderOfObject, collider, true);

			collidersBeingIgnored.Add(collider);
		}
	}

	void RestoreCollision(Collider collider) {
		if (collidersBeingIgnored.Contains(collider)) {
			Debug.LogWarning($"{colliderOfObject.name} restoring collision with {collider.gameObject.name}");
			Physics.IgnoreCollision(colliderOfObject, collider, false);

			collidersBeingIgnored.Remove(collider);
		}
	}

	void OnTriggerStay(Collider other) {
		if (effectiveDimensionObjects == null || effectiveDimensionObjects.Count == 0) return;
		DimensionObject otherDimensionObj = Utils.FindDimensionObjectRecursively<DimensionObject>(other.gameObject.transform);
		if (otherDimensionObj != null) {
			int collisionChannel = otherDimensionObj.channel;
			VisibilityState collisionVisibility = otherDimensionObj.visibilityState;

			// Make sure all channels are different or we are in a different visibility state
			if (effectiveDimensionObjects.TrueForAll(d =>
				d.channel != collisionChannel || d.visibilityState != collisionVisibility)) {
				foreach (var colliderToIgnore in otherDimensionObj.colliders) {
					IgnoreCollision(colliderToIgnore);
				}
			}
			else {
				foreach (var colliderToRestore in otherDimensionObj.colliders) {
					RestoreCollision(colliderToRestore);
				}
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
		Vector3 colliderBounds = colliderOfObject.bounds.size;
		float maxBounds = Mathf.Max(colliderBounds.x, colliderBounds.y, colliderBounds.z);
		float maxScale = Mathf.Max(thisTrigger.transform.lossyScale.x, thisTrigger.transform.lossyScale.y, thisTrigger.transform.lossyScale.z, 0.01f);

		thisTrigger.radius = Mathf.Max(minTriggerRadius, maxBounds * triggerZoneSize) / maxScale;
	}
}
