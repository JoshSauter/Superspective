using System;
using Audio;
using EpitaphUtils;
using Saving;
using SerializableClasses;
using UnityEngine;

[RequireComponent(typeof(UniqueId))]
public class CubeReceptacle : SaveableObject<CubeReceptacle, CubeReceptacle.CubeReceptacleSave> {
    public delegate void CubeReceptacleAction(CubeReceptacle receptacle, PickupObject cube);

    public delegate void CubeReceptacleActionSimple();

    public enum State {
        Empty,
        CubeEnterRotate,
        CubeEnterTranslate,
        CubeInReceptacle,
        CubeExiting
    }

    const float rotateTime = 0.25f;
    const float translateTime = 0.5f;
    const float afterReleaseCooldown = 1f;
    const float timeToRelease = .25f;
    public float timeSinceStateChange;

    public bool makesCubeIrreplaceable = true;
    public float receptacleSize = 1f;
    public float receptableDepth = 0.5f;
    public PickupObject cubeInReceptacle;
    UniqueId _id;
    State _state = State.Empty;

    ColorCoded colorCoded;
    Vector3 endPos;
    Quaternion endRot;

    Vector3 startPos;

    Quaternion startRot;

    BoxCollider triggerZone;

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
                case State.CubeEnterRotate:
                    OnCubeHoldStart?.Invoke(this, cubeInReceptacle);
                    OnCubeHoldStartSimple?.Invoke();
                    break;
                case State.CubeInReceptacle:
                    OnCubeHoldEnd?.Invoke(this, cubeInReceptacle);
                    OnCubeHoldEndSimple?.Invoke();
                    break;
                case State.CubeExiting:
                    OnCubeReleaseStart?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseStartSimple?.Invoke();
                    break;
                case State.Empty:
                    OnCubeReleaseEnd?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseEndSimple?.Invoke();
                    break;
            }

            _state = value;
        }
    }

    public bool isCubeInReceptacle => cubeInReceptacle != null;

    protected override void Start() {
        base.Start();
        AddTriggerZone();
        colorCoded = GetComponent<ColorCoded>();
    }

    void FixedUpdate() {
        UpdateCubeReceptacle();
    }

    void OnTriggerStay(Collider other) {
        PickupObject cube = other.gameObject.GetComponent<PickupObject>();
        if (colorCoded != null && !colorCoded.AcceptedColor(other.gameObject.GetComponent<ColorCoded>())) return;
        if (cube != null && cubeInReceptacle == null) StartCubeEnter(cube);
    }

    public event CubeReceptacleAction OnCubeHoldStart;
    public event CubeReceptacleAction OnCubeHoldEnd;
    public event CubeReceptacleAction OnCubeReleaseStart;
    public event CubeReceptacleAction OnCubeReleaseEnd;

    public event CubeReceptacleActionSimple OnCubeHoldStartSimple;
    public event CubeReceptacleActionSimple OnCubeHoldEndSimple;
    public event CubeReceptacleActionSimple OnCubeReleaseStartSimple;
    public event CubeReceptacleActionSimple OnCubeReleaseEndSimple;

    void AddTriggerZone() {
        //GameObject triggerZoneGO = new GameObject("TriggerZone");
        //triggerZoneGO.transform.SetParent(transform, false);
        //triggerZoneGO.layer = LayerMask.NameToLayer("Ignore Raycast");
        //triggerZone = triggerZoneGO.AddComponent<BoxCollider>();
        triggerZone = gameObject.AddComponent<BoxCollider>();
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        triggerZone.size = new Vector3(receptacleSize * 0.25f, receptacleSize * 1.5f, receptacleSize * 0.25f);
        triggerZone.isTrigger = true;
    }

    void UpdateCubeReceptacle() {
        timeSinceStateChange += Time.fixedDeltaTime;
        switch (state) {
            case State.Empty:
                if (timeSinceStateChange > afterReleaseCooldown) RestoreTriggerZoneAfterCooldown();
                break;
            case State.CubeInReceptacle:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    state = State.CubeExiting;
                    state = State.Empty;
                }

                break;
            case State.CubeEnterRotate:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    state = State.CubeExiting;
                    state = State.Empty;
                    break;
                }

                if (timeSinceStateChange < rotateTime) {
                    float t = timeSinceStateChange / rotateTime;

                    cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                    cubeInReceptacle.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
                }
                else {
                    cubeInReceptacle.transform.position = endPos;
                    cubeInReceptacle.transform.rotation = endRot;

                    startPos = cubeInReceptacle.transform.position;
                    endPos = transform.TransformPoint(0, 1 - receptableDepth, 0);

                    state = State.CubeEnterTranslate;
                }

                break;
            case State.CubeEnterTranslate:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    state = State.CubeExiting;
                    state = State.Empty;
                    break;
                }

                if (timeSinceStateChange < translateTime) {
                    float t = timeSinceStateChange / translateTime;

                    cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                }
                else {
                    cubeInReceptacle.transform.position = endPos;

                    cubeInReceptacle.interactable = true;
                    state = State.CubeInReceptacle;
                }

                break;
            case State.CubeExiting:
                if (cubeInReceptacle == null) {
                    state = State.Empty;
                    break;
                }

                if (timeSinceStateChange < timeToRelease) {
                    float t = timeSinceStateChange / timeToRelease;

                    cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                    cubeInReceptacle.transform.rotation = startRot;
                }
                else {
                    PickupObject cubeThatWasInReceptacle = cubeInReceptacle;
                    cubeThatWasInReceptacle.shouldFollow = true;
                    cubeThatWasInReceptacle.interactable = true;
                    cubeThatWasInReceptacle.isReplaceable = true;
                    triggerZone.enabled = false;

                    state = State.Empty;
                    cubeInReceptacle = null;
                }

                break;
        }
    }

    void StartCubeEnter(PickupObject cube) {
        Rigidbody cubeRigidbody = cube.GetComponent<Rigidbody>();
        cube.Drop();
        cube.interactable = false;
        cubeRigidbody.isKinematic = true;
        cubeInReceptacle = cube;
        if (makesCubeIrreplaceable) cubeInReceptacle.isReplaceable = false;
        cubeInReceptacle.OnPickupSimple += ReleaseFromReceptacle;

        startRot = cubeInReceptacle.transform.rotation;
        endRot = RightAngleRotations.GetNearestRelativeToTransform(startRot, transform);

        startPos = cubeInReceptacle.transform.position;
        endPos = transform.TransformPoint(0, transform.InverseTransformPoint(startPos).y, 0);

        AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleEnter, ID, gameObject);

        state = State.CubeEnterRotate;
    }

    public void ReleaseCubeFromReceptacleInstantly() {
        if (cubeInReceptacle == null) return;
        cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
        state = State.CubeExiting;

        AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleExit, ID, gameObject);

        startPos = cubeInReceptacle.transform.position;
        endPos = transform.TransformPoint(0, 1.5f, 0);
        startRot = cubeInReceptacle.transform.rotation;

        PickupObject cubeThatWasInReceptacle = cubeInReceptacle;
        cubeThatWasInReceptacle.shouldFollow = true;
        cubeThatWasInReceptacle.interactable = true;
        cubeThatWasInReceptacle.isReplaceable = true;
        triggerZone.enabled = false;

        state = State.Empty;
        cubeThatWasInReceptacle.thisRigidbody.isKinematic = false;
        cubeInReceptacle = null;
    }

    void ReleaseFromReceptacle() {
        cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
        ReleaseCubeFromReceptacle();
    }

    void ReleaseCubeFromReceptacle() {
        state = State.CubeExiting;

        AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleExit, ID, gameObject);

        cubeInReceptacle.shouldFollow = false;
        cubeInReceptacle.interactable = false;

        startPos = cubeInReceptacle.transform.position;
        endPos = transform.TransformPoint(0, 1.5f, 0);
        startRot = cubeInReceptacle.transform.rotation;
    }

    void RestoreTriggerZoneAfterCooldown() {
        triggerZone.enabled = true;
    }

