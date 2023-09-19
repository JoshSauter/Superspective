using System;
using Audio;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UniqueId))]
public class CubeReceptacle : SaveableObject<CubeReceptacle, CubeReceptacle.CubeReceptacleSave>, AudioJobOnGameObject {
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
    public bool lockCubeInPlace = false;
    public DissolveObject lockDissolve;
    public float receptacleSize = 1f;
    public float receptableDepth = 0.5f;
    public PickupObject cubeInReceptacle;
    public bool playSound = true;
    public bool playPuzzleCompleteSound = false;
    State _state = State.Empty;

    ColorCoded colorCoded;
    Vector3 endPos;
    Quaternion endRot;

    Vector3 startPos;

    Quaternion startRot;

    BoxCollider triggerZone;

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
                    onCubeHoldEnd?.Invoke();
                    if (playPuzzleCompleteSound) {
                        AudioManager.instance.Play(AudioName.CorrectAnswer, "CorrectAnswer", true);
                    }
                    break;
                case State.CubeExiting:
                    OnCubeReleaseStart?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseStartSimple?.Invoke();
                    break;
                case State.Empty:
                    OnCubeReleaseEnd?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseEndSimple?.Invoke();
                    if (lockCubeInPlace) {
                        lockDissolve.Dematerialize();
                    }
                    break;
            }

            _state = value;
        }
    }

    public bool isCubeInReceptacle => cubeInReceptacle != null;
    
    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    protected override void Start() {
        base.Start();
        AddTriggerZone();
        colorCoded = GetComponent<ColorCoded>();
    }

    void FixedUpdate() {
        UpdateCubeReceptacle();
    }

    void OnTriggerStay(Collider other) {
        if (gameObject.layer == LayerMask.NameToLayer("Invisible")) return;
        
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
    public UnityEvent onCubeHoldEnd;

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
                else {
                    endPos = transform.TransformPoint(0, 1 - receptableDepth, 0);
                    cubeInReceptacle.transform.position = endPos;
                    cubeInReceptacle.thisRigidbody.isKinematic = true;

                    if (lockCubeInPlace) {
                        lockDissolve.Materialize();
                        cubeInReceptacle.interactable = false; // TODO: Fix NPE here
                    }
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
                    cubeThatWasInReceptacle.thisRigidbody.isKinematic = false;
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

        if (playSound) {
            AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleEnter, ID, this);
        }

        if (cube.TryGetComponent(out DynamicObject dynamicObject)) {
            dynamicObject.ChangeScene(gameObject.scene);
        }

        state = State.CubeEnterRotate;
    }

    public void ReleaseCubeFromReceptacleInstantly() {
        if (cubeInReceptacle == null) return;
        cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
        state = State.CubeExiting;

        if (playSound) {
            AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleExit, ID, this);
        }

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

        if (playSound) {
            AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleExit, ID, this);
        }

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

    [Serializable]
    public class CubeReceptacleSave : SerializableSaveObject<CubeReceptacle> {
        SerializableReference<PickupObject, PickupObject.PickupObjectSave> cubeInReceptacle;
        SerializableVector3 endPos;
        SerializableQuaternion endRot;

        SerializableVector3 startPos;

        SerializableQuaternion startRot;
        State state;
        float timeSinceStateChange;

        public CubeReceptacleSave(CubeReceptacle receptacle) : base(receptacle) {
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

            receptacle.cubeInReceptacle = cubeInReceptacle?.GetOrNull();
            if (receptacle.cubeInReceptacle != null) {
                receptacle.cubeInReceptacle.OnPickupSimple += receptacle.ReleaseFromReceptacle;
            }
        }
    }
#endregion
}