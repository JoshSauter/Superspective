using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		Vector3 relativeGravity = inPortal.transform.InverseTransformDirection(gravityDirection);
		relativeGravity = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeGravity;
		gravityDirection = inPortal.otherPortal.transform.TransformDirection(relativeGravity);
	}

	private void FixedUpdate() {
		if (useGravity) {
			thisRigidbody.AddForce(gravityDirection * gravityMagnitude, ForceMode.Acceleration);
		}
	}
}
