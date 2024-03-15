using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Interactables;
using LevelSpecific.Fork;
using NaughtyAttributes;
using Saving;
using SerializableClasses;
using StateUtils;
using SuperspectiveUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using PickupObjectReference = SerializableClasses.SerializableReference<PickupObject, PickupObject.PickupObjectSave>;

[RequireComponent(typeof(UniqueId))]
public class CubeSpawner : SaveableObject<CubeSpawner, CubeSpawner.CubeSpawnerSave> {
    public DimensionObject backWall;
    public Button button;
    public float tDistanceBeforeEnablingNewCubeColliders = 0.5f;
    const float spawnOffset = 12;

    private Vector3 lastCubeSpawnedPosition;

    public DynamicObject cubePrefab;
    public PickupObject cubeSpawned;
    [Header("Cube grabbed from spawner:")]
    public PickupObjectReference cubeGrabbedFromSpawner;
    public Collider roofCollider;
    public Transform glass;
    const float glassLowerDelay = 1.75f;
    const float glassRaiseDelay = 0.25f;
    const float glassOffset = 1.5f;
    const float glassMoveTime = 1.2f;
    float startHeight;

    public enum SpawnState {
        // Initial state
        NoCubeSpawned,
        // After button is pressed but before cube is removed from spawner
        CubeSpawnedButNotTaken,
        // After cube is removed from spawner area
        CubeTaken,
        // Doesn't last as long as DespawnState.CubeBeingDestroyed so respawn is quicker than despawn time
        InRespawnDelay
    }

    public enum DespawnState {
        Idle,
        // When Respawn Cube button is hit after cube is spawned
        CubeBeingDestroyed
    }

    public StateMachine<SpawnState> spawnState;
    public StateMachine<DespawnState> despawnState;

    PickupObject cubeDespawning;
    private const float respawnDelay = glassRaiseDelay + glassMoveTime;
    const float despawnTime = 4f;
    Vector3 despawnStartSize = Vector3.one;
    private const float despawnEndSizeMultiplier = 4;
    AnimationCurve cubeDespawnSizeCurve = AnimationCurve.EaseInOut(0,0,1,1);

    protected override void Awake() {
        base.Awake();
        spawnState = this.StateMachine(SpawnState.NoCubeSpawned);
        despawnState = this.StateMachine(DespawnState.Idle);
        
        button.OnButtonPressBegin += (_) => SpawnNewCube();
        button.OnButtonUnpressBegin += (_) => DestroyCubeAlreadyGrabbedFromSpawner();

        startHeight = glass.transform.localPosition.y;
    }

    protected override void Init() {
        base.Init();
        InitializeSpawnStateMachine();
        InitializeDespawnStateMachine();
    }

    void InitializeSpawnStateMachine() {
        spawnState.AddStateTransition(SpawnState.InRespawnDelay, SpawnState.NoCubeSpawned, respawnDelay);

        spawnState.OnStateChange += (prevState, _) => {
            if (prevState == SpawnState.InRespawnDelay && spawnState == SpawnState.NoCubeSpawned) {
                button.PressButton();
            }
        };

        spawnState.WithUpdate(SpawnState.NoCubeSpawned, _ => {
            glass.localPosition = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
        });
        
        spawnState.WithUpdate(SpawnState.CubeSpawnedButNotTaken, timeSinceStateChanged => {
            if (cubeSpawned == null) {
                spawnState.Set(SpawnState.NoCubeSpawned);
                return;
            }
                
            DimensionObject parentDimensionObj = cubeSpawned.GetComponentInParent<DimensionObject>();
            // Make sure the collision logic trigger zone follows the actual cube that's falling
            var ignoreCollisionsTriggerZone = parentDimensionObj.ignoreCollisionsTriggerZone;
            if (ignoreCollisionsTriggerZone != null && ignoreCollisionsTriggerZone.transform.parent != cubeSpawned.transform) {
                ignoreCollisionsTriggerZone.transform.SetParent(cubeSpawned.transform);
            }

            float distanceFallen = Vector3.Distance(lastCubeSpawnedPosition, cubeSpawned.transform.position);
            float distanceThreshold = spawnOffset * tDistanceBeforeEnablingNewCubeColliders;
            
            // Disable new cube's colliders for the first part of the fall
            if (distanceFallen <= distanceThreshold) {
                foreach (var newCubeColliders in parentDimensionObj.colliders) {
                    newCubeColliders.enabled = false;
                }
            }
            else {
                // Restore collision for the cube for the second part of its fall
                foreach (var newCubeColliders in parentDimensionObj.colliders) {
                    newCubeColliders.enabled = true;
                }
            }

            if (timeSinceStateChanged > glassLowerDelay && timeSinceStateChanged < glassMoveTime + glassLowerDelay) {
                float time = timeSinceStateChanged - glassLowerDelay;
                float t = time / glassMoveTime;
                    
                Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                Vector3 endPos = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
                glass.localPosition = Vector3.Lerp(startPos, endPos, t*t);
            }
            else if (timeSinceStateChanged >= glassMoveTime + glassLowerDelay) {
                glass.localPosition = new Vector3(glass.localPosition.x, startHeight-glassOffset, glass.localPosition.z);
            }
        });

        Action<float> respawnDelayAndTakenUpdate = (timeSinceStateChanged) => {
            if (timeSinceStateChanged > glassRaiseDelay && timeSinceStateChanged < glassMoveTime + glassRaiseDelay) {
                float time = timeSinceStateChanged - glassRaiseDelay;
                float t = time / glassMoveTime;

                Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                Vector3 endPos = new Vector3(glass.localPosition.x, startHeight - glassOffset, glass.localPosition.z);
                glass.localPosition = Vector3.Lerp(endPos, startPos, t * t);
            }
        };
        spawnState.WithUpdate(SpawnState.InRespawnDelay, respawnDelayAndTakenUpdate);
        spawnState.WithUpdate(SpawnState.CubeTaken, respawnDelayAndTakenUpdate);
    }

