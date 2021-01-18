using System;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UniqueId))]
public class GravityObject : MonoBehaviour, SaveableObject {
    public Rigidbody thisRigidbody;
    public bool useGravity = true;
    public Vector3 gravityDirection = Physics.gravity.normalized;
    public float gravityMagnitude = Physics.gravity.magnitude;
    UniqueId _id;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    void Awake() {
        thisRigidbody = GetComponent<Rigidbody>();
    }

    void Start() {
        thisRigidbody.useGravity = false;

        PortalableObject portalableObject = GetComponent<PortalableObject>();
        if (portalableObject != null) portalableObject.OnObjectTeleported += ReorientGravityAfterPortaling;
    }

    void FixedUpdate() {
        if (useGravity) thisRigidbody.AddForce(gravityDirection * gravityMagnitude, ForceMode.Acceleration);
    }

    public void ReorientGravityAfterPortaling(Portal inPortal) {
        gravityDirection = inPortal.TransformDirection(gravityDirection);
    }

#region Saving
    public bool SkipSave { get; set; }

    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public string ID => $"GravityObject_{id.uniqueId}";

    [Serializable]
    class GravityObjectSave {
        SerializableVector3 gravityDirection;
        float gravityMagnitude;
        bool useGravity;

        public GravityObjectSave(GravityObject obj) {
            useGravity = obj.useGravity;
            gravityDirection = obj.gravityDirection;
            gravityMagnitude = obj.gravityMagnitude;
        }

        public void LoadSave(GravityObject obj) {
            obj.useGravity = useGravity;
            obj.gravityDirection = gravityDirection;
            obj.gravityMagnitude = gravityMagnitude;
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