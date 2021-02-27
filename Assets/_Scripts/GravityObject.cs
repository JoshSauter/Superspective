using System;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UniqueId))]
public class GravityObject : SaveableObject<GravityObject, GravityObject.GravityObjectSave> {
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

    protected override void Awake() {
        base.Awake();
        thisRigidbody = GetComponent<Rigidbody>();
    }

    protected override void Start() {
        base.Start();
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
    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public override string ID => $"GravityObject_{id.uniqueId}";

    [Serializable]
    public class GravityObjectSave : SerializableSaveObject<GravityObject> {
        SerializableVector3 gravityDirection;
        float gravityMagnitude;
        bool useGravity;

        public GravityObjectSave(GravityObject obj) : base(obj) {
            useGravity = obj.useGravity;
            gravityDirection = obj.gravityDirection;
            gravityMagnitude = obj.gravityMagnitude;
        }

        public override void LoadSave(GravityObject obj) {
            obj.useGravity = useGravity;
            obj.gravityDirection = gravityDirection;
            obj.gravityMagnitude = gravityMagnitude;
        }
    }
#endregion
}