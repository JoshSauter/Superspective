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
    public readonly HashSet<Collider> objectsInZone = new HashSet<Collider>();
    public readonly Dictionary<Collider, Rigidbody> rigidbodiesInZone = new Dictionary<Collider, Rigidbody>();

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
            if (!rigidbodiesInZone.ContainsKey(other)) {
                Rigidbody maybeRigidbody = other.GetComponentInParent<Rigidbody>();
                if (maybeRigidbody) {
                    rigidbodiesInZone.Add(other, maybeRigidbody);
                }
            }
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
            if (rigidbodiesInZone.ContainsKey(other)) {
                rigidbodiesInZone.Remove(other);
            }
        }
    }
}
