using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MagicTrigger))]
public class TeleportEnter : MonoBehaviour {
	public bool DEBUG = false;
	public bool teleportPlayer = true;
    public MagicTrigger trigger;
	public Collider teleportEnter;
    public Collider teleportExit;
	public Vector3 teleportOffset = Vector3.zero;
	public Transform[] otherObjectsToTeleport;

#region events
	public delegate void TeleportAction(Collider teleportEnter, Collider teleportExit, Collider player);
	public delegate void SimpleTeleportAction();
    public event TeleportAction OnTeleport;
	public event SimpleTeleportAction OnTeleportSimple;
    public static event TeleportAction OnAnyTeleport;
	public static event SimpleTeleportAction OnAnyTeleportSimple;
#endregion

	// Use this for initialization
	void Awake () {
		teleportEnter = GetComponent<Collider>();
        trigger = GetComponent<MagicTrigger>();

		if (otherObjectsToTeleport == null) otherObjectsToTeleport = new Transform[0];
	}

	private void OnEnable() {
		trigger.OnMagicTriggerStayOneTime += TeleportTriggered;
	}

    private void OnDisable() {
        trigger.OnMagicTriggerStayOneTime -= TeleportTriggered;
    }

	private void TeleportTriggered(Collider other) {
		Debug.Assert(teleportExit != null, "Please specify the teleport exit for " + gameObject.scene.name + ": " + gameObject.name);

		// Handle velocity
		Vector3 curVelocity = other.GetComponent<Rigidbody>().velocity;
		Vector3 relativeVelocity = teleportEnter.transform.InverseTransformDirection(curVelocity);
		curVelocity = teleportExit.transform.TransformDirection(relativeVelocity);
		if (DEBUG) {
			print("Velocity was " + other.GetComponent<Rigidbody>().velocity + " but is now " + curVelocity);
		}
		other.GetComponent<Rigidbody>().velocity = curVelocity;

		// Handle position and rotation
		Vector3 teleportDisplacement = TeleporterDisplacement() + teleportOffset;
		Vector3 displacementToCenter = teleportEnter.transform.position - other.transform.position;
		float rotationBetweenEnterExit = GetRotationAngleBetweenTeleporters();
		if (DEBUG) {
			print("Displacement: " + teleportDisplacement + "\nDisplacementToCenter: " + displacementToCenter + "\nAngleBetweenEnterExit: " + rotationBetweenEnterExit);
		}

		if (teleportPlayer) {
			other.transform.position += displacementToCenter;
			// Note: This only works for Y-axis rotations
			other.transform.Rotate(new Vector3(0, rotationBetweenEnterExit, 0));

			other.transform.position += teleportDisplacement;
			other.transform.position -= teleportExit.transform.TransformDirection(teleportEnter.transform.InverseTransformDirection(displacementToCenter));
		}

		foreach (Transform otherObject in otherObjectsToTeleport) {
			otherObject.transform.position += displacementToCenter;
			otherObject.transform.Rotate(new Vector3(0, rotationBetweenEnterExit, 0));
			otherObject.transform.position += teleportDisplacement;
			otherObject.transform.position -= teleportExit.transform.TransformVector(teleportEnter.transform.TransformVector(displacementToCenter));
		}

		TriggerEvents(other);
    }

	float GetRotationAngleBetweenTeleporters() {
		Vector3 forwardEnter = teleportEnter.transform.rotation * Vector3.forward;
		Vector3 forwardExit = teleportExit.transform.rotation * Vector3.forward;
		float angleEnter = Mathf.Rad2Deg * Mathf.Atan2(forwardEnter.x, forwardEnter.z);
		float angleExit = Mathf.Rad2Deg * Mathf.Atan2(forwardExit.x, forwardExit.z);
		return Mathf.DeltaAngle(angleEnter, angleExit);
	}

	Vector3 TeleporterDisplacement() {
		return teleportExit.transform.position - teleportEnter.transform.position;
	}

	void TriggerEvents(Collider player) {
		if (OnTeleport != null) {
			OnTeleport(teleportEnter, teleportExit, player);
		}
		if (OnAnyTeleport != null) {
			OnAnyTeleport(teleportEnter, teleportExit, player);
		}
		if (OnTeleportSimple != null) {
			OnTeleportSimple();
		}
		if (OnAnyTeleportSimple != null) {
			OnAnyTeleportSimple();
		}
	}

}
