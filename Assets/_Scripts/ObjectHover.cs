using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectHover : SaveableObject<ObjectHover, ObjectHover.ObjectHoverSave> {
    const float hoveringPauseLerp = 0.1f;
    public bool useLocalCoordinates = true;
    public float maxDisplacementUp = 0.125f;
    public float maxDisplacementForward;
    public float maxDisplacementRight;
    public float period = 1f;
    public bool hoveringPaused;
    UniqueId _id;

    Vector3 displacementCounter = Vector3.zero;
    Vector3 forward;
    Vector3 right;

    float timeElapsed;
    Vector3 up;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    // Use this for initialization
    protected override void Awake() {
        base.Awake();
        up = useLocalCoordinates ? transform.up : Vector3.up;
        forward = useLocalCoordinates ? transform.forward : Vector3.forward;
        right = useLocalCoordinates ? transform.right : Vector3.right;
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (!hoveringPaused) {
            timeElapsed += Time.fixedDeltaTime;
            float t = Time.fixedDeltaTime * Mathf.Cos(Mathf.PI * 2 * timeElapsed / period);
            Vector3 displacementUp = maxDisplacementUp * t * up;
            Vector3 displacementForward = maxDisplacementForward * t * forward;
            Vector3 displacementRight = maxDisplacementRight * t * right;
            Vector3 displacementVector = displacementUp + displacementForward + displacementRight;
            displacementCounter += displacementVector;
            transform.position += displacementVector;
        }
        else {
            Vector3 thisFrameMovement = -hoveringPauseLerp * displacementCounter;
            transform.position += thisFrameMovement;
            displacementCounter += thisFrameMovement;
        }
    }

#region Saving
    public override string ID => $"ObjectHover_{id.uniqueId}";

    [Serializable]
    public class ObjectHoverSave : SerializableSaveObject<ObjectHover> {
        SerializableVector3 displacementCounter;
        SerializableVector3 forward;
        bool hoveringPaused;
        float maxDisplacementForward;
        float maxDisplacementRight;
        float maxDisplacementUp;
        float period;

        SerializableVector3 position;
        SerializableVector3 right;

        float timeElapsed;
        SerializableVector3 up;
        bool useLocalCoordinates;

        public ObjectHoverSave(ObjectHover script) : base(script) {
            useLocalCoordinates = script.useLocalCoordinates;
            maxDisplacementUp = script.maxDisplacementUp;
            maxDisplacementForward = script.maxDisplacementForward;
            maxDisplacementRight = script.maxDisplacementRight;
            period = script.period;
            up = script.up;
            forward = script.forward;
            right = script.right;
            position = script.transform.position;
            displacementCounter = script.displacementCounter;
            hoveringPaused = script.hoveringPaused;
            timeElapsed = script.timeElapsed;
        }

        public override void LoadSave(ObjectHover script) {
            script.useLocalCoordinates = useLocalCoordinates;
            script.maxDisplacementUp = maxDisplacementUp;
            script.maxDisplacementForward = maxDisplacementForward;
            script.maxDisplacementRight = maxDisplacementRight;
            script.period = period;
            script.up = up;
            script.forward = forward;
            script.right = right;
            script.transform.position = position;
            script.displacementCounter = displacementCounter;
            script.hoveringPaused = hoveringPaused;
            script.timeElapsed = timeElapsed;
        }
    }
#endregion
}