    void InitializeDespawnStateMachine() {
        despawnState.AddStateTransition(DespawnState.CubeBeingDestroyed, DespawnState.Idle, despawnTime);

        despawnState.OnStateChangeSimple += () => {
            switch (despawnState.state) {
                case DespawnState.Idle:
                    if (cubeDespawning != null) {
                        cubeDespawning.transform.FindInParentsRecursively<DynamicObject>().Destroy();
                    }
                    cubeDespawning = null;
                    break;
                case DespawnState.CubeBeingDestroyed:
                    if (spawnState.state is SpawnState.CubeSpawnedButNotTaken or SpawnState.CubeTaken) {
                        spawnState.Set(SpawnState.InRespawnDelay);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
        
        despawnState.WithUpdate(DespawnState.CubeBeingDestroyed, UpdateDematerializeCube);
    }

    static bool ReferenceIsReplaceable(PickupObjectReference obj) {
        return obj?.Reference?.Match(
            pickupObject => pickupObject.isReplaceable,
            saveObject => saveObject.isReplaceable
        ) ?? true;
    }

    void UpdateButtonInteractability() {
        if (spawnState == SpawnState.CubeTaken && !ReferenceIsReplaceable(cubeGrabbedFromSpawner)) {
            button.interactableObject.SetAsDisabled("(Spawned cube locked in receptacle)");
        }
        else if ((spawnState == SpawnState.CubeSpawnedButNotTaken && spawnState.timeSinceStateChanged < glassMoveTime + glassLowerDelay) || spawnState == SpawnState.InRespawnDelay) {
            button.interactableObject.SetAsHidden();
        }
        else {
            string msg = spawnState == SpawnState.NoCubeSpawned ? "Spawn cube" : "Respawn cube";
            button.interactableObject.SetAsInteractable(msg);
        }
    }

    void Update() {
        if (GameManager.instance.IsCurrentlyLoading) return;

        // Make it so the button is only clickable when it will do something (spawn a cube or destroy a spawned cube)
        UpdateButtonInteractability();
    }

    void SpawnNewCube() {
        if (spawnState != SpawnState.NoCubeSpawned) return;

        DynamicObject newCubeDynamicObj = Instantiate(cubePrefab, transform);
        newCubeDynamicObj.isGlobal = false; // Not global until retrieved from cube spawner
        PickupObject newCube = newCubeDynamicObj.GetComponent<PickupObject>();
        GravityObject newCubeGravity = newCubeDynamicObj.GetComponent<GravityObject>();
        Rigidbody newCubeRigidbody = newCube.thisRigidbody;
        newCube.transform.SetParent(null);
        newCube.transform.localScale = cubePrefab.transform.localScale;
        newCube.transform.position = transform.position + transform.up * spawnOffset;
        newCubeRigidbody.MovePosition(newCube.transform.position);
        newCube.transform.Rotate(Random.insideUnitSphere.normalized, Random.Range(0, 20f));
        newCubeRigidbody.MoveRotation(newCube.transform.rotation);
        newCubeGravity.gravityDirection = -transform.up;
        newCube.spawnedFrom = this;
        SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);

        lastCubeSpawnedPosition = newCube.transform.position;
        
        AffixSpawnedCubeWithParentDimensionObject(newCube);
        
        PillarDimensionObject[] dimensionObjs =
            newCube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
        foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
            newCubeDimensionObj.channel = backWall.channel;
        }

        cubeSpawned = newCube;
        AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerSpawn, ID, button.transform.position);
        spawnState.Set(SpawnState.CubeSpawnedButNotTaken);

        if (DEBUG) {
            Debug.Break();
        }
    }

