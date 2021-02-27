using SuperspectiveUtils;
using UnityEngine;

public class FreezeRigidbodyWhenPlayerIsNear : MonoBehaviour {
    public MultiDimensionCube multiDimensionCube;

    void OnTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer() && multiDimensionCube.thisCollider.enabled)
            multiDimensionCube.pickupCube.thisRigidbody.isKinematic = true;
    }

    void OnTriggerExit(Collider other) {
        if (other.TaggedAsPlayer() && multiDimensionCube.thisCollider.enabled)
            multiDimensionCube.pickupCube.thisRigidbody.isKinematic = false;
    }
}