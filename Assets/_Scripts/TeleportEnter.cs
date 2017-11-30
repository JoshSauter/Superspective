using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MagicTrigger))]
public class TeleportEnter : MonoBehaviour {
    MagicTrigger trigger;
    Teleport parent;
    public delegate void TeleportAction(Vector3 teleportDistance);
    public event TeleportAction OnTeleport;
    public static event TeleportAction OnAnyTeleport;

    public Vector2 groundTextureDisplacement;

	// Use this for initialization
	void Awake () {
        parent = GetComponentInParent<Teleport>();
        trigger = GetComponent<MagicTrigger>();
        trigger.OnMagicTriggerStay += TeleportTriggered;
	}

    private void OnDisable() {
        trigger.OnMagicTriggerStay -= TeleportTriggered;
    }

    private void TeleportTriggered(Collider other) {
        Vector3 displacement = parent.enter.transform.position - parent.exit.transform.position;
        other.transform.position -= displacement;

        if (OnTeleport != null) {
            OnTeleport(displacement);
        }
        if (OnAnyTeleport != null) {
            OnAnyTeleport(displacement);
        }
    }
}
