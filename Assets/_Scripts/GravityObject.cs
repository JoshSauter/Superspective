using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PortalMechanics;
using Saving;
using System;
using SerializableClasses;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UniqueId))]
public class GravityObject : MonoBehaviour, SaveableObject {
	UniqueId _id;
	UniqueId id {
		get {
			if (_id == null) {
				_id = GetComponent<UniqueId>();
			}
			return _id;
		}
	}
	public Rigidbody thisRigidbody;
	public bool useGravity = true;
	public Vector3 gravityDirection = Physics.gravity.normalized;
	public float gravityMagnitude = Physics.gravity.magnitude;

	void Awake() {
		thisRigidbody = GetComponent<Rigidbody>();
	}

    void Start() {
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

	#region Saving
	public bool SkipSave { get; set; }
	// All components on PickupCubes share the same uniqueId so we need to qualify with component name
	public string ID => $"GravityObject_{id.uniqueId}";

	[Serializable]
	class GravityObjectSave {
		bool useGravity;
		SerializableVector3 gravityDirection;
		float gravityMagnitude;

		public GravityObjectSave(GravityObject obj) {
			this.useGravity = obj.useGravity;
			this.gravityDirection = obj.gravityDirection;
			this.gravityMagnitude = obj.gravityMagnitude;
		}

		public void LoadSave(GravityObject obj) {
			obj.useGravity = this.useGravity;
			obj.gravityDirection = this.gravityDirection;
			obj.gravityMagnitude = this.gravityMagnitude;
		}
	}

	public object GetSaveObject() {
		return new GravityObjectSave(this);
	}

	public void LoadFromSavedObject(object savedObject) {
		GravityObjectSave save = savedObject as GravityObjectSave;

		save.LoadSave(this);
	}
	#endregion
}
