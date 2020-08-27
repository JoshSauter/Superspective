using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;

[RequireComponent(typeof(Rigidbody))]
public class GravityObject : MonoBehaviour {
	public Rigidbody thisRigidbody;
	public bool useGravity = true;
	public Vector3 gravityDirection = Physics.gravity.normalized;
	public float gravityMagnitude = Physics.gravity.magnitude;

    void Start() {
		thisRigidbody = GetComponent<Rigidbody>();
		thisRigidbody.useGravity = false;

		PortalableObject portalableObject = GetComponent<PortalableObject>();
		if (portalableObject != null) {
			portalableObject.OnObjectTeleported += ReorientGravityAfterPortaling;
		}
    }

	public void ReorientGravityAfterPortaling(Portal inPortal) {
		gravityDirection = inPortal.TransformDirection(gravityDirection);
	}

	private void FixedUpdate() {
		if (useGravity) {
			thisRigidbody.AddForce(gravityDirection * gravityMagnitude, ForceMode.Acceleration);
		}
	}
}
