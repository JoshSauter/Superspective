using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

/// <summary>
/// Keeps track of whether or not the player is in the zone and what objects are in the zone.
/// Used for elevators and other interactables that need to know what objects are in a given trigger zone.
/// </summary>
[RequireComponent(typeof(Collider), typeof(BetterTrigger))]
public class TriggerOverlapZone : MonoBehaviour, BetterTriggers {
    public bool playerInZone;
    public HashSet<Collider> objectsInZone = new HashSet<Collider>();

    private void Awake() {
        gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;
        if (TryGetComponent(out MeshCollider mc)) {
            mc.convex = true;
        }
        GetComponent<Collider>().isTrigger = true;
    }

    public void OnBetterTriggerEnter(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = true;
        }
        else {
            objectsInZone.Add(other);
        }
    }

    public void OnBetterTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = true;
        }
        else {
            objectsInZone.Add(other);
        }
    }

    public void OnBetterTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = false;
        }
        else {
            objectsInZone.Remove(other);
        }
    }
}
