using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using EpitaphUtils.ShaderUtils;
using Saving;
using System;
using SerializableClasses;

// TODO: Change the simple localScale modification to a dissolve shader
[RequireComponent(typeof(UniqueId))]
public class MaterializeObject : MonoBehaviour, SaveableObject {
    UniqueId _id;
    UniqueId id {
        get {
            if (_id == null) {
                _id = GetComponent<UniqueId>();
            }
            return _id;
        }
    }

    public enum State {
        Materializing,
        Chilling,
        Dematerializing,
        Dematerialized
	}
    private State _state;
    public State state {
        get { return _state; }
        set {
            if (_state == value) {
                return;
			}
            timeSinceStateChange = 0f;
            switch (value) {
                case State.Materializing:
                    OnMaterializeStart?.Invoke();
                    break;
                case State.Chilling:
                    OnMaterializeEnd?.Invoke();
                    foreach (var c in allColliders) {
                        c.enabled = true;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) {
                            rigidbody.isKinematic = false;
                        }
                    }
                    break;
                case State.Dematerializing:
                    OnDematerializeStart?.Invoke();
                    thisPickupObj.Drop();
                    foreach (var c in allColliders) {
                        c.enabled = false;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) {
                            rigidbody.isKinematic = true;
                        }
                    }
                    break;
                case State.Dematerialized:
                    OnDematerializeEnd?.Invoke();
                    if (destroyObjectOnDematerialize) {
                        Destroy(gameObject);
					}
                    break;
			}
            _state = value;
        }
    }
    float timeSinceStateChange = 0f;
    public bool destroyObjectOnDematerialize = true;
    public float materializeTime = .75f;
    public float dematerializeTime = .5f;
    //Renderer[] allRenderers;
    Collider[] allColliders;

    PickupObject thisPickupObj;

    public delegate void MaterializeAction();
    public event MaterializeAction OnMaterializeStart;
    public event MaterializeAction OnMaterializeEnd;
    public event MaterializeAction OnDematerializeStart;
    public event MaterializeAction OnDematerializeEnd;

    public AnimationCurve animCurve;
    Vector3 startScale;

    void Awake() {
        thisPickupObj = GetComponent<PickupObject>();

        startScale = transform.localScale;
        //allRenderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform);
        allColliders = Utils.GetComponentsInChildrenRecursively<Collider>(transform);
    }

    private void Update() {
        UpdateMaterialize();
    }

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

                    foreach (var c in allColliders) {
                        c.enabled = true;
                        Rigidbody rigidbody = c.GetComponent<Rigidbody>();
                        if (rigidbody != null) {
                            rigidbody.isKinematic = false;
                        }
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
    public bool SkipSave { get; set; }
    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public string ID => $"MaterializeObject_{id.uniqueId}";

    [Serializable]
    class MaterializeObjectSave {
        State state;
        float timeSinceStateChange;
        bool destroyObjectOnDematerialize;
        float materializeTime;
        float dematerializeTime;

        SerializableAnimationCurve animCurve;
        SerializableVector3 startScale;
        SerializableVector3 curScale;

        public MaterializeObjectSave(MaterializeObject materialize) {
            this.state = materialize.state;
            this.timeSinceStateChange = materialize.timeSinceStateChange;
            this.destroyObjectOnDematerialize = materialize.destroyObjectOnDematerialize;
            this.materializeTime = materialize.materializeTime;
            this.dematerializeTime = materialize.dematerializeTime;
            this.animCurve = materialize.animCurve;
            this.startScale = materialize.startScale;
            this.curScale = materialize.transform.localScale;
        }

        public void LoadSave(MaterializeObject materialize) {
            materialize.state = this.state;
            materialize.timeSinceStateChange = this.timeSinceStateChange;
            materialize.destroyObjectOnDematerialize = this.destroyObjectOnDematerialize;
            materialize.materializeTime = this.materializeTime;
            materialize.dematerializeTime = this.dematerializeTime;
            materialize.animCurve = this.animCurve;
            materialize.startScale = this.startScale;
            materialize.transform.localScale = this.curScale;
        }
    }

    public object GetSaveObject() {
        return new MaterializeObjectSave(this);
    }

    public void LoadFromSavedObject(object savedObject) {
        MaterializeObjectSave save = savedObject as MaterializeObjectSave;

        save.LoadSave(this);
    }
    #endregion
}
