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
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using PickupObjectReference = SerializableClasses.SuperspectiveReference<PickupObject, PickupObject.PickupObjectSave>;

[RequireComponent(typeof(UniqueId), typeof(BetterTrigger))]
public class CubeSpawner : SuperspectiveObject<CubeSpawner, CubeSpawner.CubeSpawnerSave>, BetterTriggers {
    public DimensionObject backWall;
    public Button button;
    public float tDistanceBeforeEnablingNewCubeColliders = 0.5f;
    const float SPAWN_OFFSET = 6;
    private float SpawnOffset => SPAWN_OFFSET * CurScale;

    private Vector3 lastCubeSpawnedPosition;

    // MultiDimension cubes interacting with the dimension object parent used in the non-euclidean cube spawner don't interact in predictable ways
    // Instead, we should instantiate a simpler cube prefab, and then swap it out with a real one when the player removes it from the spawner
    public DynamicObject cubeInSpawnerPrefab;
    public DynamicObject cubePrefab;
    public PickupObject cubeSpawned;
    [Header("Cube grabbed from spawner:")]
    public PickupObjectReference cubeGrabbedFromSpawner;
    // Colliders to suspend collision with while the cube is in the spawner
    public Collider[] temporarilyIgnoreColliders;
    public Transform glass;
    public Transform glassHitbox;
    const float GLASS_LOWER_DELAY = 1.75f;
    const float GLASS_RAISE_DELAY = 0.25f;
    const float GLASS_OFFSET = 1.5f;
    const float GLASS_MOVE_TIME = 1.2f;
    float startHeight;

    private float CurScale => transform.lossyScale.y;

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
    private const float respawnDelay = GLASS_RAISE_DELAY + GLASS_MOVE_TIME;
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

        button.pressDistance *= CurScale;

