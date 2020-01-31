using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeReceptacle2 : MonoBehaviour {
	Collider thisCollider;
	PickupObject objectBeingSuspended;
	Rigidbody rigidbodyOfObjectBeingSuspended;

    void Start() {
		thisCollider = GetComponent<Collider>();
    }

    void FixedUpdate() {
		if (objectBeingSuspended != null) {
			rigidbodyOfObjectBeingSuspended.useGravity = false;
			if (!objectBeingSuspended.isHeld) {
				Vector3 center = thisCollider.bounds.center;
				Vector3 objPos = objectBeingSuspended.transform.position;
				rigidbodyOfObjectBeingSuspended.AddForce(750 * (center - objPos), ForceMode.Force);
				rigidbodyOfObjectBeingSuspended.AddTorque(10*Vector3.up, ForceMode.Force);
			}
		}
    }

	void OnTriggerEnter(Collider other) {
		PickupObject movableObject = other.gameObject.GetComponent<PickupObject>();
		if (movableObject != null) {
			StartHoldingObject(movableObject);
		}
	}

	private void OnTriggerExit(Collider other) {
		PickupObject movableObject = other.gameObject.GetComponent<PickupObject>();
		if (movableObject == objectBeingSuspended) {
			ResetState();
		}
	}

	void ResetState() {
		DebugPrintState("ResetState()");
		if (objectBeingSuspended != null) {
			rigidbodyOfObjectBeingSuspended.useGravity = !objectBeingSuspended.isHeld;
			rigidbodyOfObjectBeingSuspended = null;
			objectBeingSuspended = null;
		}
	}

	void StartHoldingObject(PickupObject obj) {
		DebugPrintState("StartHoldingObject(" + obj.gameObject.name + ")");
		if (objectBeingSuspended == null) {
			obj.Drop();
			objectBeingSuspended = obj;
			rigidbodyOfObjectBeingSuspended = obj.thisRigidbody;
			rigidbodyOfObjectBeingSuspended.useGravity = false;
		}
	}

	void DebugPrintState(string methodName) {
		//Debug.LogError("CubeReceptacle2." + methodName + "\nobjectBeingHeld: " + objectBeingSuspended?.gameObject.name);
	}
}