    void AffixSpawnedCubeWithParentDimensionObject(PickupObject newCube) {
        // Create a parent object to place the DimensionObject script on that the cube will be a child of
        GameObject cubeParent = new GameObject("CubeDimensionObjectParent");
        DimensionObject parentDimensionObj = cubeParent.AddComponent<DimensionObject>();
        //parentDimensionObj.colliders = new Collider[] {cubeParent.AddComponent<BoxCollider>()};
        //parentDimensionObj.DEBUG = DEBUG;
        parentDimensionObj.startingVisibilityState = VisibilityState.partiallyVisible;
        parentDimensionObj.treatChildrenAsOneObjectRecursively = true;
        parentDimensionObj.ignoreChildrenWithDimensionObject = false;
        parentDimensionObj.channel = backWall.channel;
        // Don't save the specifics of the DimensionObject to the scene, we'll recreate it upon loading
        parentDimensionObj.SkipSave = true;
        cubeParent.name = "CubeDimensionObjectParent";
        cubeParent.transform.position = newCube.transform.position;
        cubeParent.transform.rotation = newCube.transform.rotation;
        SceneManager.MoveGameObjectToScene(cubeParent, gameObject.scene);
        
        newCube.transform.SetParent(cubeParent.transform);
        parentDimensionObj.InitializeRenderersAndLayers();
        parentDimensionObj.SwitchVisibilityState(VisibilityState.partiallyVisible, true);
        parentDimensionObj.SetCollision(VisibilityState.visible, VisibilityState.partiallyVisible, true);
        parentDimensionObj.SetCollision(VisibilityState.visible, VisibilityState.partiallyInvisible, true);
        parentDimensionObj.SetCollisionForPlayer(VisibilityState.partiallyVisible, false);
        parentDimensionObj.SetCollisionForPlayer(VisibilityState.partiallyInvisible, false);
        
        foreach (Collider c in parentDimensionObj.colliders) {
            // Collision disabled for first half of fall
            c.enabled = false;
        }
        
        SuperspectivePhysics.IgnoreCollision(roofCollider, newCube.GetComponent<Collider>());
    }
    
    void DestroyCubeAlreadyGrabbedFromSpawner() {
        void DissolveActiveCube(PickupObjectReference cube) {
            if (ReferenceIsReplaceable(cube)) {
                cube.Reference.MatchAction(
                    // If the object is loaded, play the shrink animation for this cube
                    pickupObject => {
                        AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerDespawn, ID, button.transform.position);
                        AudioManager.instance.PlayAtLocation(AudioName.RainstickFast, ID, pickupObject.transform.position);
                        cubeDespawning = pickupObject;

                        // Setup initial dissolve properties
                        foreach (var cubeRenderer in cubeDespawning.GetComponentsInChildren<Renderer>()) {
                            cubeRenderer.material.EnableKeyword("DISSOLVE_OBJECT");
                            cubeRenderer.material.SetFloat("_DissolveBurnSize", .1f);
                            if (cubeRenderer.gameObject == cubeDespawning.gameObject) {
                                cubeRenderer.material.SetTextureScale("_DissolveTex", Vector2.one * 0.02f);
                            }
                        }

                        despawnStartSize = cubeDespawning.transform.localScale;
                        despawnState.Set(DespawnState.CubeBeingDestroyed);
                    },
                    // If the object is not loaded, just destroy it directly
                    saveObject => {
                        despawnState.Set(DespawnState.CubeBeingDestroyed);
                        saveObject.Destroy();
                        despawnState.Set(DespawnState.Idle);
                    });

            }
        }

