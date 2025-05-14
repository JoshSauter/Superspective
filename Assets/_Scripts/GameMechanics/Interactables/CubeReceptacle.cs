using System;
using System.Collections;
using Audio;
using DissolveObjects;
using PoweredObjects;
using PowerTrailMechanics;
using SuperspectiveUtils;
using Saving;
using SerializableClasses;
using Sirenix.OdinInspector;
using StateUtils;
using SuperspectiveAttributes;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UniqueId), typeof(PoweredObject))]
public class CubeReceptacle : SuperspectiveObject<CubeReceptacle, CubeReceptacle.CubeReceptacleSave>, AudioJobOnGameObject {
    public delegate void CubeReceptacleAction(CubeReceptacle receptacle, PickupObject cube);

    public delegate void CubeReceptacleActionSimple();

    public enum State : byte {
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

    public bool makesCubeIrreplaceable = true;
    public bool lockCubeInPlace = false;
    public DissolveObject lockDissolve;
    public float receptacleSize = 1f;
    public float receptableDepth = 0.5f;
    [SaveUnityObject]
    public PickupObject cubeInReceptacle;
    public bool playSound = true;
    public bool playPuzzleCompleteSound = false;

    private PoweredObject _pwr;
    public PoweredObject pwr {
        get {
            if (_pwr == null) {
                _pwr = this.GetOrAddComponent<PoweredObject>();
            }

            return _pwr;
        }
        set => _pwr = value;
    }

    ColorCoded colorCoded;
    Vector3 endPos;
    Quaternion endRot;

    Vector3 startPos;

    Quaternion startRot;

    BoxCollider triggerZone;

    public StateMachine<State> stateMachine;

    public bool isCubeInReceptacle => cubeInReceptacle != null;
    
    public Transform GetObjectToPlayAudioOn(AudioManager.AudioJob _) => transform;

    protected override void Start() {
        base.Start();
        
        AddTriggerZone();

        stateMachine = this.StateMachine(State.Empty);
        
        colorCoded = GetComponent<ColorCoded>();
        InitializeStateMachine();
    }

    private bool AcceptedColor(GameObject other) {
        return colorCoded == null || colorCoded.AcceptedColor(other.GetComponent<ColorCoded>());
    }

