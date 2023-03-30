using System;
using GrowShrink;
using PortalMechanics;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UniqueId))]
public class GravityObject : SaveableObject<GravityObject, GravityObject.GravityObjectSave> {
    public GrowShrinkObject growShrink;
    public Rigidbody thisRigidbody;
    public bool useGravity = true;
    public Vector3 gravityDirection = Physics.gravity.normalized;
    public float gravityMagnitude = Physics.gravity.magnitude;

    private int raycastLayermask => ~LayerMask.GetMask("Player");

    protected override void Awake() {
        base.Awake();
        thisRigidbody = GetComponent<Rigidbody>();
    }

    protected override void Start() {
        base.Start();
        thisRigidbody.useGravity = false;

        PortalableObject portalableObject = GetComponent<PortalableObject>();
        if (portalableObject != null) portalableObject.OnObjectTeleported += ReorientGravityAfterPortaling;
        if (growShrink == null) growShrink = GetComponent<GrowShrinkObject>();
    }

    void FixedUpdate() {
        if (useGravity) {
            Vector3 rayDirection = gravityDirection * (0.5f * transform.localScale.x);
            rayDirection += gravityDirection * (0.05f * transform.localScale.x);
            Ray groundRaycast = new Ray(transform.position, rayDirection);
            bool onGround = Physics.Raycast(groundRaycast, rayDirection.magnitude, raycastLayermask);
            Debug.DrawRay(groundRaycast.origin, rayDirection, onGround ? Color.green : Color.yellow);
            thisRigidbody.AddForce(gravityDirection * gravityMagnitude / (onGround && growShrink != null ? growShrink.currentScale : 1f), ForceMode.Acceleration);
        }
    }

    public void ReorientGravityAfterPortaling(Portal inPortal) {
        gravityDirection = inPortal.TransformDirection(gravityDirection);
    }

#region Saving

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