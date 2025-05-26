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
    public readonly Dictionary<Collider, PickupObject> pickupObjectsInZone = new Dictionary<Collider, PickupObject>();
    
    public delegate void ColliderAddedAction(Collider other);
    public event ColliderAddedAction OnColliderAdded;

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
            AddCollider(other);
        }
    }

    public void OnBetterTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = true;
        }
        else {
            AddCollider(other);
        }
    }

    public void OnBetterTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = false;
        }
        else {
            RemoveCollider(other);
        }
    }

    private void AddCollider(Collider c) {
        if (!objectsInZone.Add(c)) return;
        
        if (!rigidbodiesInZone.ContainsKey(c)) {
            Rigidbody maybeRigidbody = c.GetComponentInParent<Rigidbody>();
            if (maybeRigidbody) {
                rigidbodiesInZone.Add(c, maybeRigidbody);
            }
        }
        if (!pickupObjectsInZone.ContainsKey(c)) {
            PickupObject maybePickup = c.transform.FindInParentsRecursively<PickupObject>();
            if (maybePickup) {
                pickupObjectsInZone.Add(c, maybePickup);
            }
        }
        OnColliderAdded?.Invoke(c);
    }

    private void RemoveCollider(Collider c) {
        objectsInZone.Remove(c);
        if (rigidbodiesInZone.ContainsKey(c)) {
            rigidbodiesInZone.Remove(c);
        }
    }
}
