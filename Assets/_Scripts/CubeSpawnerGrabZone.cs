using System;
using System.Collections;
using System.Collections.Generic;
using SuperspectiveUtils;
using UnityEngine;

// Sets a CubeSpawner's cube's visibility state to Visible while the player is inside the trigger zone
// This causes the interactability of the object to turn on by DimensionObject.SetInteractability
public class CubeSpawnerGrabZone : MonoBehaviour {
    [SerializeField]
    bool playerInZone = false;
    [SerializeField]
    bool _cubeInteractable = false;
    bool cubeInteractable {
        get { return _cubeInteractable; }
        set {
            if (value != _cubeInteractable) {
                if (cubeSpawner.cubeSpawned != null) {
                    cubeSpawner.cubeSpawned.interactable = value;
                    cubeSpawner.cubeSpawned.gameObject.layer =
                        LayerMask.NameToLayer(value ? "VisibleButNoPlayerCollision" : "Ignore Raycast");

                    _cubeInteractable = value;
                }
            }
        }
    }
    public CubeSpawnerNew cubeSpawner;

    void Update() {
        if (cubeSpawner.cubeSpawned != null) {
            cubeInteractable = playerInZone;
        }
    }

    void OnTriggerStay(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = true;
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.TaggedAsPlayer()) {
            playerInZone = false;
        }
    }
}