    private void InitializeStateMachine() {
        stateMachine.AddTrigger(State.Empty, afterReleaseCooldown, RestoreTriggerZoneAfterCooldown);
        stateMachine.AddTrigger(State.CubeInReceptacle, () => {
            if (cubeInReceptacle == null) return;
            
            endPos = transform.TransformPoint(0, 1 - receptableDepth, 0);
            cubeInReceptacle.transform.position = endPos;
            
            if (!AcceptedColor(cubeInReceptacle.gameObject)) {
                AudioManager.instance.PlayAtLocation(AudioName.DisabledSound, ID, transform.position);
                ExpelCube();
                return;
            };

            if (lockCubeInPlace) {
                LockCubeInPlace();
            }
            else {
                cubeInReceptacle.transform.position = endPos;
                cubeInReceptacle.interactable = true;
            }
        });
        
        stateMachine.AddStateTransition(State.CubeEnterRotate, State.CubeEnterTranslate, rotateTime);
        stateMachine.AddTrigger(State.CubeEnterTranslate, () => {
            if (cubeInReceptacle == null) return;
            
            cubeInReceptacle.transform.position = endPos;
            cubeInReceptacle.transform.rotation = endRot;

            startPos = cubeInReceptacle.transform.position;
            endPos = transform.TransformPoint(0, 1 - receptableDepth, 0);
        });
        
        stateMachine.AddStateTransition(State.CubeEnterTranslate, State.CubeInReceptacle, translateTime);

        stateMachine.AddStateTransition(State.CubeExiting, State.Empty, timeToRelease);
        stateMachine.AddTrigger(State.Empty, () => {
            if (cubeInReceptacle == null) return;
            
            PickupObject cubeThatWasInReceptacle = cubeInReceptacle;
            cubeThatWasInReceptacle.shouldFollow = true;
            cubeThatWasInReceptacle.interactable = true;
            cubeThatWasInReceptacle.isReplaceable = true;
            triggerZone.enabled = false;
            debug.Log($"Trigger zone enabled: {triggerZone.enabled}", true);

            cubeThatWasInReceptacle.receptacleHeldIn = null;
            cubeThatWasInReceptacle.RecalculateRigidbodyKinematics();
            cubeInReceptacle = null;
        });

        stateMachine.OnStateChangeSimple += () => {
            debug.Log($"Switching to {stateMachine.State}", true);
            switch (stateMachine.State) {
                case State.Empty:
                    OnCubeReleaseEnd?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseEndSimple?.Invoke();
                    if (lockCubeInPlace) {
                        lockDissolve.Dematerialize();
                    }

                    pwr.state.Set(PowerState.Depowered);
                    break;
                case State.CubeEnterRotate:
                    OnCubeHoldStart?.Invoke(this, cubeInReceptacle);
                    OnCubeHoldStartSimple?.Invoke();

                    if (AcceptedColor(cubeInReceptacle.gameObject)) {
                        pwr.state.Set(PowerState.PartiallyPowered);
                    }
                    break;
                case State.CubeEnterTranslate:
                    break;
                case State.CubeInReceptacle:
                    OnCubeHoldEnd?.Invoke(this, cubeInReceptacle);
                    OnCubeHoldEndSimple?.Invoke();
                    onCubeHoldEnd?.Invoke();
                    if (AcceptedColor(cubeInReceptacle.gameObject)) {
                        if (playPuzzleCompleteSound) {
                            AudioManager.instance.Play(AudioName.CorrectAnswer);
                        }

                        pwr.state.Set(PowerState.Powered);
                    }
                    break;
                case State.CubeExiting:
                    OnCubeReleaseStart?.Invoke(this, cubeInReceptacle);
                    OnCubeReleaseStartSimple?.Invoke();
                    
                    pwr.state.Set(PowerState.PartiallyDepowered);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    void Update() {
        UpdateCubeReceptacle();
    }

    IEnumerator ResetPlayerInTriggerZoneState() {
        yield return new WaitForFixedUpdate();
        playerStillInTriggerZone = false;
    }

    private bool playerStillInTriggerZone = false;
    void OnTriggerStay(Collider other) {
        if (GameManager.instance.IsCurrentlyLoading) return;
        if (!enabled) return;
        if (gameObject.layer == SuperspectivePhysics.InvisibleLayer) return;
        if (other.TaggedAsPlayer()) {
            playerStillInTriggerZone = true;
            StartCoroutine(ResetPlayerInTriggerZoneState());
        }
        if (playerStillInTriggerZone) return;
        if (other.isTrigger) return;
        if (!triggerZone.enabled) return;
        
        PickupObject cube = other.gameObject.FindInParentsRecursively<PickupObject>();
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

    [ContextMenu("Add Trigger Zone"), Button("Initialize Trigger Zone")]
    void AddTriggerZone() {
        //GameObject triggerZoneGO = new GameObject("TriggerZone");
        //triggerZoneGO.transform.SetParent(transform, false);
        //triggerZoneGO.layer = LayerMask.NameToLayer("Ignore Raycast");
        //triggerZone = triggerZoneGO.AddComponent<BoxCollider>();
        triggerZone = gameObject.GetOrAddComponent<BoxCollider>();
        gameObject.layer = SuperspectivePhysics.TriggerZoneLayer;

        triggerZone.size = new Vector3(receptacleSize * 0.25f, receptacleSize * 1.5f, receptacleSize * 0.25f);
        triggerZone.isTrigger = true;
    }

    void LockCubeInPlace() {
        lockDissolve.Materialize();
        cubeInReceptacle.interactable = false; // TODO: Fix NPE here
    }

    void UnlockCubeInPlace() {
        lockDissolve.Dematerialize();
        if (cubeInReceptacle != null) {
            cubeInReceptacle.interactable = true;
        }
    }

    void UpdateCubeReceptacle() {
        if (isCubeInReceptacle) {
            cubeInReceptacle.isReplaceable = !makesCubeIrreplaceable;
        }

        // Skip first frame to avoid weird bugs where the cube snaps into a weird position for a frame
        if (stateMachine.Time == 0) return;
        float t;
        switch (stateMachine.State) {
            case State.Empty:
                break;
            case State.CubeInReceptacle:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    stateMachine.Set(State.CubeExiting);
                    stateMachine.Set(State.Empty);
                }
                
                
                if (lockCubeInPlace && lockDissolve.stateMachine == DissolveObject.State.Dematerialized) {
                    LockCubeInPlace();
                }
                break;
            case State.CubeEnterRotate:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    stateMachine.Set(State.CubeExiting);
                    stateMachine.Set(State.Empty);
                    break;
                }

                t = stateMachine.Time / rotateTime;
                cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                cubeInReceptacle.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
                break;
            case State.CubeEnterTranslate:
                if (cubeInReceptacle == null) {
                    // Trigger both ReleaseStart and ReleaseEnd events if the cube disappears while in the receptacle (i.e. replaced by a spawner)
                    stateMachine.Set(State.CubeExiting);
                    stateMachine.Set(State.Empty);
                    break;
                }

                t = stateMachine.Time / translateTime;

                cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                break;
            case State.CubeExiting:
                if (cubeInReceptacle == null) {
                    stateMachine.Set(State.Empty);
                    break;
                }

                t = stateMachine.Time / timeToRelease;

                cubeInReceptacle.transform.position = Vector3.Lerp(startPos, endPos, t);
                cubeInReceptacle.transform.rotation = startRot;
                break;
        }
    }

    void StartCubeEnter(PickupObject cube) {
        cube.receptacleHeldIn = this;
        
        Rigidbody cubeRigidbody = cube.GetComponent<Rigidbody>();
        cube.Drop();
        cube.interactable = false;
        cubeInReceptacle = cube;
        cube.thisGravity.useGravity = false;
        cubeInReceptacle.OnPickupSimple += ReleaseFromReceptacle;

        startRot = cubeInReceptacle.transform.rotation;
        endRot = RightAngleRotations.GetNearestRelativeToTransform(startRot, transform);

        startPos = cubeInReceptacle.transform.position;
        endPos = transform.TransformPoint(0, transform.InverseTransformPoint(startPos).y, 0);

        if (playSound) {
            AudioManager.instance.PlayOnGameObject(AudioName.ReceptacleEnter, ID, this);
        }

        if (cube.TryGetComponent(out DynamicObject dynamicObject)) {
            if (!dynamicObject.ChangeScene(gameObject.scene)) {
                debug.LogWarning($"Didn't move {cube.ID} to {gameObject.scene.name} (Might have already been in it).");
            }
        }

        stateMachine.Set(State.CubeEnterRotate);
    }

    public void ReleaseCubeFromReceptacleInstantly() {
        if (cubeInReceptacle == null) return;
        cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
        stateMachine.Set(State.CubeExiting);

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
        cubeThatWasInReceptacle.thisGravity.useGravity = !cubeThatWasInReceptacle.isHeld;
        triggerZone.enabled = false;

        stateMachine.Set(State.Empty);
        cubeInReceptacle = null;
    }

    private const float EXPEL_CUBE_FORCE = 1200;
    private const float EXPEL_CUBE_FORCE_VARIANCE = 240;
    public void ExpelCube() => ExpelCubeWithForce(1);

    public void ExpelCubeWithForce(float forceMultiplier) {
        PickupObject cubeToEject = cubeInReceptacle;
        if (cubeToEject == null) {
            return;
        }
        ReleaseCubeFromReceptacleInstantly();
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
        float forceMagnitude = forceMultiplier * UnityEngine.Random.Range(EXPEL_CUBE_FORCE - EXPEL_CUBE_FORCE_VARIANCE, EXPEL_CUBE_FORCE + EXPEL_CUBE_FORCE_VARIANCE);
        Vector3 ejectionDirection = transform.TransformDirection(new Vector3(-Mathf.Abs(randomDirection.x), 4, randomDirection.y)).normalized;
        debug.Log($"Adding {ejectionDirection * forceMagnitude} force to cube {cubeToEject.ID}'s {(cubeToEject.thisRigidbody.isKinematic ? "Kinematic" : "Non-Kinematic")} Rigidbody to eject it from receptacle");
        cubeToEject.thisRigidbody.AddForce(ejectionDirection * forceMagnitude, ForceMode.Impulse);
    }

    public void ExpelCubeStraightOut(float forceMultiplier) {
        PickupObject cubeToEject = cubeInReceptacle;
        if (cubeToEject == null) {
            return;
        }
        ReleaseCubeFromReceptacleInstantly();
        Vector3 ejectionDirection = transform.up;
        float forceMagnitude = forceMultiplier * UnityEngine.Random.Range(EXPEL_CUBE_FORCE - EXPEL_CUBE_FORCE_VARIANCE, EXPEL_CUBE_FORCE + EXPEL_CUBE_FORCE_VARIANCE);
        debug.Log($"Adding {ejectionDirection * forceMagnitude} force to cube {cubeToEject.ID}'s {(cubeToEject.thisRigidbody.isKinematic ? "Kinematic" : "Non-Kinematic")} Rigidbody to eject it from receptacle");
        cubeToEject.thisRigidbody.AddForce(ejectionDirection * forceMagnitude, ForceMode.Impulse);
    }

    void ReleaseFromReceptacle() {
        cubeInReceptacle.OnPickupSimple -= ReleaseFromReceptacle;
        ReleaseCubeFromReceptacle();
    }

    protected override void OnEnable() {
        base.OnEnable();
        
        if (triggerZone != null) {
            triggerZone.enabled = true;
        }
    }

    protected override void OnDisable() {
        base.OnDisable();
        if (triggerZone) {
            triggerZone.enabled = false;
        }
    }

    void ReleaseCubeFromReceptacle() {
        stateMachine.Set(State.CubeExiting);

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

    public override void LoadSave(CubeReceptacleSave save) {
        if (cubeInReceptacle != null) {
            cubeInReceptacle.OnPickupSimple += ReleaseFromReceptacle;
        }
    }

    [Serializable]
    public class CubeReceptacleSave : SaveObject<CubeReceptacle> {
        public CubeReceptacleSave(CubeReceptacle receptacle) : base(receptacle) { }
    }
#endregion
}