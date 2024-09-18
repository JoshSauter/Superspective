using SuperspectiveUtils;
using UnityEngine;

public class FreezeRigidbodyWhenPlayerIsNear : MonoBehaviour, BetterTriggers {
    public MultiDimensionCube multiDimensionCube;

    public void OnBetterTriggerEnter(Collider c) {
        if (c.TaggedAsPlayer() && multiDimensionCube.thisCollider.enabled) {
            multiDimensionCube.pickupCube.freezeRigidbodyDueToNearbyPlayer = true;
        }
    }

    public void OnBetterTriggerExit(Collider c) {
        if (c.TaggedAsPlayer() && multiDimensionCube.thisCollider.enabled) {
            multiDimensionCube.pickupCube.freezeRigidbodyDueToNearbyPlayer = false;
        }
    }

    public void OnBetterTriggerStay(Collider c) {}
}