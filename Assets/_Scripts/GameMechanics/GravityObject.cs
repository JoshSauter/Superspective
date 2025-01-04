using System;
using System.Linq;
using GrowShrink;
using NaughtyAttributes;
using PortalMechanics;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UniqueId))]
public class GravityObject : SuperspectiveObject<GravityObject, GravityObject.GravityObjectSave> {
    private const float RAY_LENGTH = .025f;
    private const float ON_GROUND_CHECK_THRESHOLD = -.1f;
    private float RayLength => RAY_LENGTH * Scale;
    private float OnGroundCheckThreshold => ON_GROUND_CHECK_THRESHOLD * Scale;
    
    public GrowShrinkObject growShrink;
    public Rigidbody thisRigidbody;
    public BoxCollider thisCollider;
    public bool useGravity = true;
    public bool onGround = false;
    [SerializeField]
    private Vector3 startingGravityDirection = Vector3.down;

    public Quaternion GravityRotation { get; private set; } = Quaternion.identity;
    [ShowNativeProperty]
    public Vector3 GravityDirection {
        get => GravityRotation * Vector3.down;
        set => GravityRotation = Quaternion.FromToRotation(Vector3.down, value.normalized);
    }
    public float gravityMagnitude = SuperspectivePhysics.originalGravity.magnitude;

    private int RaycastLayermask => SuperspectivePhysics.PhysicsRaycastLayerMask;

    private PickupObject _pickupObject;
    private PickupObject PickupObject => _pickupObject ??= GetComponent<PickupObject>();

    private GrowShrinkObject _growShrinkObject;
    private GrowShrinkObject GrowShrinkObject => _growShrinkObject ??= GetComponent<GrowShrinkObject>();
    private float Scale => GrowShrinkObject ? GrowShrinkObject.CurrentScale : 1f;

    [ShowNativeProperty]
    private Vector3 CurrentVelocity {
        get {
            if (thisRigidbody == null) return Vector3.zero;
            return thisRigidbody.velocity;
        }
    }

    protected override void Awake() {
        base.Awake();

        
        if (thisRigidbody == null) thisRigidbody = GetComponent<Rigidbody>();
        if (thisCollider == null) thisCollider = GetComponent<BoxCollider>();
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
            onGround = IsOnGround();
            
            thisRigidbody.AddForce(GravityDirection * gravityMagnitude / (onGround ? Mathf.Max(1, Scale) : 1f), ForceMode.Acceleration);
        }
        else {
            onGround = false;
        }
    }
    
    /// <summary>
    /// Checks if the cube is on the ground by raycasting from its center and corners.
    /// Assumes that the GravityObject is a cube... so maybe revisit if that changes.
    /// </summary>
    /// <returns>True if the cube is on the ground, otherwise false.</returns>
    public bool IsOnGround() {
        if (thisCollider == null || !thisCollider.enabled || thisRigidbody == null || thisRigidbody.isKinematic) {
            return false;
        }

        Vector3 curVelocity = CurrentVelocity;
        if (Vector3.Dot(curVelocity, GravityDirection) <= OnGroundCheckThreshold) {
            return false;
        }
        
        Vector3 nextPos = thisRigidbody.position + curVelocity * Time.fixedDeltaTime + (GravityDirection * RayLength);

        // Half-extents of the box
        Vector3 boxHalfExtents = transform.lossyScale.ComponentMultiply(thisCollider.size * 0.5f);

        int tempLayer = thisCollider.gameObject.layer;
        thisCollider.gameObject.layer = SuperspectivePhysics.IgnoreRaycastLayer;
        
        // Perform the BoxCast
        Collider[] result = Physics.OverlapBox(
            nextPos,
            boxHalfExtents,
            transform.rotation, // No rotation for the BoxCast
            RaycastLayermask
        );
        onGround = result.Length > 0;

        if (onGround) {
            debug.Log($"{string.Join("\n", result.Select(c => c.FullPath()))}");
        }
        
        thisCollider.gameObject.layer = tempLayer;

        if (onGround) {
            debug.Log("On ground");
        }
        else {
            debug.Log("Not on ground");
        }

        return onGround;
    }
    
    void OnDrawGizmos() {
        // Visualize the BoxCast in the editor
        if (thisCollider == null || !thisCollider.enabled || thisRigidbody == null || thisRigidbody.isKinematic) return;

        Gizmos.color = Color.green;

        Vector3 curVelocity = CurrentVelocity;
        if (Vector3.Dot(curVelocity, GravityDirection) <= OnGroundCheckThreshold) {
            return;
        }

        Vector3 nextPos = thisRigidbody.position + curVelocity * Time.fixedDeltaTime + (GravityDirection * RayLength);
        Vector3 boxHalfExtents = transform.lossyScale.ComponentMultiply(thisCollider.size * 0.5f);

        // Set up gizmos for rotation
        Gizmos.matrix = Matrix4x4.TRS(nextPos, transform.rotation, Vector3.one);

        // Draw the box
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
    }

    void ReorientGravityAfterPortaling(Portal inPortal) {
        GravityRotation = inPortal.TransformRotation(GravityRotation);
    }

#region Saving

    public override void LoadSave(GravityObjectSave save) {
        useGravity = save.useGravity;
        GravityDirection = save.gravityDirection;
        gravityMagnitude = save.gravityMagnitude;
        onGround = save.onGround;
    }

    [Serializable]
    public class GravityObjectSave : SaveObject<GravityObject> {
        public SerializableVector3 gravityDirection;
        public float gravityMagnitude;
        public bool useGravity;
        public bool onGround;

        public GravityObjectSave(GravityObject obj) : base(obj) {
            useGravity = obj.useGravity;
            gravityDirection = obj.GravityDirection;
            gravityMagnitude = obj.gravityMagnitude;
            onGround = obj.onGround;
        }
    }
#endregion
}