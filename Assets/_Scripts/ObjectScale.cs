using System;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectScale : SuperspectiveObject<ObjectScale, ObjectScale.ObjectScaleSave> {
    public float minSize = 1f;
    public float maxSize = 1f;
    public float period = 3f;

    Vector3 startScale;

    float timeElapsed;

    // Update is called once per frame
    void Update() {
        timeElapsed += Time.deltaTime;
        transform.localScale = startScale.normalized * (startScale.magnitude * Mathf.Lerp(
            minSize,
            maxSize,
            Mathf.Cos(timeElapsed * 2 * Mathf.PI / period) * 0.5f + 0.5f
        ));
    }

    protected override void OnEnable() {
        base.OnEnable();
        startScale = transform.localScale;
    }

    protected override void OnDisable() {
        base.OnDisable();
        transform.localScale = startScale;
        timeElapsed = 0;
    }

#region Saving

    public override void LoadSave(ObjectScaleSave save) {
        minSize = save.minSize;
        maxSize = save.maxSize;
        period = save.period;
        startScale = save.startScale;
        timeElapsed = save.timeElapsed;
        transform.localScale = save.scale;
    }

    [Serializable]
    public class ObjectScaleSave : SaveObject<ObjectScale> {
        public SerializableVector3 startScale;
        public SerializableVector3 scale;
        public float maxSize;
        public float minSize;
        public float period;
        public float timeElapsed;

        public ObjectScaleSave(ObjectScale script) : base(script) {
            minSize = script.minSize;
            maxSize = script.maxSize;
            period = script.period;
            startScale = script.startScale;
            timeElapsed = script.timeElapsed;
            scale = script.transform.localScale;
        }
    }
#endregion
}