        if (spawnState == SpawnState.CubeTaken) {
            DissolveActiveCube(cubeGrabbedFromSpawner);
            cubeGrabbedFromSpawner = null;
        }
        else if (spawnState == SpawnState.CubeSpawnedButNotTaken) {
            DissolveActiveCube(cubeSpawned);
            cubeSpawned = null;
        }
    }

    void UpdateDematerializeCube(float timeSinceStateChanged) {
        if (cubeDespawning != null) {
            // Don't allow shrinking cubes to be picked up
            cubeDespawning.interactable = false;
            // Trick to get the cubeDespawning to not interact with the player anymore but still collide with ground
            cubeDespawning.gameObject.layer = LayerMask.NameToLayer("VisibleButNoPlayerCollision");
            cubeDespawning.thisRigidbody.isKinematic = true;
            foreach (Collider collider in cubeDespawning.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger) collider.enabled = false;
                else collider.isTrigger = true;
            }

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            if (timeSinceStateChanged < despawnTime) {
                timeSinceStateChanged += Time.deltaTime;
                float t = Mathf.Clamp01(timeSinceStateChanged / despawnTime);

                cubeDespawning.transform.localScale = Vector3.LerpUnclamped(
                    despawnStartSize,
                    despawnStartSize*despawnEndSizeMultiplier,
                    cubeDespawnSizeCurve.Evaluate(t)
                );
                propBlock.SetFloat("_DissolveValue", t);
                foreach (var cubeRenderer in cubeDespawning.GetComponentsInChildren<Renderer>()) {
                    cubeRenderer.SetPropertyBlock(propBlock);
                }
            }
            else {
                propBlock.SetFloat("_DissolveValue", 1);
                foreach (var cubeRenderer in cubeDespawning.GetComponentsInChildren<Renderer>()) {
                    cubeRenderer.SetPropertyBlock(propBlock);
                }
                cubeDespawning.GetComponent<DynamicObject>().Destroy();
                HandleSpawnedCubeBeingDestroyed();
            }
        }
    }

    public void HandleSpawnedCubeBeingDestroyed() {
        cubeDespawning = null;
        spawnState.Set(SpawnState.NoCubeSpawned);
        
        if (button.stateMachine.state == Button.State.ButtonUnpressed) {
            button.PressButton();
        }
    }

    void OnTriggerExit(Collider other) {
        PickupObject cube = other.GetComponent<PickupObject>();
        if (cube != null && cube == cubeSpawned) {
            spawnState.Set(SpawnState.CubeTaken);
            AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerClose, ID, glass.transform.position);
            
            // Restore collision with the roof of the Cube Spawner when the cube is taken from the spawner
            SuperspectivePhysics.RestoreCollision(roofCollider, cube.GetComponent<Collider>());
            cube.GetComponent<DynamicObject>().isGlobal = true; // Restore isGlobal behavior when retrieved from spawner

            // When the cube is removed from the spawner, reset its DimensionObject channel to their default
            DimensionObject originalDimensionObj = cubePrefab.GetComponentInChildren<DimensionObject>();
            if (originalDimensionObj != null) {
                PillarDimensionObject[] dimensionObjs =
                    cube.transform.GetComponentsInChildrenRecursively<PillarDimensionObject>();
                foreach (PillarDimensionObject newCubeDimensionObj in dimensionObjs) {
                    newCubeDimensionObj.channel = originalDimensionObj.channel;
                }
            }
            
            // Also remove the dimension parent object
            DimensionObject dimensionParent = cube.transform.parent?.GetComponent<DimensionObject>();
            dimensionParent.SwitchVisibilityState(VisibilityState.visible, true);
            dimensionParent.Unregister();
            Transform parent = cube.transform.parent;
            cube.transform.SetParent(null);
            Destroy(parent.gameObject);

            cubeSpawned = null;
            cubeGrabbedFromSpawner = cube;
            cube.gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }
    
    #region Saving

    [Serializable]
    public class CubeSpawnerSave : SerializableSaveObject<CubeSpawner> {
        PickupObjectReference cubeSpawned;
        PickupObjectReference cubeGrabbedFromSpawner;
        private StateMachine<SpawnState>.StateMachineSave spawnStateSave;
        private StateMachine<DespawnState>.StateMachineSave despawnStateSave;

        PickupObjectReference cubeDespawning;
        SerializableVector3 shrinkStartSize;
        SerializableAnimationCurve cubeDespawnSizeCurve;

        public CubeSpawnerSave(CubeSpawner spawner) : base(spawner) {
            this.cubeSpawned = spawner.cubeSpawned;
            this.cubeGrabbedFromSpawner = spawner.cubeGrabbedFromSpawner;
            spawnStateSave = spawner.spawnState.ToSave();
            despawnStateSave = spawner.despawnState.ToSave();
            this.cubeDespawning = spawner.cubeDespawning;
            this.cubeDespawnSizeCurve = spawner.cubeDespawnSizeCurve;
        }

        public override void LoadSave(CubeSpawner spawner) {
            spawner.cubeSpawned = this.cubeSpawned?.GetOrNull();
            if (spawner.cubeSpawned != null) {
                spawner.AffixSpawnedCubeWithParentDimensionObject(spawner.cubeSpawned);
            }
            spawner.cubeGrabbedFromSpawner = this.cubeGrabbedFromSpawner;
            spawner.spawnState.LoadFromSave(spawnStateSave);
            spawner.despawnState.LoadFromSave(despawnStateSave);
            spawner.cubeDespawning = this.cubeDespawning?.GetOrNull();
            spawner.cubeDespawnSizeCurve = this.cubeDespawnSizeCurve;
        }

        public void HandleSpawnedCubeBeingDestroyed() {
            cubeGrabbedFromSpawner = null;
            spawnStateSave.state = SpawnState.NoCubeSpawned;
            // TODO: Handle button depressing on the CubeSpawner while unloaded?
        }
    }
    #endregion
}