        startHeight = glass.transform.localPosition.y;
    }

    protected override void Init() {
        base.Init();
        InitializeSpawnStateMachine();
        InitializeDespawnStateMachine();

        if (glassHitbox != null) {
            StartCoroutine(MatchGlassHitboxPosition());
        }
    }

    IEnumerator MatchGlassHitboxPosition() {
        while (true) {
            if (GameManager.instance.IsCurrentlyLoading) yield return null;
            glassHitbox.position = glass.position;
            
            yield return null;
        }
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
            float distanceThreshold = SpawnOffset * tDistanceBeforeEnablingNewCubeColliders;
            
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

            if (timeSinceStateChanged > GLASS_LOWER_DELAY && timeSinceStateChanged < GLASS_MOVE_TIME + GLASS_LOWER_DELAY) {
                float time = timeSinceStateChanged - GLASS_LOWER_DELAY;
                float t = time / GLASS_MOVE_TIME;
                    
                Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                Vector3 endPos = new Vector3(glass.localPosition.x, startHeight-GLASS_OFFSET, glass.localPosition.z);
                glass.localPosition = Vector3.Lerp(startPos, endPos, t*t);
            }
            else if (timeSinceStateChanged >= GLASS_MOVE_TIME + GLASS_LOWER_DELAY) {
                glass.localPosition = new Vector3(glass.localPosition.x, startHeight-GLASS_OFFSET, glass.localPosition.z);
            }
        });

        Action<float> respawnDelayAndTakenUpdate = (timeSinceStateChanged) => {
            if (timeSinceStateChanged > GLASS_RAISE_DELAY && timeSinceStateChanged < GLASS_MOVE_TIME + GLASS_RAISE_DELAY) {
                float time = timeSinceStateChanged - GLASS_RAISE_DELAY;
                float t = time / GLASS_MOVE_TIME;

                Vector3 startPos = new Vector3(glass.localPosition.x, startHeight, glass.localPosition.z);
                Vector3 endPos = new Vector3(glass.localPosition.x, startHeight - GLASS_OFFSET, glass.localPosition.z);
                glass.localPosition = Vector3.Lerp(endPos, startPos, t * t);
            }
        };
        spawnState.WithUpdate(SpawnState.InRespawnDelay, respawnDelayAndTakenUpdate);
        spawnState.WithUpdate(SpawnState.CubeTaken, respawnDelayAndTakenUpdate);
    }

    void InitializeDespawnStateMachine() {
        despawnState.AddStateTransition(DespawnState.CubeBeingDestroyed, DespawnState.Idle, despawnTime);

        despawnState.OnStateChangeSimple += () => {
            switch (despawnState.State) {
                case DespawnState.Idle:
                    if (cubeDespawning != null) {
                        cubeDespawning.transform.FindInParentsRecursively<DynamicObject>().Destroy();
                    }
                    cubeDespawning = null;
                    break;
                case DespawnState.CubeBeingDestroyed:
                    if (spawnState.State is SpawnState.CubeSpawnedButNotTaken or SpawnState.CubeTaken) {
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
        else if ((spawnState == SpawnState.CubeSpawnedButNotTaken && spawnState.Time < GLASS_MOVE_TIME + GLASS_LOWER_DELAY) || spawnState == SpawnState.InRespawnDelay) {
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

        DynamicObject newCubeDynamicObj = Instantiate(cubeInSpawnerPrefab, transform);
        newCubeDynamicObj.isAllowedToChangeScenes = false; // Not global until retrieved from cube spawner
        PickupObject newCube = newCubeDynamicObj.GetComponent<PickupObject>();
        GravityObject newCubeGravity = newCubeDynamicObj.GetComponent<GravityObject>();
        Rigidbody newCubeRigidbody = newCube.thisRigidbody;
        newCube.transform.SetParent(null);
        newCube.transform.localScale = cubeInSpawnerPrefab.transform.localScale;
        newCube.transform.position = transform.position + transform.up * SpawnOffset;
        newCubeRigidbody.MovePosition(newCube.transform.position);
        newCube.transform.Rotate(Random.insideUnitSphere.normalized, Random.Range(0, 20f));
        newCubeRigidbody.MoveRotation(newCube.transform.rotation);
        newCubeGravity.GravityDirection = -transform.up;
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
        parentDimensionObj.startingVisibilityState = VisibilityState.PartiallyVisible;
        parentDimensionObj.treatChildrenAsOneObjectRecursively = true;
        parentDimensionObj.ignoreChildrenWithDimensionObject = false;
        // We're setting the channel after OnEnable, so we need to re-apply the channel logic to update it properly
        parentDimensionObj.channel = backWall.channel;
        parentDimensionObj.ValidateAndApplyChannelLogic();
        // Don't save the specifics of the DimensionObject to the scene, we'll recreate it upon loading
        parentDimensionObj.SkipSave = true;
        cubeParent.name = "CubeDimensionObjectParent";
        cubeParent.transform.position = newCube.transform.position;
        cubeParent.transform.rotation = newCube.transform.rotation;
        SceneManager.MoveGameObjectToScene(cubeParent, gameObject.scene);
        
        newCube.transform.SetParent(cubeParent.transform);
        parentDimensionObj.InitializeRenderersAndColliders(true);
        parentDimensionObj.SwitchVisibilityState(VisibilityState.PartiallyVisible, true);
        parentDimensionObj.SetCollision(VisibilityState.Visible, VisibilityState.PartiallyVisible, false);
        parentDimensionObj.SetCollision(VisibilityState.Visible, VisibilityState.PartiallyInvisible, true);
        parentDimensionObj.SetCollisionForPlayer(VisibilityState.PartiallyVisible, false);
        parentDimensionObj.SetCollisionForPlayer(VisibilityState.PartiallyInvisible, false);
        
        foreach (Collider c in parentDimensionObj.colliders) {
            // Collision disabled for first half of fall
            c.enabled = false;
        }

        Collider newCubeCollider = newCube.GetComponent<Collider>();
        foreach (Collider ignoreCollider in temporarilyIgnoreColliders) {
            SuperspectivePhysics.IgnoreCollision(ignoreCollider, newCubeCollider, ID);
        }
    }
    
    void DestroyCubeAlreadyGrabbedFromSpawner() {
        void DissolveActiveCube(PickupObjectReference cube) {
            if (ReferenceIsReplaceable(cube)) {
                cube?.Reference?.MatchAction(
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
            cubeDespawning.gameObject.layer = SuperspectivePhysics.VisibleButNoPlayerCollisionLayer;
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
                propBlock.SetFloat("_DissolveAmount", t);
                foreach (var cubeRenderer in cubeDespawning.GetComponentsInChildren<Renderer>()) {
                    cubeRenderer.SetPropertyBlock(propBlock);
                }
            }
            else {
                propBlock.SetFloat("_DissolveAmount", 1);
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
        
        if (button.stateMachine.State == Button.State.ButtonUnpressed) {
            button.PressButton();
        }
    }

    public void OnBetterTriggerExit(Collider other) {
        PickupObject cube = other.GetComponent<PickupObject>();
        if (cube != null && cube == cubeSpawned) {
            spawnState.Set(SpawnState.CubeTaken);
            AudioManager.instance.PlayAtLocation(AudioName.CubeSpawnerClose, ID, glass.transform.position);
            
            // Restore collision with the roof of the Cube Spawner when the cube is taken from the spawner
            Collider cubeCollider = cube.GetComponent<Collider>();
            foreach (Collider ignoreCollider in temporarilyIgnoreColliders) {
                SuperspectivePhysics.RestoreCollision(ignoreCollider, cubeCollider, ID);
            }
            cube.GetComponent<DynamicObject>().isAllowedToChangeScenes = true; // Restore isGlobal behavior when retrieved from spawner

            // When the cube is removed from the spawner, swap it out with the real cube prefab
            DynamicObject newCubeDynamicObj = Instantiate(cubePrefab, transform);
            PickupObject newCube = newCubeDynamicObj.GetComponent<PickupObject>();
            GravityObject newCubeGravity = newCubeDynamicObj.GetComponent<GravityObject>();
            Rigidbody newCubeRigidbody = newCube.thisRigidbody;
            newCube.transform.SetParent(null);
            newCube.transform.localScale = cubePrefab.transform.localScale;
            newCube.transform.position = cubeSpawned.transform.position;
            newCubeRigidbody.MovePosition(newCube.transform.position);
            newCube.transform.rotation = cubeSpawned.transform.rotation;
            newCubeRigidbody.MoveRotation(newCube.transform.rotation);
            newCubeGravity.GravityDirection = cubeSpawned.GetComponent<GravityObject>().GravityDirection;
            newCube.spawnedFrom = this;
            SceneManager.MoveGameObjectToScene(newCube.gameObject, gameObject.scene);

            newCube.isHeld = cubeSpawned.isHeld;
            if (Player.instance.IsHoldingSomething) {
                Player.instance.heldObject = newCube;
            }
            newCubeGravity.useGravity = false;
            
            // Delete the temp cube from the spawner
            cubeSpawned.GetComponent<DynamicObject>().Destroy();
            
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
            dimensionParent.UninitializeRenderersAndColliders();
            dimensionParent.Unregister();
            Transform parent = cube.transform.parent;
            cube.transform.SetParent(null);
            Destroy(parent.gameObject);

            // We disable the colliders of the new cube for the first frame after being spawned, to allow GrowShrinkObject to apply its starting scale without bumping the player
            Collider[] collidersTemporarilyDisabled = newCube.transform.GetComponentsInChildrenRecursively<Collider>();
            foreach (Collider c in collidersTemporarilyDisabled) {
                c.enabled = false;
            }
            SuperspectivePhysics.IgnoreCollision(newCube.thisCollider, Player.instance.collider, newCube.ID);

            StartCoroutine(RestoreColliders(collidersTemporarilyDisabled));

            cubeSpawned = null;
            cubeGrabbedFromSpawner = newCube;
            newCube.gameObject.layer = SuperspectivePhysics.DefaultLayer;
        }
    }

    private IEnumerator RestoreColliders(Collider[] collidersTemporarilyIgnored) {
        yield return null;
        foreach (Collider c in collidersTemporarilyIgnored) {
            c.enabled = true;
        }
    }

    public void OnBetterTriggerEnter(Collider other) { }
    public void OnBetterTriggerStay(Collider other) { }

#region Saving

    public override void LoadSave(CubeSpawnerSave save) {
        cubeSpawned = save.cubeSpawned?.GetOrNull();
        if (cubeSpawned != null) {
            AffixSpawnedCubeWithParentDimensionObject(cubeSpawned);
        }
        cubeGrabbedFromSpawner = save.cubeGrabbedFromSpawner;
        spawnState.LoadFromSave(save.spawnStateSave);
        despawnState.LoadFromSave(save.despawnStateSave);
        cubeDespawning = save.cubeDespawning?.GetOrNull();
        cubeDespawnSizeCurve = save.cubeDespawnSizeCurve;
    }

    [Serializable]
    public class CubeSpawnerSave : SaveObject<CubeSpawner> {
        public StateMachine<SpawnState>.StateMachineSave spawnStateSave;
        public StateMachine<DespawnState>.StateMachineSave despawnStateSave;
        public PickupObjectReference cubeSpawned;
        public PickupObjectReference cubeGrabbedFromSpawner;

        public PickupObjectReference cubeDespawning;
        public SerializableAnimationCurve cubeDespawnSizeCurve;

        public CubeSpawnerSave(CubeSpawner spawner) : base(spawner) {
            this.cubeSpawned = spawner.cubeSpawned;
            this.cubeGrabbedFromSpawner = spawner.cubeGrabbedFromSpawner;
            spawnStateSave = spawner.spawnState.ToSave();
            despawnStateSave = spawner.despawnState.ToSave();
            this.cubeDespawning = spawner.cubeDespawning;
            this.cubeDespawnSizeCurve = spawner.cubeDespawnSizeCurve;
        }

        public void HandleSpawnedCubeBeingDestroyed() {
            cubeGrabbedFromSpawner = null;
            spawnStateSave.state = SpawnState.NoCubeSpawned;
            spawnStateSave.timeSinceStateChanged = 0;
            // TODO: Handle button depressing on the CubeSpawner while unloaded?
        }
    }
    #endregion
}
