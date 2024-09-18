using System;
using GrowShrink;
using NaughtyAttributes;
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
    [SerializeField]
    private Vector3 startingGravityDirection = Vector3.down;

    public Quaternion GravityRotation { get; private set; } = Quaternion.identity;
    [ShowNativeProperty]
    public Vector3 GravityDirection {
        get => GravityRotation * Vector3.down;
        set => GravityRotation = Quaternion.FromToRotation(Vector3.down, value.normalized);
    }
    public float gravityMagnitude = Physics.gravity.magnitude;

    private int RaycastLayermask => ~SuperspectivePhysics.PlayerLayer;

    private PickupObject _pickupObject;
    private PickupObject PickupObject {
        get {
            if (!_pickupObject) {
                _pickupObject = GetComponent<PickupObject>();
            }

            return _pickupObject;
        }
    }

    [ShowNativeProperty]
    private Vector3 CurrentVelocity {
        get {
            if (thisRigidbody == null) return Vector3.zero;
            return thisRigidbody.velocity;
        }
    }

    protected override void Awake() {
        base.Awake();
        thisRigidbody = GetComponent<Rigidbody>();
        GravityDirection = startingGravityDirection;
    }

    protected override void Start() {
        base.Start();
        thisRigidbody.useGravity = false;

        PortalableObject portalableObject = GetComponent<PortalableObject>();
        if (portalableObject != null) portalableObject.OnObjectTeleported += ReorientGravityAfterPortaling;
        if (growShrink == null) growShrink = GetComponent<GrowShrinkObject>();
    }

    void FixedUpdate() {
        if (GameManager.instance.IsCurrentlyLoading) return;
        
        if (useGravity) {
            Vector3 rayDirection = GravityDirection * (0.5f * transform.localScale.x);
            rayDirection += GravityDirection * (0.05f * transform.localScale.x);
            Ray groundRaycast = new Ray(transform.position, rayDirection);
            bool onGround = Physics.Raycast(groundRaycast, rayDirection.magnitude, RaycastLayermask);
            Debug.DrawRay(groundRaycast.origin, rayDirection, onGround ? Color.green : Color.yellow);
            thisRigidbody.AddForce(GravityDirection * gravityMagnitude / (onGround && growShrink != null ? growShrink.CurrentScale : 1f), ForceMode.Acceleration);
        }
    }

    void ReorientGravityAfterPortaling(Portal inPortal) {
        GravityRotation = inPortal.TransformRotation(GravityRotation);
    }

#region Saving

    [Serializable]
    public class GravityObjectSave : SerializableSaveObject<GravityObject> {
        SerializableVector3 gravityDirection;
        float gravityMagnitude;
        bool useGravity;

        public GravityObjectSave(GravityObject obj) : base(obj) {
            useGravity = obj.useGravity;
            gravityDirection = obj.GravityDirection;
            gravityMagnitude = obj.gravityMagnitude;
        }

        public override void LoadSave(GravityObject obj) {
            obj.useGravity = useGravity;
            obj.GravityDirection = gravityDirection;
            obj.gravityMagnitude = gravityMagnitude;
        }
    }
#endregion
}