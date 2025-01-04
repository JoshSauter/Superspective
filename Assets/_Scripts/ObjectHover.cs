using System;
using GrowShrink;
using Saving;
using SerializableClasses;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(UniqueId))]
public class ObjectHover : SuperspectiveObject<ObjectHover, ObjectHover.ObjectHoverSave> {
    public bool importantToSave = false;
    
    const float HOVERING_PAUSE_LERP = 0.1f;
    public bool useLocalCoordinates = true;
    [FormerlySerializedAs("maxDisplacementUp")]
    public float yAmplitude = 0.125f;
    [FormerlySerializedAs("maxDisplacementForward")]
    public float zAmplitude;
    [FormerlySerializedAs("maxDisplacementRight")]
    public float xAmplitude;

    private GrowShrinkObject _growShrink;
    private GrowShrinkObject GrowShrink => _growShrink ??= this.GetComponent<GrowShrinkObject>();
    private float Scale => hasGrowShrink ? GrowShrink.CurrentScale : 1;
    private bool hasGrowShrink;

    public enum LoopMode : byte {
        SinWave,    // Smoothly goes up and down on a period
        RampAndCut  // Only goes in the amplitude direction, then snaps back to initial position at period restart
    }

    public LoopMode loopMode;
    public float periodOffset;
    public float period = 1f;
    public bool hoveringPaused;

    Vector3 currentOffset = Vector3.zero;
    Vector3 forward;
    Vector3 right;

    float timeElapsed;
    Vector3 up;

    // Use this for initialization
    protected override void Awake() {
        base.Awake();
        up = useLocalCoordinates ? transform.up : Vector3.up;
        forward = useLocalCoordinates ? transform.forward : Vector3.forward;
        right = useLocalCoordinates ? transform.right : Vector3.right;

        timeElapsed = periodOffset;
        
        if (GrowShrink != null) {
            hasGrowShrink = true;
        }
    }

    // Update is called once per frame
    void Update() {
        if (hoveringPaused) return;
        
        timeElapsed += Time.fixedDeltaTime;
        timeElapsed %= period;
        float t = timeElapsed / period;

        switch (loopMode) {
            case LoopMode.SinWave:
                t = Mathf.Sin(Mathf.PI * 2 * t);
                break;
            case LoopMode.RampAndCut:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        Vector3 nextOffsetUp = yAmplitude * t * Scale * up;
        Vector3 nextOffsetForward = zAmplitude * t * Scale * forward;
        Vector3 nextOffsetRight = xAmplitude * t * Scale * right;
        Vector3 nextOffset = nextOffsetUp + nextOffsetForward + nextOffsetRight;

        Vector3 thisDisplacement = (nextOffset - currentOffset);
        
        Vector3 prevPos = transform.position;
        // Apparently setting position always seems to end up with floating point errors even for
        // axes that have 0 offset. BS that can lead to incorrect displacements over time
        transform.position += thisDisplacement;
        Vector3 actualDisplacement = transform.position - prevPos;
        // Account for above issue when setting currentOffset
        currentOffset = nextOffset + (actualDisplacement - thisDisplacement);
    }

#region Saving

    public override void LoadSave(ObjectHoverSave save) {
        useLocalCoordinates = save.useLocalCoordinates;
        yAmplitude = save.yAmplitude;
        zAmplitude = save.zAmplitude;
        xAmplitude = save.xAmplitude;
        loopMode = save.loopMode;
        periodOffset = save.periodOffset;
        period = save.period;
        up = save.up;
        forward = save.forward;
        right = save.right;
        transform.position = save.position;
        currentOffset = save.currentOffset;
        hoveringPaused = save.hoveringPaused;
        timeElapsed = save.timeElapsed;
    }

    // Many usages of ObjectHover have no gameplay impact and do not need to be saved. Mark importantToSave as true if you want to save it
    public override bool SkipSave => !importantToSave;

    [Serializable]
    public class ObjectHoverSave : SaveObject<ObjectHover> {
        public SerializableVector3 currentOffset;
        public SerializableVector3 up;
        public SerializableVector3 forward;
        public SerializableVector3 right;
        public SerializableVector3 position;
        public float zAmplitude;
        public float xAmplitude;
        public float yAmplitude;
        public float periodOffset;
        public float period;
        public float timeElapsed;
        public LoopMode loopMode;
        public bool hoveringPaused;
        public bool useLocalCoordinates;

        public ObjectHoverSave(ObjectHover script) : base(script) {
            useLocalCoordinates = script.useLocalCoordinates;
            yAmplitude = script.yAmplitude;
            zAmplitude = script.zAmplitude;
            xAmplitude = script.xAmplitude;
            loopMode = script.loopMode;
            periodOffset = script.periodOffset;
            period = script.period;
            up = script.up;
            forward = script.forward;
            right = script.right;
            position = script.transform.position;
            currentOffset = script.currentOffset;
            hoveringPaused = script.hoveringPaused;
            timeElapsed = script.timeElapsed;
        }
    }
#endregion
}