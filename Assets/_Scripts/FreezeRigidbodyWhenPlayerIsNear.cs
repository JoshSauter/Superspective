using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;

public class FreezeRigidbodyWhenPlayerIsNear : MonoBehaviour {
	public MultiDimensionCube pickupCubeDimensionShift;

	void OnTriggerEnter(Collider other) {
		if (other.TaggedAsPlayer() && pickupCubeDimensionShift.thisCollider.enabled) {
			pickupCubeDimensionShift.pickupCube.thisRigidbody.isKinematic = true;
		}
	}
}
