using System;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using SuperspectiveUtils;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class ObjectScale : SuperspectiveObject<ObjectScale, ObjectScale.ObjectScaleSave> {
    public enum Mode {
        Simple,
        PerAxis
    }
    public Mode mode = Mode.Simple;
    public bool IsSimpleMode => mode == Mode.Simple;
    [ShowIf(nameof(IsSimpleMode))]
    public float minSize = 1f;
    [ShowIf(nameof(IsSimpleMode))]
    public float maxSize = 1f;
    [HideIf(nameof(IsSimpleMode))]
    public Vector3 minScale = new Vector3(1f, 1f, 1f);
    [HideIf(nameof(IsSimpleMode))]
    public Vector3 maxScale = new Vector3(1f, 1f, 1f);
    public float period = 3f;

    public enum AnimationMode {
        Cos,
        AnimationCurve_PingPong
    }
    public AnimationMode animationMode = AnimationMode.Cos;
    public bool IsUsingCurve => animationMode is AnimationMode.AnimationCurve_PingPong;
    [ShowIf(nameof(IsUsingCurve))]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Vector3 startScale;

    float timeElapsed;

    // Update is called once per frame
    void Update() {
        timeElapsed += Time.deltaTime;
        float t = 0.5f;
        switch (animationMode) {
            case AnimationMode.Cos:
                t = Mathf.Cos(timeElapsed * 2 * Mathf.PI / period) * 0.5f + 0.5f;
                break;
            case AnimationMode.AnimationCurve_PingPong:
                t = animationCurve.Evaluate(Mathf.PingPong(timeElapsed / period, 1));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (mode) {
            case Mode.Simple:
                transform.localScale = startScale.normalized * (startScale.magnitude * Mathf.Lerp(
                    minSize,
                    maxSize,
                    t
                ));
                break;
            case Mode.PerAxis:
                transform.localScale = startScale.ScaledWith(Vector3.LerpUnclamped(minScale, maxScale, t));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
        mode = save.mode;
        minScale = save.minScale;
        maxScale = save.maxScale;
        animationMode = save.animationMode;
    }

    [Serializable]
    public class ObjectScaleSave : SaveObject<ObjectScale> {
        public SerializableVector3 startScale;
        public SerializableVector3 scale;
        public float maxSize;
        public float minSize;
        public float period;
        public float timeElapsed;
        public Mode mode;
        public SerializableVector3 minScale;
        public SerializableVector3 maxScale;
        public AnimationMode animationMode;

        public ObjectScaleSave(ObjectScale script) : base(script) {
            minSize = script.minSize;
            maxSize = script.maxSize;
            period = script.period;
            startScale = script.startScale;
            timeElapsed = script.timeElapsed;
            scale = script.transform.localScale;
            mode = script.mode;
            minScale = script.minScale;
            maxScale = script.maxScale;
            animationMode = script.animationMode;
        }
    }
#endregion
}