using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EpitaphUtils;
using System.Linq;
using EpitaphUtils.ShaderUtils;

// TODO: Change the simple localScale modification to a dissolve shader
public class MaterializeObject : MonoBehaviour {
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

    IEnumerator materializeCoroutine;
    IEnumerator dematerializeCoroutine;

    public AnimationCurve animCurve;
    Vector3 startScale;

    void Start() {
        thisPickupObj = GetComponent<PickupObject>();

        startScale = transform.localScale;
        //allRenderers = Utils.GetComponentsInChildrenRecursively<Renderer>(transform);
        allColliders = Utils.GetComponentsInChildrenRecursively<Collider>(transform);

        materializeCoroutine = MaterializeCoroutine();
        dematerializeCoroutine = DematerializeCoroutine();
    }

    // DEBUG: Remove this
    bool test = false;
    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown("m")) {
            if (test) Materialize();
            else Dematerialize();

            test = !test;
        }
    }

    public void Materialize() {
        materializeCoroutine = MaterializeCoroutine();

        StopCoroutine(dematerializeCoroutine);
        StartCoroutine(materializeCoroutine);
    }

    public void Dematerialize() {
        dematerializeCoroutine = DematerializeCoroutine();
        StopCoroutine(materializeCoroutine);
        StartCoroutine(dematerializeCoroutine);
    }

    IEnumerator MaterializeCoroutine() {
        OnMaterializeStart?.Invoke();

        float t = Utils.Vector3InverseLerp(Vector3.zero, startScale, transform.localScale);
        float timeElapsed = materializeTime * t;
        while (true) {
            timeElapsed += Time.deltaTime;
            t = timeElapsed / materializeTime;

            transform.localScale = animCurve.Evaluate(t) * startScale;
            //foreach (var r in allRenderers) {
            //    foreach (var m in r.materials) {

            //    }
            //}

            yield return null;
            // Evaluate condition at end of loop to make sure we update dissolve value all the way to 1
            if (t > 1) break;
        }

        foreach (var c in allColliders) {
            c.enabled = true;
            Rigidbody rigidbody = c.GetComponent<Rigidbody>();
            if (rigidbody != null) {
                rigidbody.isKinematic = false;
            }
        }
        OnMaterializeEnd?.Invoke();
    }

    IEnumerator DematerializeCoroutine() {
        OnDematerializeStart?.Invoke();
        thisPickupObj.Drop();
        foreach (var c in allColliders) {
            c.enabled = false;
            Rigidbody rigidbody = c.GetComponent<Rigidbody>();
            if (rigidbody != null) {
                rigidbody.isKinematic = true;
            }
        }

        float t = Utils.Vector3InverseLerp(startScale, Vector3.zero, transform.localScale);
        float timeElapsed = dematerializeTime * t;
        while (true) {
            timeElapsed += Time.deltaTime;
            t = timeElapsed / dematerializeTime;

            transform.localScale = animCurve.Evaluate(1-t) * startScale;
            //foreach (var r in allRenderers) {
            //    foreach (var m in r.materials) {
            //        m.SetFloat(dissolveValuePropName, 1-Mathf.Clamp01(t));
            //    }
            //}

            yield return null;
            // Evaluate condition at end of loop to make sure we update dissolve value all the way to 1
            if (t > 1) break;
        }

        OnDematerializeEnd?.Invoke();
        if (destroyObjectOnDematerialize) {
            Destroy(gameObject);
        }
    }
}
