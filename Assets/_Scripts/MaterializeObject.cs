using System;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

// TODO: Change the simple localScale modification to a dissolve shader
[RequireComponent(typeof(UniqueId))]
public class MaterializeObject : SaveableObject<MaterializeObject, MaterializeObject.MaterializeObjectSave> {
    public delegate void MaterializeAction();

    public enum State {
        Materializing,
        Chilling,
        Dematerializing,
        Dematerialized
    }

    public bool destroyObjectOnDematerialize = true;
    public float materializeTime = .75f;
    public float dematerializeTime = .5f;

    public AnimationCurve animCurve;
    UniqueId _id;

    State _state;

    //Renderer[] allRenderers;
    Collider[] allColliders;
    Vector3 startScale;

    PickupObject thisPickupObj;
    float timeSinceStateChange;

    UniqueId id {
        get {
            if (_id == null) _id = GetComponent<UniqueId>();
            return _id;
        }
    }

    public State state {
        get => _state;
        set {
            if (_state == value) return;
            timeSinceStateChange = 0f;
            switch (value) {
                case State.Materializing:
                    OnMaterializeStart?.Invoke();
                    break;
                case State.Chilling:
                    OnMaterializeEnd?.Invoke();
                    foreach (Collider c in allColliders) {
                        c.enabled = true;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) rigidbody.isKinematic = false;
                    }

                    break;
                case State.Dematerializing:
                    OnDematerializeStart?.Invoke();
                    thisPickupObj.Drop();
                    foreach (Collider c in allColliders) {
                        c.enabled = false;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) rigidbody.isKinematic = true;
                    }

                    break;
                case State.Dematerialized:
                    OnDematerializeEnd?.Invoke();
                    if (destroyObjectOnDematerialize) {
                        GetComponent<DynamicObject>().Destroy();
                    }
                    break;
            }

            _state = value;
        }
    }

    protected override void Awake() {
        base.Awake();
        thisPickupObj = GetComponent<PickupObject>();

        startScale = transform.localScale;
        //allRenderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform);
        allColliders = transform.GetComponentsInChildrenRecursively<Collider>();
    }

    void Update() {
        UpdateMaterialize();
    }

    public event MaterializeAction OnMaterializeStart;
    public event MaterializeAction OnMaterializeEnd;
    public event MaterializeAction OnDematerializeStart;
    public event MaterializeAction OnDematerializeEnd;

    void UpdateMaterialize() {
        timeSinceStateChange += Time.deltaTime;
        switch (state) {
            case State.Chilling:
                break;
            case State.Materializing:
                if (timeSinceStateChange < materializeTime) {
                    float t = timeSinceStateChange / materializeTime;

                    transform.localScale = animCurve.Evaluate(t) * startScale;
                }
                else {
                    transform.localScale = startScale;

                    foreach (Collider c in allColliders) {
                        c.enabled = true;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) rigidbody.isKinematic = false;
                    }

                    state = State.Chilling;
                }

                break;
            case State.Dematerializing:
                if (timeSinceStateChange < dematerializeTime) {
                    float t = timeSinceStateChange / dematerializeTime;

                    transform.localScale = animCurve.Evaluate(1 - t) * startScale;
                }
                else {
                    transform.localScale = Vector3.zero;
                    state = State.Dematerialized;
                }

                break;
            case State.Dematerialized:
                break;
        }
    }

    public void Materialize() {
        state = State.Materializing;
    }

    public void Dematerialize() {
        state = State.Dematerializing;
    }

#region Saving
    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public override string ID => $"MaterializeObject_{id.uniqueId}";

    [Serializable]
    public class MaterializeObjectSave : SerializableSaveObject<MaterializeObject> {
        SerializableAnimationCurve animCurve;
        SerializableVector3 curScale;
        float dematerializeTime;
        bool destroyObjectOnDematerialize;
        float materializeTime;
        SerializableVector3 startScale;
        State state;
        float timeSinceStateChange;

        public MaterializeObjectSave(MaterializeObject materialize) : base(materialize) {
            state = materialize.state;
            timeSinceStateChange = materialize.timeSinceStateChange;
            destroyObjectOnDematerialize = materialize.destroyObjectOnDematerialize;
            materializeTime = materialize.materializeTime;
            dematerializeTime = materialize.dematerializeTime;
            animCurve = materialize.animCurve;
            startScale = materialize.startScale;
            curScale = materialize.transform.localScale;
        }

        public override void LoadSave(MaterializeObject materialize) {
            materialize.state = state;
            materialize.timeSinceStateChange = timeSinceStateChange;
            materialize.destroyObjectOnDematerialize = destroyObjectOnDematerialize;
            materialize.materializeTime = materializeTime;
            materialize.dematerializeTime = dematerializeTime;
            materialize.animCurve = animCurve;
            materialize.startScale = startScale;
            materialize.transform.localScale = curScale;
        }
    }
#endregion
}