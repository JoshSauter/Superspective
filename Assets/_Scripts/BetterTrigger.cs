using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Saving;
using UnityEngine;

// A trigger which will actually trigger OnTriggerEnter/Exit when an object is teleported into or out of it
[RequireComponent(typeof(Collider))]
public class BetterTrigger : MonoBehaviour {
    private Collider trigger;
    private HashSet<Collider> collidersInZone = new HashSet<Collider>();

    public List<BetterTriggers> listeners = new List<BetterTriggers>();

    // Start is called before the first frame update
    void Start() {
        trigger = GetComponent<Collider>();

        if (listeners == null || listeners.Count == 0) {
            listeners = GetComponents<MonoBehaviour>().OfType<BetterTriggers>().ToList();
        }

        trigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    
    private void OnEnable() {
        SaveManager.BeforeLoad += ClearCollidersInZone;
    }

    private void OnDisable() {
        SaveManager.BeforeLoad -= ClearCollidersInZone;
    }

    private void ClearCollidersInZone() {
        collidersInZone.Clear();
    }

    private void FixedUpdate() {
        if (GameManager.instance.IsCurrentlyLoading || !GameManager.instance.gameHasLoaded) return;
        // Check the positions of each collider we're tracking to see if we need to trigger OnBetterTriggerExit
        
        // Don't remove elements from the collection while we are iterating over it, remove them in bulk afterwards
        HashSet<Collider> collidersToRemove = new HashSet<Collider>();
        foreach (var c in collidersInZone) {
            if (c == null || !IsInTriggerZone(c)) {
                collidersToRemove.Add(c);
            }
        }

        // Remove any colliders no longer in contact w/ the trigger zone, and trigger OnBetterTriggerExit for each
        collidersInZone.RemoveWhere(collidersToRemove.Contains);
        foreach (var colliderRemoved in collidersToRemove) {
            if (colliderRemoved == null) continue;

            foreach (var listener in listeners) {
                listener.OnBetterTriggerExit(colliderRemoved);
            }
        }
    }

    private void OnTriggerStay(Collider other) {
        if (IsInTriggerZone(other)) {
            // Whatever is triggering OnTriggerStay is ACTUALLY still colliding with us
            
            if (!collidersInZone.Contains(other)) {
                // We aren't tracking this object yet, so add it to our list and trigger OnBetterTriggerEnter for it
                AddCollider(other);
            }
            // I don't want to have OnBetterTriggerEnter on the same physics frame as an OnBetterTriggerStay, but if you do you can remove the else here:
            else {
                // We're already aware of this object, and it's actually in the zone, so trigger OnBetterTriggerStay for it
                foreach (var listener in listeners) {
                    listener.OnBetterTriggerStay(other);
                }
            }
        }
        else if (collidersInZone.Contains(other)) {
            // Unity dun fooled us, this collider isn't actually in contact with us still! Remove it and trigger OnBetterTriggerExit
            RemoveCollider(other);
        }
    }

    private void RemoveCollider(Collider c) {
        collidersInZone.Remove(c);
        foreach (var listener in listeners) {
            listener.OnBetterTriggerExit(c);
        }
    }

    void AddCollider(Collider c) {
        collidersInZone.Add(c);
        foreach (var listener in listeners) {
            listener.OnBetterTriggerEnter(c);
        }
    }

    bool IsInTriggerZone(Collider c) {
        return SuperspectivePhysics.CollidersOverlap(trigger, c);
    }
}

// Anything that wants better triggers simply inherits this interface and implements the methods as though they were Unity Event Functions
public interface BetterTriggers {
    void OnBetterTriggerEnter(Collider c);
    void OnBetterTriggerExit(Collider c);
    void OnBetterTriggerStay(Collider c);
}