#region Saving
    // All components on PickupCubes share the same uniqueId so we need to qualify with component name
    public override string ID => $"CubeReceptacle_{id.uniqueId}";

    [Serializable]
    public class CubeReceptacleSave : SerializableSaveObject<CubeReceptacle> {
        SerializableReference<PickupObject> cubeInReceptacle;
        SerializableVector3 endPos;
        SerializableQuaternion endRot;

        SerializableVector3 startPos;

        SerializableQuaternion startRot;
        State state;
        float timeSinceStateChange;

        public CubeReceptacleSave(CubeReceptacle receptacle) {
            state = receptacle.state;
            timeSinceStateChange = receptacle.timeSinceStateChange;

            startRot = receptacle.startRot;
            endRot = receptacle.endRot;

            startPos = receptacle.startPos;
            endPos = receptacle.endPos;

            cubeInReceptacle = receptacle.cubeInReceptacle;
        }

        public override void LoadSave(CubeReceptacle receptacle) {
            receptacle.state = state;
            receptacle.timeSinceStateChange = timeSinceStateChange;

            receptacle.startRot = startRot;
            receptacle.endRot = endRot;

            receptacle.startPos = startPos;
            receptacle.endPos = endPos;

            receptacle.cubeInReceptacle = cubeInReceptacle;
            if (receptacle.cubeInReceptacle != null) {
                receptacle.cubeInReceptacle.OnPickupSimple += receptacle.ReleaseFromReceptacle;
            }
        }
    }
#endregion
}