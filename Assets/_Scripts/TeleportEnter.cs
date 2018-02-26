using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MagicTrigger))]
public class TeleportEnter : MonoBehaviour {
	public bool DEBUG = false;
    public MagicTrigger trigger;
    Teleport parent;
	public delegate void TeleportAction(Teleport teleporter, Collider player);
    public event TeleportAction OnTeleport;
    public static event TeleportAction OnAnyTeleport;

	// Use this for initialization
	void Awake () {
        parent = GetComponentInParent<Teleport>();

        trigger = GetComponent<MagicTrigger>();
	}

	private void OnEnable() {
		trigger.OnMagicTriggerStayOneTime += TeleportTriggered;
	}

    private void OnDisable() {
        trigger.OnMagicTriggerStayOneTime -= TeleportTriggered;
    }

    private void TeleportTriggered(Collider other) {
		// Handle velocity
		Vector3 curVelocity = other.GetComponent<Rigidbody>().velocity;
		Vector3 relativeVelocity = parent.enter.transform.InverseTransformDirection(curVelocity);
		curVelocity = parent.exit.transform.TransformDirection(relativeVelocity);
		if (DEBUG) {
			print("Velocity was " + other.GetComponent<Rigidbody>().velocity + " but is now " + curVelocity);
		}
		other.GetComponent<Rigidbody>().velocity = curVelocity;

		// Handle position and rotation
		Vector3 teleportDisplacement = parent.enter.transform.position - parent.exit.transform.position;
		Vector3 displacementToCenter = parent.enter.transform.position - other.transform.position;
		float rotationBetweenEnterExit = GetRotationAngleBetweenTeleporters(parent);
		if (DEBUG) {
			print("Displacement: " + teleportDisplacement + "\nDisplacementToCenter: " + displacementToCenter + "\nAngleBetweenEnterExit: " + rotationBetweenEnterExit);
		}

		other.transform.position += displacementToCenter;
		// Note: This only works for Y-axis rotations
		other.transform.Rotate(new Vector3(0, rotationBetweenEnterExit, 0));

		other.transform.position -= teleportDisplacement;
		other.transform.position -= parent.exit.transform.TransformVector(parent.enter.transform.TransformVector(displacementToCenter));

		foreach (Transform otherObject in parent.otherObjectsToTeleport) {
			otherObject.transform.position += displacementToCenter;
			otherObject.transform.Rotate(new Vector3(0, rotationBetweenEnterExit, 0));
			otherObject.transform.position -= teleportDisplacement;
			otherObject.transform.position -= parent.exit.transform.TransformVector(parent.enter.transform.TransformVector(displacementToCenter));
		}

        if (OnTeleport != null) {
			OnTeleport(parent, other);
        }
        if (OnAnyTeleport != null) {
            OnAnyTeleport(parent, other);
        }
    }

	public static float GetRotationAngleBetweenTeleporters(Teleport teleporter) {
		Vector3 forwardEnter = teleporter.enter.transform.rotation * Vector3.forward;
		Vector3 forwardExit = teleporter.exit.transform.rotation * Vector3.forward;
		float angleEnter = Mathf.Rad2Deg * Mathf.Atan2(forwardEnter.x, forwardEnter.z);
		float angleExit = Mathf.Rad2Deg * Mathf.Atan2(forwardExit.x, forwardExit.z);
		return Mathf.DeltaAngle(angleEnter, angleExit);
	}
	
}
